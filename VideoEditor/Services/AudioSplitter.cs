using VideoTranslator.Models;
using System.Collections.Generic;
using System.Linq;
using VideoEditor.Models;

namespace VideoEditor.Services
{
    public class AudioSplitter
    {
        #region 常量定义

        private const decimal MAX_SEGMENT_DURATION_SECONDS = 300;
        private const decimal MIN_SILENCE_FOR_SPLIT_SECONDS = 0.50M;

        #endregion

        #region 公共方法

        /// <summary>
        /// 根据VAD检测结果生成分割方案
        /// </summary>
        /// <param name="vadResult">VAD检测结果</param>
        /// <returns>分割方案列表</returns>
        public List<SplitScheme> GenerateSplitSchemes(VadDetectionResult vadResult)
        {
            var schemes = new List<SplitScheme>();

            #region 按目标时长生成不同方案

            var targetDurations = new[] { 180m, 210m, 240m, 270m, 300m };

            for (int i = 0; i < targetDurations.Length; i++)
            {
                var scheme = GenerateSingleScheme(vadResult, targetDurations[i], i + 1);
                if (scheme != null)
                {
                    schemes.Add(scheme);
                }
            }

            #endregion

            return schemes;
        }

        /// <summary>
        /// 执行音频分割，返回分割后的音频片段
        /// </summary>
        /// <param name="vadResult">VAD检测结果</param>
        /// <returns>音频片段列表</returns>
        public List<AudioSegment> SplitAudio(VadDetectionResult vadResult)
        {
            var segments = new List<AudioSegment>();
            AudioSegment currentSegment = null;

            #region 遍历VAD片段进行分段

            foreach (var vadSegment in vadResult.Segments)
            {
                if (currentSegment == null)
                {
                    if (vadSegment.IsSpeech)
                    {
                        currentSegment = CreateNewSegment(vadSegment.Start, vadSegment.End);
                        segments.Add(currentSegment);
                    }
                    continue;
                }

                currentSegment.End = vadSegment.End;

                if (ShouldSplitSegment(currentSegment))
                {
                    var (splitPosition, silenceDuration) = FindBestSplitPosition(vadResult, vadSegment, currentSegment);

                    if (splitPosition.HasValue)
                    {
                        currentSegment.End = splitPosition.Value;
                        currentSegment.SplitSilenceDuration = silenceDuration;
                        currentSegment = null;
                    }
                }
            }

            #endregion

            return segments;
        }

        #endregion

        #region 私有方法 - 方案生成

        private SplitScheme GenerateSingleScheme(VadDetectionResult vadResult, decimal targetDuration, int schemeIndex)
        {
            var minTargetDuration = 180m;
            var maxTargetDuration = 300m;

            var silenceSegments = vadResult.Segments
                .Where(s => !s.IsSpeech && s.Duration >= 0.5M)
                .OrderByDescending(s => s.Duration)
                .ToList();

            var splitPoints = new List<decimal>();
            var usedSilences = new Dictionary<decimal, VadSegment>();
            var currentTime = 0m;

            #region 查找分割点

            while (currentTime < vadResult.AudioDuration - minTargetDuration)
            {
                var targetTime = currentTime + targetDuration;

                var searchRangeStart = Math.Max(0, targetTime - 60);
                var searchRangeEnd = Math.Min(vadResult.AudioDuration, targetTime + 60);

                var bestSilence = silenceSegments
                    .Where(s => s.Start >= searchRangeStart && s.Start <= searchRangeEnd && !usedSilences.ContainsKey(s.Start))
                    .OrderByDescending(s => s.Duration)
                    .FirstOrDefault();

                if (bestSilence != null)
                {
                    var segmentDuration = bestSilence.Start - currentTime;

                    if (segmentDuration >= minTargetDuration && segmentDuration <= maxTargetDuration)
                    {
                        splitPoints.Add(bestSilence.Start);
                        usedSilences[bestSilence.Start] = bestSilence;
                        currentTime = bestSilence.End;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            #endregion

            if (splitPoints.Count > 0)
            {
                var segmentCount = splitPoints.Count + 1;
                var averageDuration = vadResult.AudioDuration / segmentCount;
                var minSilence = splitPoints
                    .SelectMany(p => silenceSegments.Where(s => Math.Abs(s.Start - p) < 0.1m))
                    .Min(s => s.Duration);

                var segments = new List<SplitSchemeSegment>();
                currentTime = 0m;

                #region 生成分段详情

                for (int i = 0; i < splitPoints.Count; i++)
                {
                    var splitPoint = splitPoints[i];
                    var segment = new SplitSchemeSegment
                    {
                        SegmentIndex = i + 1,
                        StartTime = currentTime,
                        EndTime = splitPoint,
                        Duration = splitPoint - currentTime,
                        SplitSilenceStart = usedSilences.TryGetValue(splitPoint, out var silence) ? silence.Start : null,
                        SplitSilenceEnd = usedSilences.TryGetValue(splitPoint, out silence) ? silence.End : null,
                        SplitSilenceDuration = usedSilences.TryGetValue(splitPoint, out silence) ? silence.Duration : null
                    };
                    segments.Add(segment);
                    currentTime = splitPoint;
                }

                var lastSegment = new SplitSchemeSegment
                {
                    SegmentIndex = splitPoints.Count + 1,
                    StartTime = currentTime,
                    EndTime = vadResult.AudioDuration,
                    Duration = vadResult.AudioDuration - currentTime,
                    SplitSilenceStart = null,
                    SplitSilenceEnd = null,
                    SplitSilenceDuration = null
                };
                segments.Add(lastSegment);

                #endregion

                return new SplitScheme
                {
                    Index = schemeIndex,
                    SegmentCount = segmentCount,
                    AverageDuration = averageDuration,
                    MinSilenceDuration = minSilence,
                    Description = $"方案{schemeIndex}（{targetDuration / 60:F1}分钟/段）：共{segmentCount}段，平均{averageDuration / 60:F1}分钟",
                    SplitPoints = splitPoints,
                    Segments = segments
                };
            }

            return null;
        }

        #endregion

        #region 私有方法 - 音频分割

        private AudioSegment CreateNewSegment(decimal start, decimal end)
        {
            return new AudioSegment { Start = start, End = end };
        }

        private bool ShouldSplitSegment(AudioSegment segment)
        {
            return segment.Duration > MAX_SEGMENT_DURATION_SECONDS;
        }

        private (decimal? position, decimal silenceDuration) FindBestSplitPosition(
            VadDetectionResult vadResult,
            VadSegment currentVadSegment,
            AudioSegment currentSegment)
        {
            #region 在当前VAD片段之后查找合适的静音位置

            var currentIndex = vadResult.Segments.IndexOf(currentVadSegment);

            for (int i = currentIndex + 1; i < vadResult.Segments.Count; i++)
            {
                var segment = vadResult.Segments[i];

                if (!segment.IsSpeech && segment.Duration >= MIN_SILENCE_FOR_SPLIT_SECONDS)
                {
                    return (segment.Start, segment.Duration);
                }

                if (segment.IsSpeech)
                {
                    var potentialDuration = segment.End - currentSegment.Start;

                    if (potentialDuration > MAX_SEGMENT_DURATION_SECONDS * 1.5M)
                    {
                        return (segment.Start, 0);
                    }
                }
            }

            #endregion

            return (null, 0);
        }

        #endregion
    }
}
