using System;

namespace VT.Win.Forms;

public static class TimeFormatter
{
    #region Public Methods

    public static string FormatTime(int seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);

        if (time.Hours == 0 && time.Minutes == 0)
        {
            return time.ToString(@"ss\.fff");
        }
        else if (time.Hours == 0)
        {
            return time.ToString(@"mm\:ss");
        }
        else
        {
            return time.ToString(@"hh\:mm\:ss");
        }
    }

    public static int CalculateMarkerInterval(double totalDurationSeconds)
    {
        if (totalDurationSeconds <= 10) return 1;
        if (totalDurationSeconds <= 30) return 2;
        if (totalDurationSeconds <= 60) return 5;
        if (totalDurationSeconds <= 120) return 10;
        if (totalDurationSeconds <= 300) return 20;
        if (totalDurationSeconds <= 600) return 30;
        return 60;
    }

    public static int GetNextInterval(int currentInterval)
    {
        int[] intervals = { 1, 2, 5, 10, 15, 20, 30, 60, 120, 300, 600, 1800, 3600 };
        for (int i = 0; i < intervals.Length - 1; i++)
        {
            if (intervals[i] == currentInterval)
            {
                return intervals[i + 1];
            }
        }
        return 3600;
    }

    #endregion
}
