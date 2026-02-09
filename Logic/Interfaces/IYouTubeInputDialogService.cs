using VideoTranslator.Models;

namespace VideoTranslator.Interfaces;

public interface IYouTubeInputDialogService
{
    Task<YouTubeDownloadSelection?> ShowUrlInputDialogAsync();
}
