namespace VideoTranslator.Models;

public class AudioTrack
{
    public string TrackId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public AudioTrackType Type { get; set; }
    public AudioInfo? Info { get; set; }
    public bool IsMuted { get; set; }
    public double Volume { get; set; } = 1.0;
    public bool IsActive { get; set; } = true;
}

public enum AudioTrackType
{
    Source,
    Translated,
    Background,
    Effect
}
