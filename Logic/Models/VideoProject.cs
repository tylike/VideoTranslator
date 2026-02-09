using VideoTranslator.Interfaces;

namespace VideoTranslator.Models;

public class VideoProject
{
    public string ProjectId { get; set; } = Guid.NewGuid().ToString();
    public string ProjectName { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public string BaseDirectory { get; set; } = string.Empty;
    public string? SourceVideoPath { get; set; }
    public string? SourceAudioPath { get; set; }
    public string? SourceVocalAudioPath { get; set; }
    public string? SourceBackgroundAudioPath { get; set; }
    public string? SourceSubtitlePath { get; set; }
    public string? TargetSubtitlePath { get; set; }
    public string? TargetAudioSegmentsPath { get; set; }
    public string? OutputVideoPath { get; set; }
    public string? OutputAudioPath { get; set; }
    public string BaseAudioPath { get; set; } = string.Empty;
    public string SubtitlePath { get; set; } = string.Empty;
    public string SegmentsDirectory { get; set; } = string.Empty;
    public string SegmentsTargetDirectory { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string ProcessedDirectory { get; set; } = string.Empty;
    public string AdjustedDirectory { get; set; } = string.Empty;
    public string SilenceDirectory { get; set; } = string.Empty;
    public string TempDirectory { get; set; } = string.Empty;
    public List<AudioTrack> AudioTracks { get; set; } = new();
    public List<SubtitleTrack> SubtitleTracks { get; set; } = new();
    public List<Track> Tracks { get; set; } = new();
    public List<TTSSegment> ChineseAudioSegments { get; set; } = new();
    public List<AudioSegmentAdjustment> AudioSegmentAdjustments { get; set; } = new();
    public List<ProjectResource> Resources { get; set; } = new();
    public AudioVerificationResult? AudioVerificationResult { get; set; }
    public VideoStatus Status { get; set; } = VideoStatus.Idle;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public bool KeepFiles { get; set; } = true;
    public int TranslationLimit { get; set; } = 20;
    public double Duration { get; set; }
    public double CurrentTime { get; set; }
    public double ZoomLevel { get; set; } = 1.0;
    public string? VideoUrl { get; set; }

    public Track? GetTrack(string trackId)
    {
        return Tracks.FirstOrDefault(t => t.Id == trackId);
    }

    public Clip? GetClip(string clipId)
    {
        foreach (var track in Tracks)
        {
            var clip = track.Clips.FirstOrDefault(c => c.Id == clipId);
            if (clip != null) return clip;
        }
        return null;
    }

    public ProjectResource? GetResource(string resourceId)
    {
        return Resources.FirstOrDefault(r => r.ResourceId == resourceId);
    }

    public ProjectResource? GetResourceByFilePath(string filePath)
    {
        return Resources.FirstOrDefault(r => r.FilePath == filePath);
    }

    public void AddResource(ProjectResource resource)
    {
        Resources.Add(resource);
    }

    public void RemoveResource(string resourceId)
    {
        var resource = GetResource(resourceId);
        if (resource != null)
        {
            Resources.Remove(resource);
        }
    }
    IProgressService? progress;
    public void ImportFromFile(string sourceFilePath)
    {
        if (string.IsNullOrEmpty(sourceFilePath))
        {
            throw new ArgumentException("Source file path cannot be empty", nameof(sourceFilePath));
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
        }

        if (string.IsNullOrEmpty(BaseDirectory))
        {
            throw new InvalidOperationException("BaseDirectory is not set. Please set BaseDirectory before importing files.");
        }

        var sourceDir = Path.Combine(BaseDirectory, "source");
        Directory.CreateDirectory(sourceDir);

        var fileName = Path.GetFileName(sourceFilePath);
        var destPath = Path.Combine(sourceDir, fileName);

        File.Copy(sourceFilePath, destPath, overwrite: true);
        SourceVideoPath = destPath;

        var projectDir = BaseDirectory;
        var originalFilePath = Path.Combine(projectDir, fileName);
        if (File.Exists(originalFilePath) && !Path.GetFullPath(originalFilePath).Equals(Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.Delete(originalFilePath);
            }
            catch (Exception ex)
            {
                progress?.Warning($"[Warning] Failed to delete original file: {originalFilePath}, Error: {ex.Message}");
            }
        }
    }
}

public class AudioSegmentAdjustment
{
    public int SegmentIndex { get; set; }
    public double EnglishDuration { get; set; }
    public double ChineseDuration { get; set; }
    public double DurationDifference { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public double? TempoFactor { get; set; }
    public double? SilenceAdded { get; set; }
    public string AdjustedFilePath { get; set; } = string.Empty;
    public bool IsAdjusted { get; set; }
}

public class AudioVerificationResult
{
    public DateTime VerificationTime { get; set; } = DateTime.Now;
    public int TotalSegments { get; set; }
    public int VerifiedSegments { get; set; }
    public int MatchedSegments { get; set; }
    public int MismatchedSegments { get; set; }
    public double MatchRate { get; set; }
    public string VerifiedSrtPath { get; set; } = string.Empty;
    public List<VerificationDetail> Details { get; set; } = new();
}

public class VerificationDetail
{
    public int SegmentIndex { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string EnglishText { get; set; } = string.Empty;
    public string RecognizedText { get; set; } = string.Empty;
    public bool IsMatch { get; set; }
}
