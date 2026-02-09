using System;
using System.Text.RegularExpressions;

namespace VideoTranslator.Utils;

public class FFmpegProgressParser
{
    #region 字段和属性

    private TimeSpan? _totalDuration;
    private TimeSpan? _currentTime;
    private readonly Regex _durationRegex = new Regex(@"Duration:\s+(\d{2}):(\d{2}):(\d{2}\.\d{2})", RegexOptions.Compiled);
    private readonly Regex _progressRegex = new Regex(@"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})", RegexOptions.Compiled);

    public TimeSpan? TotalDuration => _totalDuration;

    public TimeSpan? CurrentTime => _currentTime;

    public double? ProgressPercentage
    {
        get
        {
            if (_totalDuration.HasValue && _currentTime.HasValue && _totalDuration.Value.TotalSeconds > 0)
            {
                return (_currentTime.Value.TotalSeconds / _totalDuration.Value.TotalSeconds) * 100;
            }
            return null;
        }
    }

    #endregion

    #region 公共方法

    public void ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var durationMatch = _durationRegex.Match(line);
        if (durationMatch.Success && !_totalDuration.HasValue)
        {
            var hours = int.Parse(durationMatch.Groups[1].Value);
            var minutes = int.Parse(durationMatch.Groups[2].Value);
            var seconds = double.Parse(durationMatch.Groups[3].Value);
            _totalDuration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        var progressMatch = _progressRegex.Match(line);
        if (progressMatch.Success)
        {
            var hours = int.Parse(progressMatch.Groups[1].Value);
            var minutes = int.Parse(progressMatch.Groups[2].Value);
            var seconds = double.Parse(progressMatch.Groups[3].Value);
            _currentTime = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }
    }

    public void Reset()
    {
        _totalDuration = null;
        _currentTime = null;
    }

    #endregion
}
