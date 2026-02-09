using VideoTranslator.Models;
using TestSplitScheme.Models;

namespace TestSplitScheme.Services
{
    public class AudioSplitter
    {
        #region 常量定义

        private const decimal MAX_SEGMENT_DURATION_SECONDS = 300;
        private const decimal MIN_SILENCE_FOR_SPLIT_SECONDS = 0.50M;

        #endregion

        #region 公共方法

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

        #region 私有方法 - 分段创建

        private AudioSegment CreateNewSegment(decimal start, decimal end)
        {
            return new AudioSegment { Start = start, End = end };
        }

        #endregion

        #region 私有方法 - 分段判断

        private bool ShouldSplitSegment(AudioSegment segment)
        {
            return segment.Duration > MAX_SEGMENT_DURATION_SECONDS;
        }

        #endregion

        #region 私有方法 - 分段位置查找

        private (decimal? position, decimal silenceDuration) FindBestSplitPosition(VadDetectionResult vadResult, VadSegment currentVadSegment, AudioSegment currentSegment)
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
