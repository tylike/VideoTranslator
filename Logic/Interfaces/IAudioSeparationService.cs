using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using VideoTranslator.Config;
using VideoTranslator.Models;
using VideoTranslator.Services;

namespace VideoTranslator.Interfaces;

public interface IAudioSeparationService
{
    Task<SeparationResult> SeparateVocalAndBackgroundAsync(string inputAudioPath, string outputDirectory );
    Task<SeparationResult> SeparateVocalAndBackgroundByProjectIdAsync(string inputAudioPath, string projectId);
    bool IsSeparatedAudioAvailable(string projectId);
    SeparationResult? GetSeparatedAudioPaths(string projectId);
}

public class AudioSeparationService :ServiceBase, IAudioSeparationService
{
    private readonly string _pythonPath;
    private readonly string _separationScriptPath;
    private readonly string _demucsOutputBasePath;
    private readonly string _spleeterExePath;
    private readonly string _spleeterModelPath;
    private static readonly Dictionary<string, SeparationResult> _separationCache = new();
    private readonly ILogger<AudioSeparationService> _logger;

    public AudioSeparationService(ILogger<AudioSeparationService> logger, IProgressService? progress = null) : base()
    {
        _pythonPath = Settings.PythonPath;
        _separationScriptPath = Settings.AudioSeparationScriptPath;
        _demucsOutputBasePath = Settings.DemucsOutputBasePath;
        _spleeterExePath = Settings.SpleeterExePath;
        _spleeterModelPath = Settings.SpleeterModelPath;
        _logger = logger;
    }

    public async Task<SeparationResult> SeparateVocalAndBackgroundAsync(string inputAudioPath, string outputDirectory)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SeparationResult
        {
            InputAudioPath = inputAudioPath,
            OutputDirectory = outputDirectory,
            Success = false
        };

        try
        {
            progress?.Report("正在准备音频分离...");

            if (!File.Exists(inputAudioPath))
            {
                result.ErrorMessage = $"输入音频文件不存在: {inputAudioPath}";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            var outputDir = Path.Combine(outputDirectory, "spleeter_output");
            Directory.CreateDirectory(outputDir);

            progress?.Report("正在调用 Spleeter 模型进行音频分离...");

            var (success, output) = await ExecuteSpleeterAsync(inputAudioPath, outputDir);

            _logger.LogInformation($"Spleeter 输出:\n{output}");

            if (!success)
            {
                result.ErrorMessage = $"音频分离失败: {output}";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            var vocalPath = Path.Combine(outputDir, "vocals.wav");
            var backgroundPath = Path.Combine(outputDir, "accompaniment.wav");

            if (!File.Exists(vocalPath))
            {
                result.ErrorMessage = $"生成的人声音频文件不存在: {vocalPath}";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            if (!File.Exists(backgroundPath))
            {
                result.ErrorMessage = $"生成的背景音频文件不存在: {backgroundPath}";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            result.VocalAudioPath = vocalPath;
            result.BackgroundAudioPath = backgroundPath;
            result.Success = true;

            progress?.Report("音频分离完成！");

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation($"音频分离成功，处理时间: {result.ProcessingTime.TotalSeconds:F2}秒");
            _logger.LogInformation($"  人声音频: {vocalPath}");
            _logger.LogInformation($"  背景音频: {backgroundPath}");

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"音频分离过程中发生异常: {ex.Message}";
            _logger.LogError(ex, result.ErrorMessage);
            return result;
        }
    }

    public async Task<SeparationResult> SeparateVocalAndBackgroundByProjectIdAsync(string inputAudioPath, string projectId)
    {
        if (_separationCache.TryGetValue(projectId, out var cachedResult))
        {
            if (File.Exists(cachedResult.VocalAudioPath) && File.Exists(cachedResult.BackgroundAudioPath))
            {
                _logger.LogInformation($"使用缓存的分离结果 (ProjectId: {projectId})");
                return cachedResult;
            }
        }

        var outputDirectory = Path.Combine(_demucsOutputBasePath, projectId, "audio_test");
        var result = await SeparateVocalAndBackgroundAsync(inputAudioPath, outputDirectory);

        if (result.Success)
        {
            _separationCache[projectId] = result;
        }

        return result;
    }

    public bool IsSeparatedAudioAvailable(string projectId)
    {
        if (_separationCache.TryGetValue(projectId, out var result))
        {
            return result.Success && File.Exists(result.VocalAudioPath) && File.Exists(result.BackgroundAudioPath);
        }

        var outputDir = Path.Combine(_demucsOutputBasePath, projectId, "audio_test", "spleeter_output");
        var vocalPath = Path.Combine(outputDir, "vocals.wav");
        var backgroundPath = Path.Combine(outputDir, "accompaniment.wav");

        return File.Exists(vocalPath) && File.Exists(backgroundPath);
    }

    public SeparationResult? GetSeparatedAudioPaths(string projectId)
    {
        if (_separationCache.TryGetValue(projectId, out var result))
        {
            if (result.Success && File.Exists(result.VocalAudioPath) && File.Exists(result.BackgroundAudioPath))
            {
                return result;
            }
        }

        var outputDir = Path.Combine(_demucsOutputBasePath, projectId, "audio_test", "spleeter_output");
        var vocalPath = Path.Combine(outputDir, "vocals.wav");
        var backgroundPath = Path.Combine(outputDir, "accompaniment.wav");

        if (File.Exists(vocalPath) && File.Exists(backgroundPath))
        {
            return new SeparationResult
            {
                VocalAudioPath = vocalPath,
                BackgroundAudioPath = backgroundPath,
                Success = true
            };
        }

        return null;
    }

    private async Task<(bool Success, string Output)> ExecuteSpleeterAsync(string inputAudioPath, string outputDirectory)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _spleeterExePath,
            Arguments = $"-m {_spleeterModelPath} -o \"{outputDirectory}\\$(TrackName).wav\" --verbose --overwrite \"{inputAudioPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(_spleeterExePath) ?? Directory.GetCurrentDirectory(),
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process();
        process.StartInfo = processInfo;
        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                outputBuilder.AppendLine(args.Data);

                var percentageMatch = System.Text.RegularExpressions.Regex.Match(args.Data, @"\[\s*(\d+\.?\d*)%\]");
                if (percentageMatch.Success && double.TryParse(percentageMatch.Groups[1].Value, out var percentage))
                {
                    progress?.ReportProgress(percentage);
                }
                else
                {
                    progress?.Report(args.Data);
                }

                _logger.LogInformation(args.Data);
            }
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                errorBuilder.AppendLine(args.Data);
                _logger.LogError(args.Data);
            }
        };
        progress?.Report("正在分离人声和伴奏...");
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var allOutput = outputBuilder.ToString() + errorBuilder.ToString();
        var success = process.ExitCode == 0;

        return (success, allOutput);
    }

}
public class SeparationResult
{
    public string InputAudioPath { get; set; } = string.Empty;
    public string VocalAudioPath { get; set; } = string.Empty;
    public string BackgroundAudioPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
