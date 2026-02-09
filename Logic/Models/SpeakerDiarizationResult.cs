using VideoTranslator.Interfaces;

namespace VideoTranslator.Models;

public class SpeakerSegment
{
    public double Start { get; set; }
    public double End { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public TimeSpan StartTime => TimeSpan.FromSeconds(Start);
    public TimeSpan EndTime => TimeSpan.FromSeconds(End);
    public double Duration => End - Start;
}

public class SpeakerDiarizationResult
{
    public List<SpeakerSegment> Segments { get; set; } = new();
    public int SpeakerCount => Segments.Select(s => s.Speaker).Distinct().Count();
    public double TotalDuration => Segments.Count > 0 ? Segments.Max(s => s.End) : 0;
}
