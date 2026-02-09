using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VadTimeProcessor.Models;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VideoTranslator.SRT.Core.Models;

namespace VideoTranslator.Interfaces;
public interface ISpeechRecognitionService
{
    Task<string> RecognizeAudioAsync(string audioPath, string outputPath, string language = "auto");
}



public class WhisperRecognitionServiceSimple :ServiceBase, ISpeechRecognitionService
{
    private readonly string _whisperPath;
    private readonly string _modelPath;

    public WhisperRecognitionServiceSimple():base()
    {
        _whisperPath = ResolveWhisperPath(Settings.WhisperPath);
        _modelPath = ResolveModelPath(Settings.WhisperModelPath);
    }

    private string ResolveWhisperPath(string configuredPath)
    {
        progress?.Report($"[SpeechRecognitionService] ResolveWhisperPath 输入: {configuredPath}");

        if (string.IsNullOrEmpty(configuredPath))
        {
            progress?.Report("[SpeechRecognitionService] 路径为空，使用默认: whisper-cli.exe");
            return "whisper-cli.exe";
        }

        if (Path.IsPathRooted(configuredPath))
        {
            progress?.Report($"[SpeechRecognitionService] 路径是绝对路径: {configuredPath}");
            return configuredPath;
        }

        var basePath = GetBaseDirectory();
        progress?.Report($"[SpeechRecognitionService] 基础目录: {basePath}");

        var fullPath = Path.GetFullPath(Path.Combine(basePath, configuredPath));
        progress?.Report($"[SpeechRecognitionService] 完整路径: {fullPath}");

        if (File.Exists(fullPath))
        {
            progress?.Report($"[SpeechRecognitionService] 文件存在: {fullPath}");
            return fullPath;
        }

        progress?.Report($"[SpeechRecognitionService] 文件不存在，使用原始路径: {configuredPath}");
        return configuredPath;
    }

    private string ResolveModelPath(string configuredPath)
    {
        progress?.Report($"[SpeechRecognitionService] ResolveModelPath 输入: {configuredPath}");

        if (string.IsNullOrEmpty(configuredPath))
        {
            progress?.Report("[SpeechRecognitionService] 路径为空，使用默认: .");
            return ".";
        }

        if (Path.IsPathRooted(configuredPath))
        {
            progress?.Report($"[SpeechRecognitionService] 路径是绝对路径: {configuredPath}");
            return configuredPath;
        }

        var basePath = GetBaseDirectory();
        progress?.Report($"[SpeechRecognitionService] 基础目录: {basePath}");

        var fullPath = Path.GetFullPath(Path.Combine(basePath, configuredPath));
        progress?.Report($"[SpeechRecognitionService] 完整路径: {fullPath}");

        if (Directory.Exists(fullPath))
        {
            progress?.Report($"[SpeechRecognitionService] 目录存在: {fullPath}");
            return fullPath;
        }

        progress?.Report($"[SpeechRecognitionService] 目录不存在，使用原始路径: {configuredPath}");
        return configuredPath;
    }

    private string GetBaseDirectory()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        progress?.Report($"[SpeechRecognitionService] 程序位置: {assemblyLocation}");

