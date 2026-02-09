using AudioSample.Models;
using AudioSample.Utils;

namespace AudioSample.Integrators
{
    public class TranscriptionIntegrator
    {
        #region 公共方法

        public static IntegratedTranscriptionResult Integrate(
            WhisperResult whisperResult,
            SherpaVADResult? vadResult = null,
            SherpaSpeakerResult? speakerResult = null,
            SherpaTranscriptionResult? wordTimestampResult = null)
        {
            var integrated = new IntegratedTranscriptionResult();

            #region 基础信息

            integrated.FullText = whisperResult.Text;
            integrated.SpeakerCount = speakerResult?.Segments.Select(s => s.SpeakerId).Distinct().Count() ?? 0;
            integrated.TotalDuration = whisperResult.Segments.Any() 
                ? whisperResult.Segments.Max(s => s.End) 
                : 0;

            #endregion

            #region 整合片段

            foreach (var whisperSegment in whisperResult.Segments)
            {
                var audioSegment = new AudioSegment
                {
                    StartTime = whisperSegment.Start,
                    EndTime = whisperSegment.End,
                    Text = whisperSegment.Text
                };

                #region 使用 VAD 结果调整时间边界

                if (vadResult != null && vadResult.Segments.Any())
                {
                    var adjustedSegment = AdjustSegmentWithVAD(audioSegment, vadResult);
                    audioSegment.StartTime = adjustedSegment.StartTime;
                    audioSegment.EndTime = adjustedSegment.EndTime;
                }

                #endregion

                #region 添加说话人信息

                if (speakerResult != null && speakerResult.Segments.Any())
                {
                    audioSegment.SpeakerId = GetSpeakerForSegment(audioSegment, speakerResult);
                }

                #endregion

                #region 添加词级时间戳

                if (wordTimestampResult != null && wordTimestampResult.WordTimestamps.Any())
                {
                    audioSegment.WordTimestamps = GetWordTimestampsForSegment(audioSegment, wordTimestampResult);
                }
                else if (whisperSegment.Words != null && whisperSegment.Words.Any())
                {
                    audioSegment.WordTimestamps = whisperSegment.Words;
                }

                #endregion

                integrated.Segments.Add(audioSegment);
            }

            #endregion

            return integrated;
        }

        #endregion

        #region 私有方法

        private static AudioSegment AdjustSegmentWithVAD(AudioSegment segment, SherpaVADResult vadResult)
        {
            var adjusted = new AudioSegment
            {
                StartTime = segment.StartTime,
                EndTime = segment.EndTime,
                Text = segment.Text
            };

            var overlappingVADSegments = vadResult.Segments
                .Where(v => v.EndTime > segment.StartTime && v.StartTime < segment.EndTime)
                .OrderBy(v => v.StartTime)
                .ToList();

            if (overlappingVADSegments.Any())
            {
                adjusted.StartTime = Math.Max(segment.StartTime, overlappingVADSegments.First().StartTime);
                adjusted.EndTime = Math.Min(segment.EndTime, overlappingVADSegments.Last().EndTime);
            }

            return adjusted;
        }

        private static int? GetSpeakerForSegment(AudioSegment segment, SherpaSpeakerResult speakerResult)
        {
            var segmentCenter = (segment.StartTime + segment.EndTime) / 2;

            var overlappingSpeakers = speakerResult.Segments
                .Where(s => s.EndTime > segment.StartTime && s.StartTime < segment.EndTime)
                .ToList();

            if (!overlappingSpeakers.Any())
            {
                return null;
            }

            if (overlappingSpeakers.Count == 1)
            {
                return overlappingSpeakers[0].SpeakerId;
            }

            var longestOverlap = overlappingSpeakers
                .Select(s => new
                {
                    Speaker = s,
                    OverlapDuration = Math.Min(s.EndTime, segment.EndTime) - Math.Max(s.StartTime, segment.StartTime)
                })
                .OrderByDescending(x => x.OverlapDuration)
                .First();

            return longestOverlap.Speaker.SpeakerId;
        }

        private static List<WordTimestamp> GetWordTimestampsForSegment(
            AudioSegment segment,
            SherpaTranscriptionResult wordTimestampResult)
        {
            return wordTimestampResult.WordTimestamps
                .Where(w => w.EndTime > segment.StartTime && w.StartTime < segment.EndTime)
                .Select(w => new WordTimestamp
                {
                    Word = w.Word,
                    StartTime = Math.Max(w.StartTime, segment.StartTime),
                    EndTime = Math.Min(w.EndTime, segment.EndTime)
                })
                .OrderBy(w => w.StartTime)
                .ToList();
        }

        #endregion

        #region 导出方法

        public static string ExportToSRT(IntegratedTranscriptionResult result)
        {
            var srtBuilder = new System.Text.StringBuilder();

            for (int i = 0; i < result.Segments.Count; i++)
            {
                var segment = result.Segments[i];
                srtBuilder.AppendLine($"{i + 1}");
                srtBuilder.AppendLine($"{AudioUtils.FormatTime(segment.StartTime)} --> {AudioUtils.FormatTime(segment.EndTime)}");

                var speakerPrefix = segment.SpeakerId.HasValue ? $"[Speaker {segment.SpeakerId.Value}] " : "";
                srtBuilder.AppendLine($"{speakerPrefix}{segment.Text}");
                srtBuilder.AppendLine();
            }

            return srtBuilder.ToString();
        }

        public static string ExportToJson(IntegratedTranscriptionResult result)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            return System.Text.Json.JsonSerializer.Serialize(result, options);
        }

        public static string ExportToText(IntegratedTranscriptionResult result)
        {
            var textBuilder = new System.Text.StringBuilder();

            foreach (var segment in result.Segments)
            {
                var speakerPrefix = segment.SpeakerId.HasValue ? $"[Speaker {segment.SpeakerId.Value}] " : "";
                textBuilder.AppendLine($"{speakerPrefix}{segment.Text}");
            }

            return textBuilder.ToString();
        }

        public static string ExportToDetailedText(IntegratedTranscriptionResult result)
        {
            var textBuilder = new System.Text.StringBuilder();

            foreach (var segment in result.Segments)
            {
                var speakerPrefix = segment.SpeakerId.HasValue ? $"[Speaker {segment.SpeakerId.Value}] " : "";
                textBuilder.AppendLine($"[{AudioUtils.FormatTime(segment.StartTime)} - {AudioUtils.FormatTime(segment.EndTime)}] {speakerPrefix}{segment.Text}");

                if (segment.WordTimestamps.Any())
                {
                    foreach (var word in segment.WordTimestamps)
                    {
                        textBuilder.AppendLine($"  [{AudioUtils.FormatTime(word.StartTime)} - {AudioUtils.FormatTime(word.EndTime)}] {word.Word}");
                    }
                }
                textBuilder.AppendLine();
            }

            return textBuilder.ToString();
        }

        #endregion
    }
}
