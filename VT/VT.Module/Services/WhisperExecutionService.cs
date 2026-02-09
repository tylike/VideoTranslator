using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using VideoTranslator.Interfaces;
using VT.Core;
using VT.Module.BusinessObjects;
using VT.Module.BusinessObjects.Whisper;

namespace VT.Module.Services;

public interface IWhisperExecutionService
{
    Task<WhisperResult> ExecuteAsync(WhisperTask task, IProgressService? progress = null);
}

public class WhisperExecutionService : IWhisperExecutionService
{
    private readonly IServiceProvider _serviceProvider;
    private const string DefaultWhisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";

    public WhisperExecutionService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<WhisperResult> ExecuteAsync(WhisperTask task, IProgressService? progress = null)
    {
        progress.Report($"[WhisperExecutionService] 开始执行任务: {task.TaskName}");
        progress.Report(new string('=', 60));

        try
        {
            ValidateTask(task, progress);

            var args = BuildArguments(task);
            progress.Report($"命令参数: {args}");

            task.Status = WhisperTaskStatus.Running;
            task.StartTime = DateTime.Now;
            task.Save();

            var result = await ExecuteWhisperAsync(task, args, progress);

            task.EndTime = DateTime.Now;
            task.ProcessingDuration = (task.EndTime - task.StartTime)?.TotalSeconds;
            task.Status = WhisperTaskStatus.Completed;
            task.IsSuccess = result.Success;
            task.ExitCode = result.ExitCode;
            task.StandardOutput = result.StandardOutput;
            task.StandardError = result.StandardError;
            task.GeneratedFiles = string.Join("\n", result.GeneratedFiles);
            task.Save();

            progress.Report($"[OK] 任务完成，处理时长: {task.ProcessingDuration:F2}秒");

            return result;
        }
        catch (Exception ex)
        {
            task.Status = WhisperTaskStatus.Failed;
            task.EndTime = DateTime.Now;
            task.ProcessingDuration = (task.EndTime - task.StartTime)?.TotalSeconds;
            task.ErrorMessage = ex.Message;
            task.Save();

            progress.Error($"[ERROR] 任务失败: {ex.Message}");
            throw;
        }
    }

    #region 验证

