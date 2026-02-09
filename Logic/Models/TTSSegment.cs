namespace VideoTranslator.Models;

public class TTSSegment
{
    public int Index { get; set; }
    public string TextTarget { get; set; } = string.Empty;
    public double Start { get; set; }
    public double End { get; set; }
    public double Duration { get; set; }
    public int SpeakerId { get; set; }
    public string AudioPath { get; set; } = string.Empty;
}
