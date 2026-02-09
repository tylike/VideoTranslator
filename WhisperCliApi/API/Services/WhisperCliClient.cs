using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using VideoTranslator.Interfaces;
using WhisperCliApi.Enums;
using WhisperCliApi.Models;

namespace WhisperCliApi.Services;

public class WhisperCliClient
{
    private readonly string _whisperExecutablePath;
    private readonly IProgressService? _progress;

    public WhisperCliClient(string whisperExecutablePath, IProgressService? progress = null)
    {
        _whisperExecutablePath = whisperExecutablePath;
        _progress = progress;
    }

    #region 公共方法

    public async Task<WhisperResult> RecognizeAsync(WhisperOptions options)
    {
        _progress?.Report("[WhisperCliClient] 开始语音识别任务");
        _progress?.Report(new string('=', 60));

        ValidateOptions(options);

        var args = BuildArguments(options);
        _progress?.Report($"命令参数: {args}");

        var result = await ExecuteWhisperAsync(args);

        if (result.Success)
        {
            CollectGeneratedFiles(options, result);
        }

        return result;
    }

    #endregion

    #region 参数验证

    private void ValidateOptions(WhisperOptions options)
    {
        if (string.IsNullOrEmpty(options.AudioFilePath))
        {
            throw new ArgumentException("必须指定音频文件路径", nameof(options.AudioFilePath));
        }

        if (!File.Exists(options.AudioFilePath))
        {
            throw new FileNotFoundException($"音频文件不存在: {options.AudioFilePath}");
        }

        if (string.IsNullOrEmpty(options.ModelPath))
        {
            throw new ArgumentException("必须指定模型文件路径", nameof(options.ModelPath));
        }

        if (!File.Exists(options.ModelPath))
        {
            throw new FileNotFoundException($"模型文件不存在: {options.ModelPath}");
        }

        if (!string.IsNullOrEmpty(options.OutputFilePath))
        {
            var outputDir = Path.GetDirectoryName(options.OutputFilePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }
    }

    #endregion

    #region 参数构建

    private string BuildArguments(WhisperOptions options)
    {
        var args = new StringBuilder();

        #region 输入输出参数

        args.Append($"-m \"{options.ModelPath}\"");
        args.Append($" -f \"{options.AudioFilePath}\"");

        if (!string.IsNullOrEmpty(options.OutputFilePath))
        {
            var outputDir = Path.GetDirectoryName(options.OutputFilePath);
            var outputBase = Path.GetFileNameWithoutExtension(options.OutputFilePath);
            args.Append($" -of \"{Path.Combine(outputDir ?? "", outputBase)}\"");
        }

        foreach (var format in options.OutputFormats)
        {
            args.Append($" {GetFormatArgument(format)}");
        }

        if (options.NoPrints)
        {
            args.Append(" -np");
        }

        if (options.PrintProgress)
        {
            args.Append(" -pp");
        }

        #endregion

        #region 模型参数

        if (options.Language != Language.Auto)
        {
            args.Append($" -l {GetLanguageCode(options.Language)}");
        }

        if (options.DetectLanguage)
        {
            args.Append(" -dl");
        }

        if (!string.IsNullOrEmpty(options.Prompt))
        {
            args.Append($" --prompt \"{options.Prompt}\"");
        }

        if (options.CarryInitialPrompt)
        {
            args.Append(" --carry-initial-prompt");
        }

        #endregion

        #region 音频处理参数

        if (options.AudioContext.HasValue)
        {
            args.Append($" -ac {options.AudioContext.Value}");
        }

        if (options.Offset.HasValue)
        {
            args.Append($" -ot {options.Offset.Value}");
        }

        if (options.Duration.HasValue)
        {
            args.Append($" -d {options.Duration.Value}");
        }

        if (options.MaxContext.HasValue)
        {
            args.Append($" -mc {options.MaxContext.Value}");
        }

        if (options.MaxLength.HasValue)
        {
            args.Append($" -ml {options.MaxLength.Value}");
        }

        if (options.SplitOnWord)
        {
            args.Append(" -sow");
        }

        #endregion

        #region 解码参数

        if (options.BestOf.HasValue)
        {
            args.Append($" -bo {options.BestOf.Value}");
        }

        if (options.BeamSize.HasValue)
        {
            args.Append($" -bs {options.BeamSize.Value}");
        }

        if (options.Temperature.HasValue)
        {
            args.Append($" -tp {options.Temperature.Value:F2}");
        }

        if (options.TemperatureInc.HasValue)
        {
            args.Append($" -tpi {options.TemperatureInc.Value:F2}");
        }

        if (options.WordThreshold.HasValue)
        {
            args.Append($" -wt {options.WordThreshold.Value:F2}");
        }

        if (options.EntropyThreshold.HasValue)
        {
            args.Append($" -et {options.EntropyThreshold.Value:F2}");
        }

        if (options.LogProbThreshold.HasValue)
        {
            args.Append($" -lpt {options.LogProbThreshold.Value:F2}");
        }

        if (options.NoSpeechThreshold.HasValue)
        {
            args.Append($" -nth {options.NoSpeechThreshold.Value:F2}");
        }

        if (options.NoFallback)
        {
            args.Append(" -nf");
        }

        if (options.Translate)
        {
            args.Append(" -tr");
        }

        #endregion

        #region 高级参数

        if (options.DebugMode)
        {
            args.Append(" -debug");
        }

        if (options.Diarize)
        {
            args.Append(" -di");
        }

        if (options.TinyDiarize)
        {
            args.Append(" -tdrz");
        }

        if (options.PrintSpecial)
        {
            args.Append(" -ps");
        }

        if (options.PrintColors)
        {
            args.Append(" -pc");
        }

        if (options.PrintConfidence)
        {
            args.Append(" --print-confidence");
        }

        if (options.NoTimestamps)
        {
            args.Append(" -nt");
        }

        if (options.LogScore)
        {
            args.Append(" -ls");
        }

        if (options.NoGpu)
        {
            args.Append(" -ng");
        }

        if (!options.FlashAttention)
        {
            args.Append(" -nfa");
        }

        if (options.SuppressNonSpeechTokens)
        {
            args.Append(" -sns");
        }

        if (!string.IsNullOrEmpty(options.SuppressRegex))
        {
            args.Append($" --suppress-regex \"{options.SuppressRegex}\"");
        }

        if (!string.IsNullOrEmpty(options.Grammar))
        {
            args.Append($" --grammar \"{options.Grammar}\"");
        }

        if (!string.IsNullOrEmpty(options.GrammarRule))
        {
            args.Append($" --grammar-rule \"{options.GrammarRule}\"");
        }

        if (options.GrammarPenalty.HasValue)
        {
            args.Append($" --grammar-penalty {options.GrammarPenalty.Value:F1}");
        }

        #endregion

        #region VAD 参数

        if (options.EnableVad)
        {
            args.Append(" --vad");
        }

        if (!string.IsNullOrEmpty(options.VadModelPath))
        {
            args.Append($" -vm \"{options.VadModelPath}\"");
        }

        if (options.VadThreshold.HasValue)
        {
            args.Append($" -vt {options.VadThreshold.Value:F2}");
        }

        if (options.VadMinSpeechDurationMs.HasValue)
        {
            args.Append($" -vspd {options.VadMinSpeechDurationMs.Value}");
        }

        if (options.VadMinSilenceDurationMs.HasValue)
        {
            args.Append($" -vsd {options.VadMinSilenceDurationMs.Value}");
        }

        if (options.VadMaxSpeechDurationS.HasValue)
        {
            args.Append($" -vmsd {options.VadMaxSpeechDurationS.Value}");
        }

        if (options.VadSpeechPadMs.HasValue)
        {
            args.Append($" -vp {options.VadSpeechPadMs.Value}");
        }

        if (options.VadSamplesOverlap.HasValue)
        {
            args.Append($" -vo {options.VadSamplesOverlap.Value:F2}");
        }

        #endregion

        #region 其他参数

        if (!string.IsNullOrEmpty(options.FontPath))
        {
            args.Append($" -fp \"{options.FontPath}\"");
        }

        if (!string.IsNullOrEmpty(options.OpenVinoDevice))
        {
            args.Append($" -oved {options.OpenVinoDevice}");
        }

        if (!string.IsNullOrEmpty(options.DtwModel))
        {
            args.Append($" -dtw {options.DtwModel}");
        }

        #endregion

        return args.ToString();
    }

    private string GetFormatArgument(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Text => "-otxt",
            OutputFormat.Vtt => "-ovtt",
            OutputFormat.Srt => "-osrt",
            OutputFormat.Lrc => "-olrc",
            OutputFormat.Words => "-owts",
            OutputFormat.Csv => "-ocsv",
            OutputFormat.Json => "-oj",
            OutputFormat.JsonFull => "-ojf",
            _ => string.Empty
        };
    }

