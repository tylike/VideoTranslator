namespace VideoTranslator.Models;

public class ProjectResource
{
    public string ResourceId { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsUsed { get; set; }
    public string? Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
}

public enum ResourceType
{
    Video,
    Audio,
    Image,
    Subtitle,
    Other
}