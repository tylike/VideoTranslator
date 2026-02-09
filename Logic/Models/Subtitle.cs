using VideoTranslator.Utils;

namespace VideoTranslator.Models;

[Obsolete]
public class Subtitle
{
    public int Index { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;

    public double StartSeconds => string.IsNullOrEmpty(StartTime) ? 0 : StartTime.SrtTimeToSeconds();
    public double EndSeconds => string.IsNullOrEmpty(EndTime) ? 0 : EndTime.SrtTimeToSeconds();
    public double Duration => EndSeconds - StartSeconds;
}
