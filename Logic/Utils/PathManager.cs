using System.Text.Json;

namespace VideoTranslator.Utils;

public class PathManager
{
    private readonly string _baseDirectory;

    public PathManager(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? 
            Path.Combine(AppContext.BaseDirectory, "Audio.En2Cn", "projects");
    }

    public string BaseDirectory => _baseDirectory;
    public string ProjectsDirectory => Path.Combine(_baseDirectory, "projects");
    public string ScriptsDirectory => Path.Combine(_baseDirectory, "scripts");
    public string FfmpegPath => Path.Combine(_baseDirectory, "ffmpeg", "ffmpeg.exe");

    public string GetProjectDir(string videoId) => Path.Combine(ProjectsDirectory, $"video_{videoId}");
    public string GetSourceDir(string videoId) => Path.Combine(GetProjectDir(videoId), "source");
    public string GetSegmentsDir(string videoId, string langType) => Path.Combine(GetProjectDir(videoId), "segments", langType);
    public string GetProcessedDir(string videoId) => Path.Combine(GetProjectDir(videoId), "processed");
    public string GetOutputDir(string videoId) => Path.Combine(GetProjectDir(videoId), "output");
    public string GetMetadataFile(string videoId) => Path.Combine(GetProjectDir(videoId), "metadata.json");
    public string GetSourceVideo(string videoId, string filename = "1.mp4") => Path.Combine(GetSourceDir(videoId), filename);
    public string GetSourceAudio(string videoId, string langType = "source") => Path.Combine(GetSourceDir(videoId), $"audio_{langType}.wav");
    public string GetSourceSubtitle(string videoId, string langType) => Path.Combine(GetSourceDir(videoId), $"subtitle_{langType}.srt");
    public string GetSegmentFile(string videoId, string langType, int index, int speakerId = 0) => 
        langType == "source" 
            ? Path.Combine(GetSegmentsDir(videoId, langType), $"segment_{index:0000}.wav")
            : Path.Combine(GetSegmentsDir(videoId, langType), $"segment_{index:0000}_speaker_{speakerId}.wav");
    public string GetMergedAudio(string videoId) => Path.Combine(GetProcessedDir(videoId), "merged", "audio_merged.wav");
    public string GetOutputVideo(string videoId, string filename = "1_final.mp4") => Path.Combine(GetOutputDir(videoId), filename);
    public string GetTempFile(string videoId, string filename) => Path.Combine(GetProcessedDir(videoId), "temp", filename);

    public void EnsureDirs(string videoId)
    {
        var dirs = new[]
        {
            GetProjectDir(videoId),
            GetSourceDir(videoId),
            GetSegmentsDir(videoId, "source"),
            GetSegmentsDir(videoId, "target"),
            GetProcessedDir(videoId),
            Path.Combine(GetProcessedDir(videoId), "adjusted"),
            Path.Combine(GetProcessedDir(videoId), "silence"),
            Path.Combine(GetProcessedDir(videoId), "merged"),
            Path.Combine(GetProcessedDir(videoId), "temp"),
            GetOutputDir(videoId)
        };

        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(dir);
        }
    }

    public string[] GetAllSegmentFiles(string videoId, string langType)
    {
        var segmentsDir = GetSegmentsDir(videoId, langType);
        if (!Directory.Exists(segmentsDir))
        {
            return Array.Empty<string>();
        }

        var files = Directory.GetFiles(segmentsDir, "*.wav");
        Array.Sort(files);
        return files;
    }

    public void CleanupTempFiles(string videoId)
    {
        var tempDir = Path.Combine(GetProcessedDir(videoId), "temp");
        if (Directory.Exists(tempDir))
        {
            foreach (var file in Directory.GetFiles(tempDir))
            {
                File.Delete(file);
            }
        }

        var adjustedDir = Path.Combine(GetProcessedDir(videoId), "adjusted");
        if (Directory.Exists(adjustedDir))
        {
            foreach (var file in Directory.GetFiles(adjustedDir))
            {
                File.Delete(file);
            }
        }

        var silenceDir = Path.Combine(GetProcessedDir(videoId), "silence");
        if (Directory.Exists(silenceDir))
        {
            foreach (var file in Directory.GetFiles(silenceDir))
            {
                File.Delete(file);
            }
        }
    }
}
