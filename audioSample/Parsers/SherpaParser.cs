using System.Text.RegularExpressions;
using AudioSample.Models;
using AudioSample.Utils;

namespace AudioSample.Parsers
{
    public class SherpaParser
    {
        #region VAD 解析

        public static async Task<SherpaVADResult> ParseVADOutputAsync(string output)
        {
            var result = new SherpaVADResult();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var segmentPattern = @"Speech segment\s+(\d+):\s*start\s*=\s*([\d.]+),\s*end\s*=\s*([\d.]+)";
            var segmentRegex = new Regex(segmentPattern);

            foreach (var line in lines)
            {
                var match = segmentRegex.Match(line);
                if (match.Success)
                {
                    result.Segments.Add(new VADSegment
                    {
                        StartTime = double.Parse(match.Groups[2].Value) / 1000.0,
                        EndTime = double.Parse(match.Groups[3].Value) / 1000.0
                    });
                }
            }

            return await Task.FromResult(result);
        }

        #endregion

        #region 说话人识别解析

        public static async Task<SherpaSpeakerResult> ParseSpeakerDiarizationAsync(string output)
        {
            var result = new SherpaSpeakerResult();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var segmentPattern = @"speaker\s+(\d+):\s*([\d.]+)\s*--\s*([\d.]+)";
            var segmentRegex = new Regex(segmentPattern);

            foreach (var line in lines)
            {
                var match = segmentRegex.Match(line);
                if (match.Success)
                {
                    result.Segments.Add(new SpeakerSegment
                    {
                        SpeakerId = int.Parse(match.Groups[1].Value),
                        StartTime = double.Parse(match.Groups[2].Value),
                        EndTime = double.Parse(match.Groups[3].Value)
                    });
                }
            }

            return await Task.FromResult(result);
        }

        #endregion

        #region 词级时间戳解析

        public static async Task<SherpaTranscriptionResult> ParseTranscriptionOutputAsync(string output)
        {
            var result = new SherpaTranscriptionResult();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var textBuilder = new System.Text.StringBuilder();
            var wordPattern = @"([\d.]+)\s*-\s*([\d.]+):\s*(.+)";
            var wordRegex = new Regex(wordPattern);

            foreach (var line in lines)
            {
                var match = wordRegex.Match(line);
                if (match.Success)
                {
                    var startTime = double.Parse(match.Groups[1].Value);
                    var endTime = double.Parse(match.Groups[2].Value);
                    var text = match.Groups[3].Value.Trim();

                    result.WordTimestamps.Add(new WordTimestamp
                    {
                        Word = text,
                        StartTime = startTime,
                        EndTime = endTime
                    });

                    textBuilder.Append(text + " ");
                }
                else if (!line.Contains(':') && !line.Contains('-'))
                {
                    textBuilder.AppendLine(line.Trim());
                }
            }

            result.Text = textBuilder.ToString().Trim();
            return await Task.FromResult(result);
        }

        #endregion

        #region JSON 格式解析

        public static async Task<SherpaTranscriptionResult> ParseTranscriptionJsonAsync(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"JSON file not found: {jsonPath}");
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            var result = new SherpaTranscriptionResult
            {
                Text = root.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? string.Empty : string.Empty
            };

            if (root.TryGetProperty("segments", out var segmentsProp))
            {
                foreach (var segment in segmentsProp.EnumerateArray())
                {
                    if (segment.TryGetProperty("words", out var wordsProp))
                    {
                        foreach (var word in wordsProp.EnumerateArray())
                        {
                            result.WordTimestamps.Add(new WordTimestamp
                            {
                                Word = word.TryGetProperty("word", out var wordProp) ? wordProp.GetString() ?? string.Empty : string.Empty,
                                StartTime = word.TryGetProperty("start", out var wStartProp) ? wStartProp.GetDouble() : 0,
                                EndTime = word.TryGetProperty("end", out var wEndProp) ? wEndProp.GetDouble() : 0
                            });
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
