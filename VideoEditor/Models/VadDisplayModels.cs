using System;
using System.Collections.Generic;
using System.Linq;

namespace VideoEditor.Models;

public static class TimeFormatter
{
    public static string FormatDuration(decimal seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds:F1}秒";
        }

        var totalSeconds = (double)seconds;
        var minutes = (int)(totalSeconds / 60);
        var remainingSeconds = totalSeconds - minutes * 60;

        return $"{minutes}分{remainingSeconds:F1}秒";
    }

    public static string FormatDurationShort(decimal seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds:F0}秒";
        }

        var totalSeconds = (double)seconds;
        var minutes = (int)(totalSeconds / 60);
        var remainingSeconds = totalSeconds - minutes * 60;

        return $"{minutes}分{remainingSeconds:F0}秒";
    }
}

public class VadSegmentDisplay
{
    public int Index { get; set; }
    public decimal Start { get; set; }
    public decimal End { get; set; }
    public decimal Duration { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class SplitSchemeSegment
{
    public int SegmentIndex { get; set; }
    public decimal StartTime { get; set; }
    public decimal EndTime { get; set; }
    public decimal Duration { get; set; }
    public decimal? SplitSilenceStart { get; set; }
    public decimal? SplitSilenceEnd { get; set; }
    public decimal? SplitSilenceDuration { get; set; }
}

public class SplitScheme
{
    public int Index { get; set; }
    public int SegmentCount { get; set; }
    public decimal AverageDuration { get; set; }
    public decimal MinSilenceDuration { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<decimal> SplitPoints { get; set; } = new();
    public List<SplitSchemeSegment> Segments { get; set; } = new();
}

public class VadResultSummary
{
    public decimal AudioDuration { get; set; }
    public int SpeechSegmentCount { get; set; }
    public int SilenceSegmentCount { get; set; }
    public decimal TotalSpeechDuration { get; set; }
    public decimal TotalSilenceDuration { get; set; }
    public string DurationText { get; set; } = string.Empty;
    public string SpeechPercentage { get; set; } = string.Empty;
}
