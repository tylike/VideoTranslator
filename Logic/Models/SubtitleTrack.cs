namespace VideoTranslator.Models;

public class SubtitleTrack
{
    public string TrackId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public SubtitleTrackType Type { get; set; }
    public List<Subtitle> Subtitles { get; set; } = new();
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public enum SubtitleTrackType
{
    Source,
    Translated,
    Custom
}
