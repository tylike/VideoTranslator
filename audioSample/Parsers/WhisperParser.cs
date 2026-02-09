using System.Text.RegularExpressions;
using AudioSample.Models;
using AudioSample.Utils;

namespace AudioSample.Parsers
{
    public class WhisperParser
    {
        #region 公共方法

        public static async Task<WhisperResult> ParseFromOutputAsync(string output)
        {
            var result = new WhisperResult();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            #region 解析文本内容

            var textBuilder = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                if (!line.StartsWith('[') && !line.StartsWith('{'))
                {
                    textBuilder.AppendLine(line.Trim());
                }
            }
            result.Text = textBuilder.ToString().Trim();

            #endregion

            #region 解析时间戳片段

            var segmentPattern = @"\[(\d{2}:\d{2}:\d{2}\.\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2}\.\d{3})\]\s*(.+)";
            var segmentRegex = new Regex(segmentPattern);

            foreach (var line in lines)
            {
                var match = segmentRegex.Match(line);
                if (match.Success)
                {
                    var startTime = AudioUtils.ParseTimeToSeconds(match.Groups[1].Value);
                    var endTime = AudioUtils.ParseTimeToSeconds(match.Groups[2].Value);
                    var text = match.Groups[3].Value.Trim();

                    result.Segments.Add(new WhisperSegment
                    {
                        Id = result.Segments.Count,
                        Start = startTime,
                        End = endTime,
                        Text = text
                    });
                }
            }

            #endregion

            return await Task.FromResult(result);
        }

        public static async Task<WhisperResult> ParseFromJsonAsync(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"JSON file not found: {jsonPath}");
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            var result = new WhisperResult
            {
                Text = root.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? string.Empty : string.Empty
            };

            if (root.TryGetProperty("segments", out var segmentsProp))
            {
                foreach (var segment in segmentsProp.EnumerateArray())
                {
                    var whisperSegment = new WhisperSegment
                    {
                        Id = segment.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0,
                        Start = segment.TryGetProperty("start", out var startProp) ? startProp.GetDouble() : 0,
                        End = segment.TryGetProperty("end", out var endProp) ? endProp.GetDouble() : 0,
                        Text = segment.TryGetProperty("text", out var segTextProp) ? segTextProp.GetString() ?? string.Empty : string.Empty
                    };

                    if (segment.TryGetProperty("words", out var wordsProp))
                    {
                        whisperSegment.Words = new List<WordTimestamp>();
                        foreach (var word in wordsProp.EnumerateArray())
                        {
                            whisperSegment.Words.Add(new WordTimestamp
                            {
                                Word = word.TryGetProperty("word", out var wordProp) ? wordProp.GetString() ?? string.Empty : string.Empty,
                                StartTime = word.TryGetProperty("start", out var wStartProp) ? wStartProp.GetDouble() : 0,
                                EndTime = word.TryGetProperty("end", out var wEndProp) ? wEndProp.GetDouble() : 0
                            });
                        }
                    }

                    result.Segments.Add(whisperSegment);
                }
            }

            return result;
        }

        public static async Task<WhisperResult> ParseFromSrtAsync(string srtPath)
        {
            if (!File.Exists(srtPath))
            {
                throw new FileNotFoundException($"SRT file not found: {srtPath}");
            }

            var content = await File.ReadAllTextAsync(srtPath);
            var result = new WhisperResult();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var i = 0;

            while (i < lines.Length)
            {
                if (int.TryParse(lines[i].Trim(), out _))
                {
                    i++;
                    if (i >= lines.Length) break;

                    var timeLine = lines[i].Trim();
                    var timeMatch = Regex.Match(timeLine, @"(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})");
                    if (timeMatch.Success)
                    {
                        var startTime = AudioUtils.ParseTimeToSeconds(timeMatch.Groups[1].Value.Replace(',', '.'));
                        var endTime = AudioUtils.ParseTimeToSeconds(timeMatch.Groups[2].Value.Replace(',', '.'));
                        i++;

                        var textBuilder = new System.Text.StringBuilder();
                        while (i < lines.Length && !int.TryParse(lines[i].Trim(), out _))
                        {
                            textBuilder.AppendLine(lines[i].Trim());
                            i++;
                        }

                        result.Segments.Add(new WhisperSegment
                        {
                            Id = result.Segments.Count,
                            Start = startTime,
                            End = endTime,
                            Text = textBuilder.ToString().Trim()
                        });
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            result.Text = string.Join(" ", result.Segments.Select(s => s.Text));
            return result;
        }

        #endregion
    }
}
