using VideoTranslator.Interfaces;
using WhisperCliApi.Enums;
using WhisperCliApi.Models;

namespace WhisperCliApi.Interfaces;

public interface IWhisperService
{
    Task<WhisperResult> RecognizeAudioAsync(string audioPath, string outputPath, string language = "en");
}

public interface IWhisperServiceAdvanced : IWhisperService
{
    Task<WhisperResult> RecognizeAsync(WhisperOptions options);
}