    private string GetLanguageCode(Language language)
    {
        return language switch
        {
            Language.Auto => "auto",
            Language.English => "en",
            Language.Chinese => "zh",
            Language.Japanese => "ja",
            Language.Korean => "ko",
            Language.Spanish => "es",
            Language.French => "fr",
            Language.German => "de",
            Language.Italian => "it",
            Language.Portuguese => "pt",
            Language.Russian => "ru",
            Language.Arabic => "ar",
            Language.Hindi => "hi",
            Language.Turkish => "tr",
            Language.Vietnamese => "vi",
            Language.Thai => "th",
            Language.Dutch => "nl",
            Language.Polish => "pl",
            Language.Swedish => "sv",
            Language.Norwegian => "no",
            Language.Danish => "da",
            Language.Finnish => "fi",
            Language.Greek => "el",
            Language.Czech => "cs",
            Language.Hungarian => "hu",
            Language.Romanian => "ro",
            Language.Bulgarian => "bg",
            Language.Croatian => "hr",
            Language.Serbian => "sr",
            Language.Slovak => "sk",
            Language.Slovenian => "sl",
            Language.Lithuanian => "lt",
            Language.Latvian => "lv",
            Language.Estonian => "et",
            Language.Ukrainian => "uk",
            Language.Hebrew => "he",
            Language.Persian => "fa",
            Language.Bengali => "bn",
            Language.Tamil => "ta",
            Language.Telugu => "te",
            Language.Kannada => "kn",
            Language.Malayalam => "ml",
            Language.Marathi => "mr",
            Language.Gujarati => "gu",
            Language.Punjabi => "pa",
            Language.Urdu => "ur",
            Language.Indonesian => "id",
            Language.Malay => "ms",
            Language.Filipino => "tl",
            Language.Swahili => "sw",
            Language.Amharic => "am",
            Language.Burmese => "my",
            Language.Georgian => "ka",
            Language.Khmer => "km",
            Language.Lao => "lo",
            Language.Mongolian => "mn",
            Language.Nepali => "ne",
            Language.Sinhala => "si",
            Language.Albanian => "sq",
            Language.Armenian => "hy",
            Language.Azerbaijani => "az",
            Language.Basque => "eu",
            Language.Belarusian => "be",
            Language.Bosnian => "bs",
            Language.Catalan => "ca",
            Language.Esperanto => "eo",
            Language.Galician => "gl",
            Language.Icelandic => "is",
            Language.Irish => "ga",
            Language.Kazakh => "kk",
            Language.Luxembourgish => "lb",
            Language.Macedonian => "mk",
            Language.Moldovan => "ro",
            Language.Montenegrin => "sr",
            Language.Pashto => "ps",
            Language.Uzbek => "uz",
            _ => "en"
        };
    }

