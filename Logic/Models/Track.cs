namespace VideoTranslator.Models;

public class Track
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public TrackType Type { get; set; }
    public bool IsMuted { get; set; }
    public bool IsVisible { get; set; } = true;
    public double Volume { get; set; } = 1.0;
    public List<Clip> Clips { get; set; } = new();
    public SubtitleLanguage? SubtitleLanguage { get; set; }
    public int Order { get; set; }
    public string Color { get; set; } = "#7e6fff";
}

public enum TrackType
{
    Video,
    Audio,
    Subtitle
}

public enum SubtitleLanguage
{
    English,
    Chinese
}
