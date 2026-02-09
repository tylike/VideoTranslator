using VideoTranslator.Interfaces;
using WhisperCliApi.Enums;
using WhisperCliApi.Extensions;
using WhisperCliApi.Models;
using WhisperCliApi.Services;

namespace WhisperCliApi.Examples;

public class UsageExamples
{
    private readonly IProgressService? _progress;

    public UsageExamples(IProgressService? progress = null)
    {
        _progress = progress;
    }

    #region 基础使用示例

    public async Task<WhisperResult> BasicExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";
        var audioPath = @"D:\audio\test.wav";
        var outputPath = @"D:\output\result.srt";

        var service = new WhisperService(whisperPath, _progress);
        var result = await service.RecognizeAudioAsync(audioPath, outputPath, "en");

        return result;
    }

    #endregion

    #region 使用 WhisperOptions 的示例

    public async Task<WhisperResult> AdvancedExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

        var service = new WhisperService(whisperPath, _progress);

        var options = new WhisperOptions
        {
            AudioFilePath = @"D:\audio\test.wav",
            ModelPath = @"D:\models\ggml-base.en.bin",
            OutputFilePath = @"D:\output\result",
            Language = Language.Chinese,
            OutputFormats = { OutputFormat.JsonFull, OutputFormat.Srt }
        };

        var result = await service.RecognizeAsync(options);
        return result;
    }

    #endregion

    #region 使用扩展方法的示例

    public async Task<WhisperResult> FluentApiExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

        var service = new WhisperService(whisperPath, _progress);

        var options = new WhisperOptions()
            .WithLanguage(Language.Chinese)
            .WithJsonOutput()
            .WithSrtOutput()
            .EnableVad(0.5)
            .WithMaxSegmentLength(50);

        options.AudioFilePath = @"D:\audio\test.wav";
        options.ModelPath = @"D:\models\ggml-base.en.bin";
        options.OutputFilePath = @"D:\output\result";

        var result = await service.RecognizeAsync(options);
        return result;
    }

    #endregion

    #region VAD 启用示例

    public async Task<WhisperResult> VadExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

        var service = new WhisperService(whisperPath, _progress);

        var options = new WhisperOptions
        {
            AudioFilePath = @"D:\audio\test.wav",
            ModelPath = @"D:\models\ggml-base.en.bin",
            OutputFilePath = @"D:\output\result",
            Language = Language.Auto
        };

        options.EnableVad(0.5);

        var result = await service.RecognizeAsync(options);
        return result;
    }

    #endregion

    #region 翻译示例

    public async Task<WhisperResult> TranslationExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

        var service = new WhisperService(whisperPath, _progress);

        var options = new WhisperOptions
        {
            AudioFilePath = @"D:\audio\test.wav",
            ModelPath = @"D:\models\ggml-base.en.bin",
            OutputFilePath = @"D:\output\result",
            Language = Language.Chinese,
            OutputFormats = { OutputFormat.JsonFull, OutputFormat.Srt }
        };

        options.EnableTranslation();

        var result = await service.RecognizeAsync(options);
        return result;
    }

    #endregion

    #region 说话人分离示例

    public async Task<WhisperResult> DiarizationExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

        var service = new WhisperService(whisperPath, _progress);

        var options = new WhisperOptions
        {
            AudioFilePath = @"D:\audio\test.wav",
            ModelPath = @"D:\models\ggml-base.en.bin",
            OutputFilePath = @"D:\output\result",
            Language = Language.Auto,
            OutputFormats = { OutputFormat.JsonFull, OutputFormat.Srt }
        };

        options.EnableDiarization();

        var result = await service.RecognizeAsync(options);
        return result;
    }

    #endregion

    #region 性能优化示例

    public async Task<WhisperResult> PerformanceExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

        var service = new WhisperService(whisperPath, _progress);

        var options = new WhisperOptions
        {
            AudioFilePath = @"D:\audio\test.wav",
            ModelPath = @"D:\models\ggml-base.en.bin",
            OutputFilePath = @"D:\output\result",
            Language = Language.Auto,
            OutputFormats = { OutputFormat.JsonFull }
        };

        options.WithBestOf(5)
               .WithBeamSize(5)
               .WithTemperature(0.0);

        var result = await service.RecognizeAsync(options);
        return result;
    }

    #endregion

    #region 完整配置示例

    public async Task<WhisperResult> FullConfigExample()
    {
        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

        var service = new WhisperService(whisperPath, _progress);

        var options = new WhisperOptions
        {
            AudioFilePath = @"D:\audio\test.wav",
            ModelPath = @"D:\models\ggml-base.en.bin",
            OutputFilePath = @"D:\output\result",
            Language = Language.Auto,
            OutputFormats = { OutputFormat.JsonFull, OutputFormat.Srt, OutputFormat.Vtt },
            MaxLength = 50,
            MaxContext = 0,
            SplitOnWord = false,
            WordThreshold = 0.01,
            NoSpeechThreshold = 0.6,
            PrintProgress = true
        };

        options.EnableVad(0.5)
               .WithVadSettings(0.5, 250, 100);

        var result = await service.RecognizeAsync(options);
        return result;
    }

    #endregion
}
