namespace VideoTranslator.Models;

public class AudioInfo
{
    public double DurationSeconds { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public string FilePath { get; set; } = string.Empty;
}