        if (string.IsNullOrEmpty(assemblyLocation))
        {
            var currentDir = Directory.GetCurrentDirectory();
            progress?.Report($"[SpeechRecognitionService] 程序位置为空，使用当前目录: {currentDir}");
            return currentDir;
        }

        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        progress?.Report($"[SpeechRecognitionService] 程序目录: {assemblyDirectory}");

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            var currentDir = Directory.GetCurrentDirectory();
            progress?.Report($"[SpeechRecognitionService] 程序目录为空，使用当前目录: {currentDir}");
            return currentDir;
        }

        return assemblyDirectory;
    }

    public async Task<string> RecognizeAudioAsync(string audioPath, string outputPath, string language = "auto")
    {
        progress?.Report($"[SpeechRecognitionService] 开始识别任务");
        progress?.Report(new string('=', 60));
        progress?.Report($"音频文件: {audioPath}");
        progress?.Report($"输出文件: {outputPath}");
        progress?.Report($"语言: {language}");

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var modelFile = FindWhisperModel(_modelPath);
        if (string.IsNullOrEmpty(modelFile))
        {
            throw new FileNotFoundException($"未找到Whisper模型文件，请确认模型文件路径: {_modelPath}");
        }

        progress?.Report($"使用模型: {modelFile}");

        var outputDir = Path.GetDirectoryName(outputPath);
        var outputBase = Path.GetFileNameWithoutExtension(outputPath);
        var jsonPath = Path.Combine(outputDir, outputBase + ".json");
        
        var audioDir = Path.GetDirectoryName(audioPath);
        var srtOutputPath = Path.Combine(audioDir, "whisper_source.srt");

        var args = $"-m \"{modelFile}\" -ojf -osrt -l {language} -of \"{Path.Combine(outputDir, outputBase)}\" -f \"{audioPath}\" -ml 50 -mc 0";

        progress?.Report($"\n开始语音识别...");
        progress?.Report($"命令: {_whisperPath} {args}");

        progress?.Report("正在加载Whisper模型...");

        var processInfo = new ProcessStartInfo
        {
            FileName = _whisperPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        var totalDuration = TimeSpan.Zero;

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                progress?.Report(e.Data);
                outputBuilder.AppendLine(e.Data);
                ProcessWhisperOutput(e.Data, ref totalDuration);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                progress?.Report($"[ERROR] {e.Data}");
                errorBuilder.AppendLine(e.Data);
                ProcessWhisperOutput(e.Data, ref totalDuration);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        var exitCode = process.ExitCode;
        progress?.Report($"\nWhisper退出代码: {exitCode}");

        if (exitCode != 0)
        {
            throw new Exception($"Whisper识别失败，退出代码: {exitCode}\n错误信息: {errorBuilder}");
        }

        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Whisper未生成JSON文件: {jsonPath}");
        }

        var whisperSrtPath = Path.Combine(outputDir, outputBase + ".srt");
        if (File.Exists(whisperSrtPath))
        {
            if (File.Exists(srtOutputPath))
            {
                File.Delete(srtOutputPath);
            }
            File.Move(whisperSrtPath, srtOutputPath);
            progress?.Report($"\n[OK] Whisper源SRT已生成: {srtOutputPath}");
        }

        progress?.Report($"\n[OK] JSON已生成: {jsonPath}");
        progress?.Report("正在生成TTS优化的SRT字幕...");

        var srtContent = GenerateTtsOptimizedSrt(jsonPath);
        await File.WriteAllTextAsync(outputPath, srtContent);

        progress?.Report($"\n[OK] SRT字幕已生成: {outputPath}");
        progress?.Report(new string('=', 60));

        return outputPath;
    }

    private string FindWhisperModel(string modelDir)
    {
        var modelFiles = Directory.GetFiles(modelDir, "*.pt")
            .Concat(Directory.GetFiles(modelDir, "*.bin"))
            .Concat(Directory.GetFiles(modelDir, "*.gguf"))
            .ToList();

        var preferredModels = new[] { "large-v3", "large-v2", "large", "medium", "small", "base" };

        foreach (var model in preferredModels)
        {
            var modelFile = modelFiles.FirstOrDefault(f =>
                f.Contains(model, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(modelFile))
            {
                return modelFile;
            }
        }

        return modelFiles.FirstOrDefault() ?? string.Empty;
    }

    #region Whisper Output Processing

    private void ProcessWhisperOutput(string output, ref TimeSpan totalDuration)
    {
        var durationMatch = System.Text.RegularExpressions.Regex.Match(output, @"\((\d+) samples, ([\d.]+) sec\)");
        if (durationMatch.Success && double.TryParse(durationMatch.Groups[2].Value, out var totalSeconds))
        {
            totalDuration = TimeSpan.FromSeconds(totalSeconds);
        }

        var timestampMatch = System.Text.RegularExpressions.Regex.Match(output, @"\[(\d{2}):(\d{2}):(\d{2})\.(\d{3}) --> (\d{2}):(\d{2}):(\d{2})\.(\d{3})\]");
        if (timestampMatch.Success && totalDuration.TotalSeconds > 0)
        {
            var endTime = TimeSpan.Parse(timestampMatch.Groups[5].Value + ":" + timestampMatch.Groups[6].Value + ":" + timestampMatch.Groups[7].Value + "." + timestampMatch.Groups[8].Value);
            var percentage = Math.Min((endTime.TotalSeconds / totalDuration.TotalSeconds) * 100, 100);
            progress?.ReportProgress(percentage, $"正在识别源字幕... {percentage:F0}%");
        }
    }

    #endregion

    #region SRT Generation

    private string GenerateTtsOptimizedSrt(string jsonPath)
    {
        try
        {
            progress?.Report("解析Whisper JSON数据...");
            var parser = new VideoTranslator.SRT.Services.WhisperJsonParser();
            var whisperData = parser.Parse(jsonPath);

            progress?.Report("生成TTS优化的SRT字幕...");
            var config = new VideoTranslator.SRT.Models.TtsOptimizationConfig();
            var generator = new VideoTranslator.SRT.Services.SrtGenerator(config);
            var srtContent = generator.GenerateSrtForTts(whisperData);

            progress?.Report("SRT字幕生成完成");
            return srtContent;
        }
        catch (Exception ex)
        {
            progress?.Error($"[ERROR] SRT生成失败: {ex.Message}");
            throw;
        }
    }

    #endregion
}
