using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VideoTranslator.Services;

public class VideoVadWorkflowService : ServiceBase
{
    private readonly WhisperRecognitionService _whisperRecognitionService;

    public VideoVadWorkflowService(       
        WhisperRecognitionService whisperRecognitionService
        ) : base()
    {        
        _whisperRecognitionService = whisperRecognitionService;
    }

    #region 获取音频时长

    public async Task<decimal> GetAudioDurationAsync(string mediaPath)
    {
        progress?.Report("[VideoVadWorkflowService] 开始获取音频时长");
        progress?.Report($"  媒体文件: {mediaPath}");

        if (!File.Exists(mediaPath))
        {
            throw new FileNotFoundException($"媒体文件不存在: {mediaPath}");
        }

        var arguments = $"-i \"{mediaPath}\" -f null -";

        try
        {
            var output = await Ffmpeg.ExecuteCommandAsync(arguments);

            var durationMatch = System.Text.RegularExpressions.Regex.Match(output, @"Duration:\s+(\d+):(\d+):([\d.]+)");
            if (durationMatch.Success)
            {
                var hours = int.Parse(durationMatch.Groups[1].Value);
                var minutes = int.Parse(durationMatch.Groups[2].Value);
                var seconds = decimal.Parse(durationMatch.Groups[3].Value);
                var totalSeconds = hours * 3600 + minutes * 60 + seconds;

                progress?.Report($"[VideoVadWorkflowService] 音频时长: {totalSeconds:F2}秒");
                return totalSeconds;
            }

            throw new InvalidOperationException("无法从FFmpeg输出中解析音频时长");
        }
        catch (Exception ex)
        {
            progress?.Error($"[VideoVadWorkflowService] 获取音频时长失败: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region 根据指定分割点分割视频

    public List<string> SplitVideoByPoints(
        string videoPath,
        List<decimal> splitPoints,
        string outputFolder,
        string outputPrefix = "segment")
    {
        progress?.Report("[VideoVadWorkflowService] 开始根据指定分割点分割视频");
        progress?.Report($"  视频文件: {videoPath}");
        progress?.Report($"  分割点数量: {splitPoints.Count}");
        progress?.Report($"  输出文件夹: {outputFolder}");

        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException($"视频文件不存在: {videoPath}");
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            progress?.Report($"[VideoVadWorkflowService] 创建输出文件夹: {outputFolder}");
        }

        var outputFiles = new List<string>();
        var ffmpegPath = Settings.FfmpegPath;
        var sortedSplitPoints = splitPoints.OrderBy(p => p).ToList();
        var startTime = 0m;

        for (int i = 0; i < sortedSplitPoints.Count; i++)
        {
            var endTime = sortedSplitPoints[i];
            var outputFile = Path.Combine(outputFolder, $"{i + 1}.mp4");
            var duration = endTime - startTime;

            if (duration <= 0)
            {
                progress?.Report($"[VideoVadWorkflowService] 片段 {i + 1} 时长无效，跳过: {duration:F2}秒");
                continue;
            }

            var arguments = $"-ss {startTime} " +
                           $"-i \"{videoPath}\" " +
                           $"-t {duration} " +
                           $"-c:v libx264 " +
                           $"-c:a aac " +
                           $"-y \"{outputFile}\"";

            progress?.Report($"[VideoVadWorkflowService] 分割片段 {i + 1}: {startTime:F2}s - {endTime:F2}s (时长: {duration:F2}s)");

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var stdError = process.StandardError.ReadToEnd();
            var stdOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0 && File.Exists(outputFile))
            {
                outputFiles.Add(outputFile);
                progress?.Report($"[VideoVadWorkflowService] 片段 {i + 1} 分割成功: {outputFile}");
            }
            else
            {
                progress?.Error($"[VideoVadWorkflowService] 片段 {i + 1} 分割失败，退出代码: {process.ExitCode}");
                if (!string.IsNullOrEmpty(stdError))
                {
                    progress?.Error($"[VideoVadWorkflowService] FFmpeg错误: {stdError}");
                }
            }

            startTime = endTime;
        }

        var lastOutputFile = Path.Combine(outputFolder, $"{sortedSplitPoints.Count + 1}.mp4");
        var lastArguments = $"-ss {startTime} " +
                           $"-i \"{videoPath}\" " +
                           $"-c:v libx264 " +
                           $"-c:a aac " +
                           $"-y \"{lastOutputFile}\"";

        progress?.Report($"[VideoVadWorkflowService] 分割最后一个片段: {startTime:F2}s - 结束");

        var lastStartInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = lastArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var lastProcess = new Process { StartInfo = lastStartInfo };
        lastProcess.Start();
        var lastStdError = lastProcess.StandardError.ReadToEnd();
        var lastStdOutput = lastProcess.StandardOutput.ReadToEnd();
        lastProcess.WaitForExit();

        if (lastProcess.ExitCode == 0 && File.Exists(lastOutputFile))
        {
            outputFiles.Add(lastOutputFile);
            progress?.Report($"[VideoVadWorkflowService] 最后一个片段分割成功: {lastOutputFile}");
        }
        else
        {
            progress?.Report($"[VideoVadWorkflowService] 最后一个片段分割失败，退出代码: {lastProcess.ExitCode}");
            if (!string.IsNullOrEmpty(lastStdError))
            {
                progress?.Report($"[VideoVadWorkflowService] FFmpeg错误: {lastStdError}");
            }
        }

        progress?.Report($"[VideoVadWorkflowService] 视频分割完成，共生成 {outputFiles.Count} 个片段");
        return outputFiles;
    }

    #endregion

    #region 检查视频是否包含音频流

    private async Task<bool> HasAudioStreamAsync(string mediaPath)
    {
        progress?.Report("[VideoVadWorkflowService] 检查媒体文件是否包含音频流");
        progress?.Report($"  媒体文件: {mediaPath}");

        if (!File.Exists(mediaPath))
        {
            throw new FileNotFoundException($"媒体文件不存在: {mediaPath}");
        }

        var extension = Path.GetExtension(mediaPath).ToLower();
        
        var audioExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac", ".flac", ".ogg", ".wma", ".opus" };
        
        if (audioExtensions.Contains(extension))
        {
            progress?.Report($"[VideoVadWorkflowService] 检测到纯音频文件，直接返回true");
            return true;
        }

        var arguments = $"-i \"{mediaPath}\" -f null -";

        try
        {
            var output = await Ffmpeg.ExecuteCommandAsync(arguments);

            var audioStreamMatch = System.Text.RegularExpressions.Regex.Match(output, @"Stream #\d+:\d+\[.*?\]\(.*?\): Audio:");
            var hasAudio = audioStreamMatch.Success;

            progress?.Report($"[VideoVadWorkflowService] 音频流检查结果: {(hasAudio ? "包含音频流" : "不包含音频流")}");

            return hasAudio;
        }
        catch (Exception ex)
        {
            progress?.Error($"[VideoVadWorkflowService] 检查音频流失败: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region 从视频提取音频

    public async Task<string> ExtractAudioFromVideoAsync(
        string videoPath,
        string? outputWavPath = null,
        int sampleRate = 16000,
        int channels = 1)
    {
        progress?.Report("[VideoVadWorkflowService] 开始从视频提取音频");
        progress?.Report($"  视频文件: {videoPath}");

        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException($"视频文件不存在: {videoPath}");
        }

        if (string.IsNullOrEmpty(outputWavPath))
        {
            var directory = Path.GetDirectoryName(videoPath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoPath);
            outputWavPath = Path.Combine(directory ?? "", $"{fileNameWithoutExtension}_extracted.wav");
        }

        if (File.Exists(outputWavPath))
        {
            progress?.Report($"[VideoVadWorkflowService] WAV文件已存在，跳过提取: {outputWavPath}");
            return outputWavPath;
        }

        var hasAudio = await HasAudioStreamAsync(videoPath);
        if (!hasAudio)
        {
            throw new InvalidOperationException($"视频文件不包含音频流，无法提取音频: {videoPath}");
        }

        var arguments = $"-i \"{videoPath}\" " +
                       $"-vn " +
                       $"-acodec pcm_s16le " +
                       $"-ar {sampleRate} " +
                       $"-ac {channels} " +
                       $"-y \"{outputWavPath}\"";

        progress?.Report($"[VideoVadWorkflowService] FFmpeg参数: {arguments}");

        try
        {
            await Ffmpeg.ExecuteCommandAsync(arguments);
            progress?.Report($"[VideoVadWorkflowService] 音频提取完成: {outputWavPath}");

            if (!File.Exists(outputWavPath))
            {
                throw new FileNotFoundException($"音频提取失败，输出文件不存在: {outputWavPath}");
            }

            return outputWavPath;
        }
        catch (Exception ex)
        {
            progress?.Error($"[VideoVadWorkflowService] 音频提取失败: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region 音频格式转换

    private async Task<string> ConvertAudioToWavAsync(string audioPath)
    {
        progress?.Report("[VideoVadWorkflowService] 开始转换音频文件");
        progress?.Report($"  输入文件: {audioPath}");

        var extension = Path.GetExtension(audioPath).ToLower();
        if (extension == ".wav")
        {
            progress?.Report("[VideoVadWorkflowService] 检测到WAV文件，检查格式...");
            var audioInfo = await CheckWavFormatAsync(audioPath);
            if (audioInfo.SampleRate == 16000 && audioInfo.Channels == 1)
            {
                progress?.Report("[VideoVadWorkflowService] WAV格式已符合要求，无需转换");
                return audioPath;
            }
            progress?.Report($"[VideoVadWorkflowService] WAV格式不符合要求: 采样率={audioInfo.SampleRate}Hz, 声道={audioInfo.Channels}");
        }

        var tempDir = Path.GetTempPath();
        var tempFileName = $"vad_temp_{Guid.NewGuid():N}.wav";
        var outputPath = Path.Combine(tempDir, tempFileName);

        progress?.Report($"[VideoVadWorkflowService] 输出文件: {outputPath}");

        var arguments = $"-i \"{audioPath}\" " +
                       $"-vn " +
                       $"-ar 16000 " +
                       $"-ac 1 " +
                       $"-ab 32k " +
                       $"-af volume=1.75 " +
                       $"-f wav " +
                       $"-y \"{outputPath}\"";

        progress?.Report($"[VideoVadWorkflowService] FFmpeg参数: {arguments}");

        try
        {
            await Ffmpeg.ExecuteCommandAsync(arguments);
            progress?.Report("[VideoVadWorkflowService] 音频转换成功");
            return outputPath;
        }
        catch (Exception ex)
        {
            progress?.Error($"[VideoVadWorkflowService] 音频转换失败: {ex.Message}");
            throw;
        }
    }

    private async Task<(int SampleRate, int Channels)> CheckWavFormatAsync(string wavPath)
    {
        var arguments = $"-i \"{wavPath}\" -f null -";
        try
        {
            var output = await Ffmpeg.ExecuteCommandAsync(arguments);

            int sampleRate = 0;
            int channels = 0;

            var sampleRateMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s*Hz");
            if (sampleRateMatch.Success)
            {
                int.TryParse(sampleRateMatch.Groups[1].Value, out sampleRate);
            }

            var channelsMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s*channel");
            if (channelsMatch.Success)
            {
                int.TryParse(channelsMatch.Groups[1].Value, out channels);
            }

            return (sampleRate, channels);
        }
        catch
        {
            return (0, 0);
        }
    }

    #endregion

    #region 执行VAD检测

    public async Task<VadDetectionResult> ExecuteVadDetectionAsync(
        string audioPath,
        decimal threshold = 0.5m,
        int minSpeechDurationMs = 250,
        int minSilenceDurationMs = 100)
    {
        progress?.Report("[VideoVadWorkflowService] 开始执行VAD检测");
        progress?.Report($"  音频文件: {audioPath}");
        progress?.Report($"  VAD阈值: {threshold}");
        progress?.Report($"  最小语音时长: {minSpeechDurationMs}ms");
        progress?.Report($"  最小静音时长: {minSilenceDurationMs}ms");

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        var result = await _whisperRecognitionService.DetectVadSegmentsAsync(
            audioPath,
            threshold,
            minSpeechDurationMs,
            minSilenceDurationMs);

        progress?.Report($"[VideoVadWorkflowService] VAD检测完成");
        progress?.Report($"  语音片段: {result.SpeechSegmentCount} 个");
        progress?.Report($"  静音片段: {result.SilenceSegmentCount} 个");
        progress?.Report($"  总语音时长: {result.TotalSpeechDuration:F2}秒");
        progress?.Report($"  总静音时长: {result.TotalSilenceDuration:F2}秒");

        return result;
    }

    #endregion

    #region 根据VAD结果分割视频

    public List<string> SplitVideoByVadSegments(
        string videoPath,
        VadDetectionResult vadResult,
        string outputFolder,
        string outputPrefix = "segment")
    {
        progress?.Report("[VideoVadWorkflowService] 开始根据VAD结果分割视频");
        progress?.Report($"  视频文件: {videoPath}");
        progress?.Report($"  输出文件夹: {outputFolder}");

        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException($"视频文件不存在: {videoPath}");
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            progress?.Report($"[VideoVadWorkflowService] 创建输出文件夹: {outputFolder}");
        }

        var outputFiles = new List<string>();
        var ffmpegPath = Settings.FfmpegPath;

        foreach (var segment in vadResult.Segments.Where(s => s.IsSpeech))
        {
            var outputFile = Path.Combine(outputFolder, $"{outputPrefix}_{segment.Index:D3}.mp4");
            var duration = segment.End - segment.Start;

            var arguments = $"-ss {segment.Start} " +
                           $"-i \"{videoPath}\" " +
                           $"-t {duration} " +
                           $"-c:v libx264 " +
                           $"-c:a aac " +
                           $"-y \"{outputFile}\"";

            progress?.Report($"[VideoVadWorkflowService] 分割片段 {segment.Index}: {segment.Start:F2}s - {segment.End:F2}s");

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0 && File.Exists(outputFile))
            {
                outputFiles.Add(outputFile);
                progress?.Report($"[VideoVadWorkflowService] 片段 {segment.Index} 分割成功: {outputFile}");
            }
            else
            {
                progress?.Error($"[VideoVadWorkflowService] 片段 {segment.Index} 分割失败，退出代码: {process.ExitCode}");
            }
        }

        progress?.Report($"[VideoVadWorkflowService] 视频分割完成，共生成 {outputFiles.Count} 个片段");
        return outputFiles;
    }

    #endregion

    #region 保存VAD信息

    public void SaveVadInfo(
        string outputPath,
        VadDetectionResult vadResult,
        List<string>? splitFiles = null)
    {
        progress?.Report("[VideoVadWorkflowService] 开始保存VAD信息");
        progress?.Report($"  输出路径: {outputPath}");

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var vadInfo = new VadInfo
        {
            AudioPath = vadResult.AudioPath,
            AudioDuration = vadResult.AudioDuration,
            SpeechSegmentCount = vadResult.SpeechSegmentCount,
            SilenceSegmentCount = vadResult.SilenceSegmentCount,
            TotalSpeechDuration = vadResult.TotalSpeechDuration,
            TotalSilenceDuration = vadResult.TotalSilenceDuration,
            Segments = vadResult.Segments.Select(s => new VadSegmentInfo
            {
                Index = s.Index,
                Start = s.Start,
                End = s.End,
                Duration = s.Duration,
                IsSpeech = s.IsSpeech
            }).ToList(),
            SplitFiles = splitFiles ?? new List<string>()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(vadInfo, options);
        File.WriteAllText(outputPath, json, Encoding.UTF8);

        progress?.Report($"[VideoVadWorkflowService] VAD信息已保存: {outputPath}");
    }

    #endregion

    #region 完整VAD工作流

    public async Task<VadWorkflowResult> ExecuteFullVadWorkflowAsync(
        string videoPath,
        string? outputWavPath = null,
        decimal vadThreshold = 0.5m,
        int minSpeechDurationMs = 250,
        int minSilenceDurationMs = 100,
        bool splitVideo = true,
        string? outputFolder = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new VadWorkflowResult
        {
            VideoPath = videoPath,
            Success = false
        };

        try
        {
            progress?.Report("[VideoVadWorkflowService] 开始执行完整VAD工作流");
            progress?.Report($"  媒体文件: {videoPath}");

            #region 步骤1: 检查媒体文件类型和音频流
            progress?.Report("[VideoVadWorkflowService] 步骤1: 检查媒体文件类型和音频流");
            
            var extension = Path.GetExtension(videoPath).ToLower();
            var audioExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac", ".flac", ".ogg", ".wma", ".opus" };
            var isAudioFile = audioExtensions.Contains(extension);
            
            if (isAudioFile)
            {
                progress?.Report("[VideoVadWorkflowService] 检测到纯音频文件，直接使用");
                result.AudioPath = videoPath;
            }
            else
            {
                var hasAudio = await HasAudioStreamAsync(videoPath);

                if (!hasAudio)
                {
                    progress?.Report("[VideoVadWorkflowService] 视频文件不包含音频流，无法进行VAD检测");
                    result.Success = false;
                    result.ErrorMessage = "视频文件不包含音频流，无法进行VAD检测";
                    result.TotalDuration = stopwatch.Elapsed;
                    progress?.Report($"[VideoVadWorkflowService] VAD工作流执行失败（无音频流），总耗时: {result.TotalDuration.TotalSeconds:F2}秒");
                    return result;
                }
            }
            #endregion

            #region 步骤2: 从视频提取音频（如果是视频文件）
            if (!isAudioFile)
            {
                progress?.Report("[VideoVadWorkflowService] 步骤2: 从视频提取音频");
                var wavPath = await ExtractAudioFromVideoAsync(videoPath, outputWavPath);
                result.AudioPath = wavPath;
            }
            else
            {
                progress?.Report("[VideoVadWorkflowService] 步骤2: 音频文件无需提取，直接使用");
            }
            #endregion

            #region 步骤3: 执行VAD检测
            progress?.Report("[VideoVadWorkflowService] 步骤3: 执行VAD检测");
            
            if (string.IsNullOrEmpty(result.AudioPath))
            {
                throw new InvalidOperationException("音频路径为空，无法执行VAD检测");
            }
            
            var vadResult = await ExecuteVadDetectionAsync(
                result.AudioPath,
                vadThreshold,
                minSpeechDurationMs,
                minSilenceDurationMs);
            result.VadResult = vadResult;
            #endregion

            #region 步骤4: 分割视频（可选）
            if (splitVideo && !isAudioFile)
            {
                progress?.Report("[VideoVadWorkflowService] 步骤4: 根据VAD结果分割视频");
                outputFolder ??= Path.Combine(Path.GetDirectoryName(videoPath) ?? "", "segments");
                var splitFiles = SplitVideoByVadSegments(videoPath, vadResult, outputFolder);
                result.SplitFiles = splitFiles;
                result.OutputFolder = outputFolder;
            }
            #endregion

            #region 步骤5: 保存VAD信息
            progress?.Report("[VideoVadWorkflowService] 步骤5: 保存VAD信息");
            var vadInfoPath = Path.Combine(
                Path.GetDirectoryName(videoPath) ?? "",
                "vad_info.json");
            SaveVadInfo(vadInfoPath, vadResult, result.SplitFiles);
            result.VadInfoPath = vadInfoPath;
            #endregion

            result.Success = true;
            result.TotalDuration = stopwatch.Elapsed;
            progress?.Report($"[VideoVadWorkflowService] 完整VAD工作流执行成功，总耗时: {result.TotalDuration.TotalSeconds:F2}秒");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.TotalDuration = stopwatch.Elapsed;
            progress?.Error($"[VideoVadWorkflowService] 完整VAD工作流执行失败: {ex.Message}");
            Log.Error(ex, "完整VAD工作流执行失败: {VideoPath}", videoPath);
        }

        return result;
    }

    #endregion

    #region 使用音频文件执行VAD工作流

    public async Task<VadWorkflowResult> ExecuteVadWorkflowWithAudioAsync(
        string audioPath,
        string? outputWavPath = null,
        decimal vadThreshold = 0.5m,
        int minSpeechDurationMs = 250,
        int minSilenceDurationMs = 100,
        bool splitVideo = false,
        string? videoPath = null,
        string? outputFolder = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new VadWorkflowResult
        {
            Success = false
        };

        try
        {
            progress?.Report("[VideoVadWorkflowService] 开始执行VAD工作流（使用音频文件）");
            progress?.Report($"  音频文件: {audioPath}");

            if (!File.Exists(audioPath))
            {
                throw new FileNotFoundException($"音频文件不存在: {audioPath}");
            }

            #region 步骤1: 转换音频为WAV格式
            progress?.Report("[VideoVadWorkflowService] 步骤1: 转换音频为WAV格式");
            var wavPath = await ConvertAudioToWavAsync(audioPath);
            result.AudioPath = wavPath;
            #endregion

            #region 步骤2: 执行VAD检测
            progress?.Report("[VideoVadWorkflowService] 步骤2: 执行VAD检测");
            var vadResult = await ExecuteVadDetectionAsync(
                wavPath,
                vadThreshold,
                minSpeechDurationMs,
                minSilenceDurationMs);
            result.VadResult = vadResult;
            #endregion

            #region 步骤3: 分割视频（可选）
            if (splitVideo && !string.IsNullOrEmpty(videoPath))
            {
                progress?.Report("[VideoVadWorkflowService] 步骤3: 根据VAD结果分割视频");
                outputFolder ??= Path.Combine(Path.GetDirectoryName(videoPath) ?? "", "segments");
                var splitFiles = SplitVideoByVadSegments(videoPath, vadResult, outputFolder);
                result.SplitFiles = splitFiles;
                result.OutputFolder = outputFolder;
                result.VideoPath = videoPath;
            }
            #endregion

            #region 步骤4: 保存VAD信息
            progress?.Report("[VideoVadWorkflowService] 步骤4: 保存VAD信息");
            var vadInfoPath = Path.Combine(
                Path.GetDirectoryName(audioPath) ?? "",
                "vad_info.json");
            SaveVadInfo(vadInfoPath, vadResult, result.SplitFiles);
            result.VadInfoPath = vadInfoPath;
            #endregion

            result.Success = true;
            result.TotalDuration = stopwatch.Elapsed;
            progress?.Report($"[VideoVadWorkflowService] VAD工作流执行成功，总耗时: {result.TotalDuration.TotalSeconds:F2}秒");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.TotalDuration = stopwatch.Elapsed;
            progress?.Error($"[VideoVadWorkflowService] VAD工作流执行失败: {ex.Message}");
            Log.Error(ex, "VAD工作流执行失败: {AudioPath}", audioPath);
        }

        return result;
    }

    #endregion
}

#region 数据模型

public class VadWorkflowResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string VideoPath { get; set; } = string.Empty;
    public string? AudioPath { get; set; }
    public VadDetectionResult? VadResult { get; set; }
    public List<string> SplitFiles { get; set; } = new();
    public string? OutputFolder { get; set; }
    public string? VadInfoPath { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public class VadInfo
{
    public string? AudioPath { get; set; }
    public decimal AudioDuration { get; set; }
    public int SpeechSegmentCount { get; set; }
    public int SilenceSegmentCount { get; set; }
    public decimal TotalSpeechDuration { get; set; }
    public decimal TotalSilenceDuration { get; set; }
    public List<VadSegmentInfo> Segments { get; set; } = new();
    public List<string> SplitFiles { get; set; } = new();
}

public class VadSegmentInfo
{
    public int Index { get; set; }
    public decimal Start { get; set; }
    public decimal End { get; set; }
    public decimal Duration { get; set; }
    public bool IsSpeech { get; set; }
}

#endregion
