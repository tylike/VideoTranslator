using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoTranslator.SRT.Core.Services;

public class SrtCleaner
{
    #region 公共方法

    public SrtFile CleanEmptySubtitles(SrtFile srtFile)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var cleanedSubtitles = srtFile.Subtitles
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .ToList();

        var result = new SrtFile(cleanedSubtitles);
        result.ReindexSubtitles();
        return result;
    }

    public string CleanEmptySubtitles(string srtContent)
    {
        if (string.IsNullOrWhiteSpace(srtContent))
        {
            return srtContent;
        }

        var lines = srtContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cleanedLines = new List<string>();
        var i = 0;

        while (i < lines.Length)
        {
            if (int.TryParse(lines[i].Trim(), out _))
            {
                var timeLineIndex = i + 1;
                var textStartIndex = i + 2;

                if (timeLineIndex < lines.Length && IsTimeLine(lines[timeLineIndex]))
                {
                    var textLines = new List<string>();
                    var j = textStartIndex;

                    while (j < lines.Length && !string.IsNullOrWhiteSpace(lines[j]) && !int.TryParse(lines[j].Trim(), out _))
                    {
                        textLines.Add(lines[j]);
                        j++;
                    }

                    if (textLines.Any(t => !string.IsNullOrWhiteSpace(t)))
                    {
                        cleanedLines.Add(lines[i]);
                        cleanedLines.Add(lines[timeLineIndex]);
                        cleanedLines.AddRange(textLines);
                        cleanedLines.Add("");
                    }

                    i = j;
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

        return string.Join("\r\n", cleanedLines);
    }

    public void CleanEmptySubtitles(string inputPath, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("输入路径不能为空", nameof(inputPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("输出路径不能为空", nameof(outputPath));
        }

        var content = System.IO.File.ReadAllText(inputPath);
        var cleanedContent = CleanEmptySubtitles(content);
        System.IO.File.WriteAllText(outputPath, cleanedContent);
    }

    public SrtFile RemoveDuplicateText(SrtFile srtFile)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var seenTexts = new HashSet<string>();
        var uniqueSubtitles = new List<ISrtSubtitle>();

        foreach (var subtitle in srtFile.Subtitles)
        {
            var normalizedText = NormalizeText(subtitle.Text);
            if (!seenTexts.Contains(normalizedText))
            {
                seenTexts.Add(normalizedText);
                uniqueSubtitles.Add(subtitle);
            }
        }

        var result = new SrtFile(uniqueSubtitles);
        result.ReindexSubtitles();
        return result;
    }

    public SrtFile RemoveOverlappingDuplicates(SrtFile srtFile)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var cleanedSubtitles = new List<ISrtSubtitle>();
        var seenTexts = new HashSet<string>();

        foreach (var subtitle in srtFile.Subtitles.OrderBy(s => s.StartTime))
        {
            var normalizedText = NormalizeText(subtitle.Text);

            if (!seenTexts.Contains(normalizedText))
            {
                seenTexts.Add(normalizedText);
                cleanedSubtitles.Add(subtitle);
            }
            else
            {
                var existing = cleanedSubtitles.Last(s => NormalizeText(s.Text) == normalizedText);
                if (subtitle.EndTime > existing.EndTime)
                {
                    existing.EndTime = subtitle.EndTime;
                }
            }
        }

        var result = new SrtFile(cleanedSubtitles);
        result.ReindexSubtitles();
        return result;
    }

    #endregion

    #region 私有方法

    private bool IsTimeLine(string line)
    {
        return Regex.IsMatch(line, @"\d{2}:\d{2}:\d{2},\d{3}\s*-->\s*\d{2}:\d{2}:\d{2},\d{3}");
    }

    private string NormalizeText(string text)
    {
        return text
            .Replace(">>", "")
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Trim()
            .ToLowerInvariant();
    }

    #endregion
}
