namespace VideoTranslator.Models;

public class VideoEditor
{
    public string EditorId { get; set; } = Guid.NewGuid().ToString();
    public string ProjectName { get; set; } = string.Empty;
    public string? SourceVideoPath { get; set; }
    public string? SourceAudioPath { get; set; }
    public string? SourceSubtitlePath { get; set; }
    public string? TargetSubtitlePath { get; set; }
    public string? TargetAudioSegmentsPath { get; set; }
    public string? OutputVideoPath { get; set; }
    public string? OutputAudioPath { get; set; }
    public List<AudioTrack> AudioTracks { get; set; } = new();
    public List<SubtitleTrack> SubtitleTracks { get; set; } = new();
    public VideoStatus Status { get; set; } = VideoStatus.Idle;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public enum VideoStatus
{
    Idle,
    Downloading,
    Processing,
    Completed,
    Error
}
