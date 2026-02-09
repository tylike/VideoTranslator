namespace VideoTranslator.Models;

public class YouTubeVideoStream
{
    public string FormatId { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string QualityLabel { get; set; } = string.Empty;
    public int? Framerate { get; set; }
    public long FileSize { get; set; }
    public string? DownloadUrl { get; set; }
}
