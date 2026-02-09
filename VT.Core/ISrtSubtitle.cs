namespace VT.Core;

public interface ISrtSubtitle:ISpeechSegment
{
    string Text { get; set; }
    public string ToSrtTimeString()
    {
        return $"{FormatTime(StartTime)} --> {FormatTime(EndTime)}";
    }

    private string FormatTime(TimeSpan time)
    {
        int hours = (int)time.TotalHours;
        int minutes = time.Minutes;
        int seconds = time.Seconds;
        int milliseconds = time.Milliseconds;
        return $"{hours:D2}:{minutes:D2}:{seconds:D2},{milliseconds:D3}";
    }
}
