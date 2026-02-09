using WhisperCliApi.Enums;
using WhisperCliApi.Models;

namespace WhisperCliApi.Extensions;

public static class WhisperOptionsExtensions
{
    #region 输出格式扩展

    public static WhisperOptions WithJsonOutput(this WhisperOptions options, bool full = true)
    {
        options.OutputFormats.Add(full ? OutputFormat.JsonFull : OutputFormat.Json);
        return options;
    }

    public static WhisperOptions WithSrtOutput(this WhisperOptions options)
    {
        options.OutputFormats.Add(OutputFormat.Srt);
        return options;
    }

    public static WhisperOptions WithVttOutput(this WhisperOptions options)
    {
        options.OutputFormats.Add(OutputFormat.Vtt);
        return options;
    }

    public static WhisperOptions WithTextOutput(this WhisperOptions options)
    {
        options.OutputFormats.Add(OutputFormat.Text);
        return options;
    }

    public static WhisperOptions WithAllOutputs(this WhisperOptions options)
    {
        options.OutputFormats.AddRange(new[]
        {
            OutputFormat.JsonFull,
            OutputFormat.Srt,
            OutputFormat.Vtt,
            OutputFormat.Text
        });
        return options;
    }

    #endregion

    #region 语言扩展

    public static WhisperOptions WithLanguage(this WhisperOptions options, Language language)
    {
        options.Language = language;
        return options;
    }

    public static WhisperOptions WithAutoLanguage(this WhisperOptions options)
    {
        options.Language = Language.Auto;
        return options;
    }

    #endregion

    #region VAD 扩展

    public static WhisperOptions EnableVad(this WhisperOptions options, double threshold = 0.5)
    {
        options.EnableVad = true;
        options.VadThreshold = threshold;
        return options;
    }

    public static WhisperOptions WithVadSettings(this WhisperOptions options, double threshold, int minSpeechDurationMs = 250, int minSilenceDurationMs = 100)
    {
        options.EnableVad = true;
        options.VadThreshold = threshold;
        options.VadMinSpeechDurationMs = minSpeechDurationMs;
        options.VadMinSilenceDurationMs = minSilenceDurationMs;
        return options;
    }

    #endregion

    #region 翻译扩展

    public static WhisperOptions EnableTranslation(this WhisperOptions options)
    {
        options.Translate = true;
        return options;
    }

    #endregion

    #region 说话人分离扩展

    public static WhisperOptions EnableDiarization(this WhisperOptions options)
    {
        options.Diarize = true;
        return options;
    }

    public static WhisperOptions EnableTinyDiarization(this WhisperOptions options)
    {
        options.TinyDiarize = true;
        return options;
    }

    #endregion

    #region 性能扩展

    public static WhisperOptions WithBestOf(this WhisperOptions options, int bestOf)
    {
        options.BestOf = bestOf;
        return options;
    }

    public static WhisperOptions WithBeamSize(this WhisperOptions options, int beamSize)
    {
        options.BeamSize = beamSize;
        return options;
    }

    public static WhisperOptions WithTemperature(this WhisperOptions options, double temperature)
    {
        options.Temperature = temperature;
        return options;
    }

    public static WhisperOptions DisableGpu(this WhisperOptions options)
    {
        options.NoGpu = true;
        return options;
    }

    #endregion

    #region 高级扩展

    public static WhisperOptions WithPrompt(this WhisperOptions options, string prompt)
    {
        options.Prompt = prompt;
        return options;
    }

    public static WhisperOptions WithMaxSegmentLength(this WhisperOptions options, int maxLength)
    {
        options.MaxLength = maxLength;
        return options;
    }

    public static WhisperOptions WithSplitOnWord(this WhisperOptions options)
    {
        options.SplitOnWord = true;
        return options;
    }

    #endregion
}
