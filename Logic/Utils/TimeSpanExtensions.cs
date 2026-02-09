using System;

namespace VideoTranslator.Utils;

public static class TimeSpanExtensions
{
    public static string FormatTimeSpan(this TimeSpan? ts)
    {
        if (!ts.HasValue) return "-";
        var t = ts.Value;
        if (t.TotalHours >= 1)
            return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        else
            return $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    public static string FormatTimeSpan(this TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        else
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    public static string FormatTimeSpanMs(this TimeSpan? ts)
    {
        if (!ts.HasValue) return "-";
        var t = ts.Value;
        if (t.TotalHours >= 1)
            return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}";
        else
            return $"{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}";
    }

    public static string FormatTimeSpanMs(this TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        else
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    public static string ToSrtTime(this TimeSpan timeSpan)
    {
        return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2},{timeSpan.Milliseconds:D3}";
    }

    public static double SrtTimeToSeconds(this string timeStr)
    {
        var parts = timeStr.Split(',');
        var timePart = parts[0];
        var milliseconds = parts.Length > 1 ? int.Parse(parts[1]) : 0;

        var timeComponents = timePart.Split(':');
        var hours = int.Parse(timeComponents[0]);
        var minutes = int.Parse(timeComponents[1]);
        var seconds = double.Parse(timeComponents[2]);

        return hours * 3600 + minutes * 60 + seconds + milliseconds / 1000.0;
    }

    public static string ToSrtTimeString(this double seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2},{timeSpan.Milliseconds:D3}";
    }
}