    private void ValidateTask(WhisperTask task, IProgressService progress)
    {
        progress.Report("验证任务参数...");

        if (string.IsNullOrEmpty(task.AudioFilePath))
        {
            throw new ArgumentException("必须指定音频文件路径", nameof(task.AudioFilePath));
        }

        if (!File.Exists(task.AudioFilePath))
        {
            throw new FileNotFoundException($"音频文件不存在: {task.AudioFilePath}");
        }

        if (string.IsNullOrEmpty(task.ModelPath))
        {
            throw new ArgumentException("必须指定模型文件路径", nameof(task.ModelPath));
        }

        if (!File.Exists(task.ModelPath))
        {
            throw new FileNotFoundException($"模型文件不存在: {task.ModelPath}");
        }

        if (string.IsNullOrEmpty(task.OutputFilePath))
        {
            throw new ArgumentException("必须指定输出文件路径", nameof(task.OutputFilePath));
        }

        var outputDir = Path.GetDirectoryName(task.OutputFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        progress.Report("[OK] 参数验证通过");
    }

    #endregion

    #region 参数构建

    private string BuildArguments(WhisperTask task)
    {
        var args = new StringBuilder();

        args.Append($"-m \"{task.ModelPath}\"");
        args.Append($" -f \"{task.AudioFilePath}\"");

        if (!string.IsNullOrEmpty(task.OutputFilePath))
        {
            var outputDir = Path.GetDirectoryName(task.OutputFilePath);
            var outputBase = Path.GetFileNameWithoutExtension(task.OutputFilePath);
            args.Append($" -of \"{Path.Combine(outputDir ?? "", outputBase)}\"");
        }

        args.Append($" {GetFormatArgument(task.OutputFormat)}");

        if (task.Language != Language.Auto)
        {
            args.Append($" -l {GetLanguageCode(task.Language)}");
        }

        if (task.DetectLanguage)
        {
            args.Append(" -dl");
        }

        if (!string.IsNullOrEmpty(task.Prompt))
        {
            args.Append($" --prompt \"{task.Prompt}\"");
        }

        if (task.MaxLength.HasValue)
        {
            args.Append($" -ml {task.MaxLength.Value}");
        }

        if (task.MaxContext.HasValue)
        {
            args.Append($" -mc {task.MaxContext.Value}");
        }

        if (task.SplitOnWord)
        {
            args.Append(" -sow");
        }

        if (task.BestOf.HasValue)
        {
            args.Append($" -bo {task.BestOf.Value}");
        }

        if (task.BeamSize.HasValue)
        {
            args.Append($" -bs {task.BeamSize.Value}");
        }

        if (task.Temperature.HasValue)
        {
            args.Append($" -tp {task.Temperature.Value:F2}");
        }

        if (task.Translate)
        {
            args.Append(" -tr");
        }

        if (task.Diarize)
        {
            args.Append(" -di");
        }

        if (task.TinyDiarize)
        {
            args.Append(" -tdrz");
        }

        if (task.NoGpu)
        {
            args.Append(" -ng");
        }

        if (task.EnableVad)
        {
            args.Append(" --vad");
        }

        if (task.VadThreshold.HasValue)
        {
            args.Append($" -vt {task.VadThreshold.Value:F2}");
        }

        if (task.VadMinSpeechDurationMs.HasValue)
        {
            args.Append($" -vspd {task.VadMinSpeechDurationMs.Value}");
        }

        if (task.VadMinSilenceDurationMs.HasValue)
        {
            args.Append($" -vsd {task.VadMinSilenceDurationMs.Value}");
        }

        if (task.PrintProgress)
        {
            args.Append(" -pp");
        }

        return args.ToString();
    }

    private string GetFormatArgument(WhisperOutputFormat format)
    {
        return format switch
        {
            WhisperOutputFormat.Text => "-otxt",
            WhisperOutputFormat.Vtt => "-ovtt",
            WhisperOutputFormat.Srt => "-osrt",
            WhisperOutputFormat.Lrc => "-olrc",
            WhisperOutputFormat.Words => "-owts",
            WhisperOutputFormat.Csv => "-ocsv",
            WhisperOutputFormat.Json => "-oj",
            WhisperOutputFormat.JsonFull => "-ojf",
            _ => "-ojf"
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

    private async Task<WhisperResult> ExecuteWhisperAsync(WhisperTask task, string args, IProgressService progress)
    {
        var result = new WhisperResult(task.Session)
        {
            ResultName = $"{task.TaskName}_结果_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var totalDuration = TimeSpan.Zero;
        var startTime = DateTime.Now;

        progress.Report("正在加载Whisper模型...");

        var processInfo = new ProcessStartInfo
        {
            FileName = DefaultWhisperPath,
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
                progress.Report(e.Data);
                outputBuilder.AppendLine(e.Data);
                ProcessWhisperOutput(e.Data, ref totalDuration, task, progress);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                progress.Report($"[ERROR] {e.Data}");
                errorBuilder.AppendLine(e.Data);
                ProcessWhisperOutput(e.Data, ref totalDuration, task, progress);
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

        progress.Report($"\nWhisper退出代码: {result.ExitCode}");
        progress.Report($"处理时间: {result.ProcessingTime.TotalSeconds:F2}秒");

        if (result.ExitCode != 0)
        {
            result.Success = false;
            result.ErrorMessage = $"Whisper识别失败，退出代码: {result.ExitCode}\n错误信息: {errorBuilder}";
            throw new Exception(result.ErrorMessage);
        }

        result.Success = true;
        CollectGeneratedFiles(task, result, progress);

        return result;
    }

    #endregion

    #region 输出处理

    private void ProcessWhisperOutput(string output, ref TimeSpan totalDuration, WhisperTask task, IProgressService progress)
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
            var percentage = Math.Min((endTime.TotalSeconds / totalDuration.TotalSeconds) * 100, 100);
            task.ProgressPercentage = (int)percentage;
            progress.ReportProgress(percentage, $"正在识别... {percentage:F0}%");
        }
    }

    #endregion

    #region 收集生成的文件

    private void CollectGeneratedFiles(WhisperTask task, WhisperResult result, IProgressService progress)
    {
        if (string.IsNullOrEmpty(task.OutputFilePath))
        {
            return;
        }

        var outputDir = Path.GetDirectoryName(task.OutputFilePath);
        var outputBase = Path.GetFileNameWithoutExtension(task.OutputFilePath);

        var extensions = new List<string> { ".json", ".srt", ".vtt", ".txt", ".lrc", ".csv", ".wts" };
        var generatedFiles = new List<string>();

        foreach (var ext in extensions)
        {
            var filePath = Path.Combine(outputDir ?? "", $"{outputBase}{ext}");
            if (File.Exists(filePath))
            {
                generatedFiles.Add(filePath);
                progress.Report($"[OK] 文件已生成: {filePath}");

                if (ext == ".json")
                {
                    result.JsonFilePath = filePath;
                    result.JsonContent = File.ReadAllText(filePath);
                }
                else if (ext == ".srt")
                {
                    result.SrtFilePath = filePath;
                    result.SrtContent = File.ReadAllText(filePath);
                }
                else if (ext == ".vtt")
                {
                    result.VttFilePath = filePath;
                    result.VttContent = File.ReadAllText(filePath);
                }
                else if (ext == ".txt")
                {
                    result.TxtFilePath = filePath;
                    result.TxtContent = File.ReadAllText(filePath);
                }
            }
        }

        result.GeneratedFiles = string.Join("\n", generatedFiles);
        result.OutputPath = task.OutputFilePath;
    }

    #endregion

    #region 静默进度服务

    private class SilentProgressService : IProgressService
    {
        //public object Application { get; set; }
        public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true) { }
        public void ShowProgress(bool marquee = false) { }
        public void HideProgress() { }
        public void ResetProgress() { }
        public void Report() { }
        public void Report(string message, MessageType messageType = MessageType.Info) { }
        public void Title(string message) { }
        public void Success(string message) { }
        public void Error(string message) { }
        public void Warning(string message) { }

        void IProgressService.ReportProgress(double value)
        {
        }

        void IProgressService.SetProgressMaxValue(double value)
        {
        }
    }

    #endregion
}