    #endregion

    #region 执行 Whisper

    private async Task<WhisperResult> ExecuteWhisperAsync(string args)
    {
        var result = new WhisperResult();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var totalDuration = TimeSpan.Zero;
        var startTime = DateTime.Now;

        _progress?.Report("正在加载Whisper模型...");

        var processInfo = new ProcessStartInfo
        {
            FileName = _whisperExecutablePath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _progress?.Report(e.Data);
                outputBuilder.AppendLine(e.Data);
                ProcessWhisperOutput(e.Data, ref totalDuration);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _progress?.Report($"[ERROR] {e.Data}");
                errorBuilder.AppendLine(e.Data);
                ProcessWhisperOutput(e.Data, ref totalDuration);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        result.ExitCode = process.ExitCode;
        result.StandardOutput = outputBuilder.ToString();
        result.StandardError = errorBuilder.ToString();
        result.ProcessingTime = DateTime.Now - startTime;

        _progress?.Report($"\nWhisper退出代码: {result.ExitCode}");
        _progress?.Report($"处理时间: {result.ProcessingTime.TotalSeconds:F2}秒");

        if (result.ExitCode != 0)
        {
            result.Success = false;
            result.ErrorMessage = $"Whisper识别失败，退出代码: {result.ExitCode}\n错误信息: {errorBuilder}";
            throw new Exception(result.ErrorMessage);
        }

        result.Success = true;
        return result;
    }

    #endregion

    #region 输出处理

    private void ProcessWhisperOutput(string output, ref TimeSpan totalDuration)
    {
        var durationMatch = Regex.Match(output, @"\((\d+) samples, ([\d.]+) sec\)");
        if (durationMatch.Success && double.TryParse(durationMatch.Groups[2].Value, out var totalSeconds))
        {
            totalDuration = TimeSpan.FromSeconds(totalSeconds);
        }

        var timestampMatch = Regex.Match(output, @"\[(\d{2}):(\d{2}):(\d{2})\.(\d{3}) --> (\d{2}):(\d{2}):(\d{2})\.(\d{3})\]");
        if (timestampMatch.Success && totalDuration.TotalSeconds > 0)
        {
            var endTime = TimeSpan.Parse(timestampMatch.Groups[5].Value + ":" + timestampMatch.Groups[6].Value + ":" + timestampMatch.Groups[7].Value + "." + timestampMatch.Groups[8].Value);
            var percentage = (int)Math.Min((endTime.TotalSeconds / totalDuration.TotalSeconds) * 100, 100);
            _progress?.ReportInt(percentage, $"正在识别... {percentage:F0}%");
        }
    }

    #endregion

    #region 收集生成的文件

    private void CollectGeneratedFiles(WhisperOptions options, WhisperResult result)
    {
        if (string.IsNullOrEmpty(options.OutputFilePath))
        {
            return;
        }

        var outputDir = Path.GetDirectoryName(options.OutputFilePath);
        var outputBase = Path.GetFileNameWithoutExtension(options.OutputFilePath);

        var extensions = new List<string>();

        foreach (var format in options.OutputFormats)
        {
            extensions.Add(GetFileExtension(format));
        }

        foreach (var ext in extensions)
        {
            var filePath = Path.Combine(outputDir ?? "", $"{outputBase}{ext}");
            if (File.Exists(filePath))
            {
                result.GeneratedFiles.Add(filePath);
                _progress?.Report($"[OK] 文件已生成: {filePath}");
            }
        }

        result.OutputPath = options.OutputFilePath;
    }

    private string GetFileExtension(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Text => ".txt",
            OutputFormat.Vtt => ".vtt",
            OutputFormat.Srt => ".srt",
            OutputFormat.Lrc => ".lrc",
            OutputFormat.Words => ".wts",
            OutputFormat.Csv => ".csv",
            OutputFormat.Json => ".json",
            OutputFormat.JsonFull => ".json",
            _ => string.Empty
        };
    }

    #endregion
}
