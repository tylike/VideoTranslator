using VT.Core;

namespace VideoTranslator.Models;

public class YouTubeDownloadSelection
{
    public string Url { get; set; } = string.Empty;
    public bool DownloadVideo { get; set; }
    public bool DownloadAudio { get; set; }
    public List<string> SelectedSubtitleLanguages { get; set; } = new();
    public YouTubeVideoStream? SelectedVideoStream { get; set; }
    public YouTubeAudio? SelectedAudioStream { get; set; }
    public YouTubeVideo? VideoInfo { get; set; }
    public Language SourceLanguage { get; set; } = Language.Auto;
    public Language TargetLanguage { get; set; } = Language.Chinese;
}
