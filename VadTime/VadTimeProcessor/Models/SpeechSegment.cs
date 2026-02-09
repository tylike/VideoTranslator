using VT.Core;

namespace VadTimeProcessor.Models;

public class SpeechSegment : ISpeechSegment
{
    #region Constructors

    public SpeechSegment(int index, double start, double end)
    {
        Index = index;
        this.StartMS = start;
        this.EndMS = end;
    }

    public int Index { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public ISpeechSegment? Next { get; set; }
    public ISpeechSegment? Previous { get; set; }

    #endregion

    public override string ToString()
    {
        return $"{Index}: {this.StartMS / 1000:F2}s - {this.EndMS / 1000:F2}s (时长: {this.DurationMS / 1000:F2}s)";
    }
}
