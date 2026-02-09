namespace VideoTranslator.Models;

public class Clip
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double Duration => EndTime - StartTime;
    public string? FilePath { get; set; }
    public string? Content { get; set; }
    public bool IsMuted { get; set; }
    public bool IsVisible { get; set; } = true;
    public double Volume { get; set; } = 1.0;
    public string? OriginalContent { get; set; }
    public double? OriginalDuration { get; set; }
    public bool IsModified => OriginalDuration.HasValue && OriginalDuration != Duration;
    public string? AudioFilePath { get; set; }
    public bool IsGenerated { get; set; }
    public string? TranslatedContent { get; set; }
}
