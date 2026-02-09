using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoTranslator.SRT.Core.Interfaces;
using VideoTranslator.SRT.Core.Models;

namespace VideoTranslator.SRT.Core.Services;

public class SrtReader : ISrtReader
{
    #region 私有字段

    private static readonly Regex SrtBlockPattern = new(
        @"(\d+)\r?\n(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})\r?\n(.*?)(?=\r?\n\r?\n|\Z)",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex TimePattern = new(
        @"(\d{2}):(\d{2}):(\d{2}),(\d{3})",
        RegexOptions.Compiled
    );

    #endregion

    #region 同步方法

    public SrtFile Read(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("SRT文件不存在", filePath);
        }

        string content = File.ReadAllText(filePath);
        return ReadFromString(content);
    }

    public SrtFile ReadFromString(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SrtFile();
        }

        var srtFile = new SrtFile();
        var matches = SrtBlockPattern.Matches(content);

        foreach (Match match in matches)
        {
            var subtitle = ParseSubtitle(match);
            if (subtitle != null)
            {
                srtFile.AddSubtitle(subtitle);
            }
        }

        return srtFile;
    }

    #endregion

    #region 异步方法

    public async Task<SrtFile> ReadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("SRT文件不存在", filePath);
        }

        string content = await File.ReadAllTextAsync(filePath);
        return await ReadFromStringAsync(content);
    }

    public Task<SrtFile> ReadFromStringAsync(string content)
    {
        return Task.FromResult(ReadFromString(content));
    }

    #endregion

    #region 验证方法

    public bool IsValidSrtFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        string content = File.ReadAllText(filePath);
        return IsValidSrtContent(content);
    }

    public bool IsValidSrtContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return SrtBlockPattern.IsMatch(content);
    }

    #endregion

    #region 私有方法

    private SrtSubtitle? ParseSubtitle(Match match)
    {
        try
        {
            int index = int.Parse(match.Groups[1].Value);
            TimeSpan startTime = ParseTimeString(match.Groups[2].Value);
            TimeSpan endTime = ParseTimeString(match.Groups[3].Value);
            string text = match.Groups[4].Value.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return new SrtSubtitle(index, startTime, endTime, text);
        }
        catch
        {
            return null;
        }
    }

    private TimeSpan ParseTimeString(string timeString)
    {
        var match = TimePattern.Match(timeString);
        if (!match.Success)
        {
            throw new FormatException($"无效的时间格式: {timeString}");
        }

        int hours = int.Parse(match.Groups[1].Value);
        int minutes = int.Parse(match.Groups[2].Value);
        int seconds = int.Parse(match.Groups[3].Value);
        int milliseconds = int.Parse(match.Groups[4].Value);

        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }

    #endregion
}
