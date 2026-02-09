using VideoTranslator.Interfaces;
using WhisperCliApi.Enums;
using WhisperCliApi.Interfaces;
using WhisperCliApi.Models;

namespace WhisperCliApi.Services;

public class WhisperService : IWhisperServiceAdvanced
{
    private readonly WhisperCliClient _client;

    public WhisperService(string whisperExecutablePath, IProgressService? progress = null)
    {
        _client = new WhisperCliClient(whisperExecutablePath, progress);
    }

    #region 基础方法

    public async Task<WhisperResult> RecognizeAudioAsync(string audioPath, string outputPath, string language = "en")
    {
        var options = new WhisperOptions
        {
            AudioFilePath = audioPath,
            OutputFilePath = outputPath,
            OutputFormats = { OutputFormat.JsonFull },
            Language = ParseLanguage(language)
        };

        return await RecognizeAsync(options);
    }

    #endregion

    #region 高级方法

    public async Task<WhisperResult> RecognizeAsync(WhisperOptions options)
    {
        return await _client.RecognizeAsync(options);
    }

    #endregion

    #region 辅助方法

    private Language ParseLanguage(string language)
    {
        if (string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return Language.Auto;
        }

        return Enum.TryParse<Language>(language, true, out var lang) ? lang : Language.English;
    }

    #endregion
}
