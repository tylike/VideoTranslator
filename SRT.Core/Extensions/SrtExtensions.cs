using System;
using System.Collections.Generic;
using System.Linq;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoTranslator.SRT.Core.Extensions;

public static class SrtExtensions
{
    #region 时间相关扩展

    public static TimeSpan ToTimeSpan(this string timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return TimeSpan.Zero;
        }

        string[] parts = timeString.Split(':', ',');
        if (parts.Length != 4)
        {
            throw new FormatException($"无效的时间格式: {timeString}");
        }

        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        int seconds = int.Parse(parts[2]);
        int milliseconds = int.Parse(parts[3]);

        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }

    public static string ToSrtTimeString(this TimeSpan timeSpan)
    {
        int hours = (int)timeSpan.TotalHours;
        int minutes = timeSpan.Minutes;
        int seconds = timeSpan.Seconds;
        int milliseconds = timeSpan.Milliseconds;
        return $"{hours:D2}:{minutes:D2}:{seconds:D2},{milliseconds:D3}";
    }

    #endregion

    #region SrtFile 扩展方法

    public static SrtFile FilterByTimeRange(this SrtFile srtFile, TimeSpan startTime, TimeSpan endTime)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var filteredSubtitles = srtFile.Subtitles
            .Where(s => s.StartTime >= startTime && s.EndTime <= endTime)
            .ToList();

        return new SrtFile(filteredSubtitles);
    }

    

    public static SrtFile FilterByText(this SrtFile srtFile, Func<string, bool> predicate)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        var filteredSubtitles = srtFile.Subtitles
            .Where(s => predicate(s.Text))
            .ToList();

        return new SrtFile(filteredSubtitles);
    }

    public static SrtFile ShiftTime(this SrtFile srtFile, TimeSpan offset)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        foreach (var subtitle in srtFile.Subtitles)
        {
            subtitle.StartTime = TimeSpan.FromTicks(Math.Max(0, subtitle.StartTime.Add(offset).Ticks));
            subtitle.EndTime = TimeSpan.FromTicks(Math.Max(0, subtitle.EndTime.Add(offset).Ticks));
        }

        return srtFile;
    }

    public static SrtFile ScaleTime(this SrtFile srtFile, double factor)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        if (factor <= 0)
        {
            throw new ArgumentException("缩放因子必须大于0", nameof(factor));
        }

        foreach (var subtitle in srtFile.Subtitles)
        {
            subtitle.StartTime = TimeSpan.FromMilliseconds(subtitle.StartTime.TotalMilliseconds * factor);
            subtitle.EndTime = TimeSpan.FromMilliseconds(subtitle.EndTime.TotalMilliseconds * factor);
        }

        return srtFile;
    }

    public static SrtFile MergeAdjacent(this SrtFile srtFile, TimeSpan maxGap)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var mergedSubtitles = new List<ISrtSubtitle>();
        var subtitles = srtFile.Subtitles.OrderBy(s => s.StartTime).ToList();

        if (!subtitles.Any())
        {
            return new SrtFile();
        }

        var current = subtitles[0];

        for (int i = 1; i < subtitles.Count; i++)
        {
            var next = subtitles[i];
            var gap = next.StartTime - current.EndTime;

            if (gap <= maxGap)
            {
                current = new SrtSubtitle
                {
                    Index = current.Index,
                    StartTime = current.StartTime,
                    EndTime = next.EndTime,
                    Text = $"{current.Text} {next.Text}"
                };
            }
            else
            {
                mergedSubtitles.Add(current);
                current = next;
            }
        }

        mergedSubtitles.Add(current);

        var result = new SrtFile(mergedSubtitles);
        result.ReindexSubtitles();
        return result;
    }

    public static List<TimeSpan> GetTimeGaps(this SrtFile srtFile)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var gaps = new List<TimeSpan>();
        var subtitles = srtFile.Subtitles.OrderBy(s => s.StartTime).ToList();

        for (int i = 0; i < subtitles.Count - 1; i++)
        {
            var gap = subtitles[i + 1].StartTime - subtitles[i].EndTime;
            gaps.Add(gap);
        }

        return gaps;
    }

    public static SrtFile RemoveEmpty(this SrtFile srtFile)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var filteredSubtitles = srtFile.Subtitles
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .ToList();

        return new SrtFile(filteredSubtitles);
    }

    #endregion

    #region SrtSubtitle 扩展方法

    public static bool Overlaps(this ISrtSubtitle subtitle, ISrtSubtitle other)
    {
        if (subtitle == null || other == null)
        {
            return false;
        }

        return subtitle.StartTime < other.EndTime && subtitle.EndTime > other.StartTime;
    }

    public static bool ContainsTime(this ISrtSubtitle subtitle, TimeSpan time)
    {
        if (subtitle == null)
        {
            return false;
        }

        return time >= subtitle.StartTime && time <= subtitle.EndTime;
    }

    #endregion
}
