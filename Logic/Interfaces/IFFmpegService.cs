using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoTranslator.Config;
using VideoTranslator.Services;
using VideoTranslator.Utils;

namespace VideoTranslator.Interfaces;

public enum StreamType
{
    Video,
    Audio,
    Subtitle,
    Data
}

public class VideoStreamInfo
{
    public int Index { get; set; }
    public StreamType Type { get; set; }
    public string CodecName { get; set; } = string.Empty;
    public string CodecLongName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int? BitRate { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? FrameRate { get; set; }
    public int? SampleRate { get; set; }
    public int? Channels { get; set; }
}

public class StreamSeparationResult
{
    public string InputVideoPath { get; set; } = string.Empty;
    public List<VideoStreamInfo> Streams { get; set; } = new();
    public Dictionary<int, string> ExtractedFilePaths { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IFFmpegService
{
    public string FfmpegPath { get; }
    Task<string> ExecuteCommandAsync(string arguments);
    Task<string> ExecuteCommandAsync(List<string> arguments);
    Task<string> ExecuteCommandAsync(string arguments, TimeSpan? timeout);
    Task<string> ExecuteCommandAsync(List<string> arguments, TimeSpan? timeout);
}
public class FFmpegService : ServiceBase, IFFmpegService
{
    public string FfmpegPath=> _ffmpegPath;
    private readonly string _ffmpegPath;
    private readonly FFmpegProgressParser _progressParser;
    private DateTime _lastProgressUpdateTime = DateTime.MinValue;
    private const int PROGRESS_UPDATE_INTERVAL_MS = 500;

    public FFmpegService() : base()
    {
        progress?.Report($"[FFmpegService] 初始化，配置路径: {Settings.FfmpegPath}");
        _ffmpegPath = ResolvePath(Settings.FfmpegPath);
        progress?.Report($"[FFmpegService] 解析后的路径: {_ffmpegPath}");
        _progressParser = new FFmpegProgressParser();
    }

    private string ResolvePath(string path)
    {
        progress?.Report($"[FFmpegService] ResolvePath 输入: {path}");

        if (string.IsNullOrWhiteSpace(path))
        {
            progress?.Report("[FFmpegService] 路径为空，使用默认: ffmpeg");
            return "ffmpeg";
        }

        if (Path.IsPathRooted(path))
        {
            progress?.Report($"[FFmpegService] 路径是绝对路径: {path}");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"FFmpeg 可执行文件不存在: {path}\n请检查 App.config 中的 VideoTranslator_FfmpegPath 配置是否正确。", path);
            }
            return path;
        }

        var basePath = GetBaseDirectory();
        progress?.Report($"[FFmpegService] 基础目录: {basePath}");

        var fullPath = Path.GetFullPath(Path.Combine(basePath, path));
        progress?.Report($"[FFmpegService] 完整路径: {fullPath}");

        if (File.Exists(fullPath))
        {
            progress?.Report($"[FFmpegService] 文件存在: {fullPath}");
            return fullPath;
        }

        throw new FileNotFoundException($"FFmpeg 可执行文件不存在: {fullPath}\n请检查 App.config 中的 VideoTranslator_FfmpegPath 配置是否正确。\n当前配置路径: {path}\n基础目录: {basePath}", fullPath);
    }

    private string GetBaseDirectory()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        progress?.Report($"[FFmpegService] 程序集位置: {assemblyLocation}");

        if (string.IsNullOrEmpty(assemblyLocation))
        {
            var currentDir = Directory.GetCurrentDirectory();
            progress?.Report($"[FFmpegService] 程序集位置为空，使用当前目录: {currentDir}");
            return currentDir;
        }

        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        progress?.Report($"[FFmpegService] 程序集目录: {assemblyDirectory}");

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            var currentDir = Directory.GetCurrentDirectory();
            progress?.Report($"[FFmpegService] 程序集目录为空，使用当前目录: {currentDir}");
            return currentDir;
        }

        return assemblyDirectory;
    }

    public async Task<string> ExecuteCommandAsync(string arguments) => await ExecuteCommandAsync(arguments, null);

    public async Task<string> ExecuteCommandAsync(List<string> arguments) => await ExecuteCommandAsync(string.Join(" ", arguments));

    public async Task<string> ExecuteCommandAsync(string arguments, TimeSpan? timeout)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = processInfo };
        progress.Report($"[FFmpeg] {_ffmpegPath} {arguments}");

        _progressParser.Reset();

        process.Start();

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        var outputTask = Task.Run(() =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                if (line != null)
                {
                    outputBuilder.AppendLine(line);
                    _progressParser.ParseLine(line);
                    ReportProgressIfAvailable();
                }
            }
        });

        var errorTask = Task.Run(() =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = process.StandardError.ReadLine();
                if (line != null)
                {
                    errorBuilder.AppendLine(line);
                    _progressParser.ParseLine(line);
                    ReportProgressIfAvailable();
                }
            }
        });

        var completionTask = Task.WhenAll(outputTask, errorTask);
        if (timeout.HasValue)
        {
            var timeoutTask = Task.Delay(timeout.Value);
            var completedTask = await Task.WhenAny(completionTask, timeoutTask);

            if (completedTask == timeoutTask && !process.HasExited)
            {
                progress?.Report($"[FFmpeg] 命令超时，尝试终止进程...");
                try
                {
                    process.Kill(true);
                    await process.WaitForExitAsync();
                }
                catch { }
                throw new TimeoutException($"FFmpeg command timed out after {timeout.Value}");
            }
        }
        await process.WaitForExitAsync();
        await completionTask;

        _lastProgressUpdateTime = DateTime.MinValue;
        ReportProgressIfAvailable();

        if (process.ExitCode != 0)
        {
            progress?.Report($"[FFmpeg] 退出代码: {process.ExitCode}");
            progress.ReportProgress(100);

            throw new Exception($"FFmpeg command failed with exit code {process.ExitCode}. Error: {errorBuilder}");
        }

        return outputBuilder.ToString() + errorBuilder.ToString();
    }

    private void ReportProgressIfAvailable()
    {
        var now = DateTime.Now;
        var elapsed = (now - _lastProgressUpdateTime).TotalMilliseconds;

        if (elapsed < PROGRESS_UPDATE_INTERVAL_MS)
        {
            return;
        }

        _lastProgressUpdateTime = now;

        var progressPercentage = _progressParser.ProgressPercentage;
        if (progressPercentage.HasValue)
        {
            var currentTime = _progressParser.CurrentTime;
            var totalTime = _progressParser.TotalDuration;
            var message = $"FFmpeg 进度: {progressPercentage.Value:F1}%";
            if (currentTime.HasValue && totalTime.HasValue)
            {
                message += $" ({currentTime.Value:hh\\:mm\\:ss} / {totalTime.Value:hh\\:mm\\:ss})";
            }
            progress.ReportProgress(progressPercentage.Value, message);
        }
    }

    public async Task<string> ExecuteCommandAsync(List<string> arguments, TimeSpan? timeout) => await ExecuteCommandAsync(string.Join(" ", arguments), timeout);
}


public static class FfmpegExtend
{
    /// <summary>
    /// 返回一个视频中是否有视频,音频流
    /// </summary>
    /// <param name="ffmpeg"></param>
    /// <param name="audioPath"></param>
    /// <returns></returns>
    public static async Task<(bool HasAudio, bool HasVideo)> GetVideoStreamInfo(this IFFmpegService ffmpeg, string videoPath)
    {
        var list = await ffmpeg.GetVideoStreamDetails(videoPath);
        var hasVideo = list.Any(x => x.Type == StreamType.Video);
        var hasAudio = list.Any(x=>x.Type == StreamType.Audio);
        return (hasAudio, hasVideo);
    }

    /// <summary>
    /// 获取视频文件的详细流信息
    /// </summary>
    /// <param name="ffmpeg">FFmpeg服务实例</param>
    /// <param name="videoPath">视频文件路径</param>
    /// <returns>流信息列表</returns>
    public static async Task<List<VideoStreamInfo>> GetVideoStreamDetails(this IFFmpegService ffmpeg, string videoPath)
    {
        videoPath.ValidateFileExists();

        // 使用ffprobe获取流信息（使用ffprobe会更快更准确）
        var args = $"-i \"{videoPath}\" -f null -";
        var output = await ffmpeg.ExecuteCommandAsync(args);

        var streams = new List<VideoStreamInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        VideoStreamInfo? currentStream = null;

        foreach (var line in lines)
        {
            // 检测流信息行，例如: "    Stream #0:0(und): Video: h264 (High) (avc1 / 0x31637661), yuv420p, 1920x1080, 24 fps"
            // 或者: "    Stream #0:1[0x2](und): Audio: aac (LC) (mp4a / 0x6134706D), 44100 Hz, stereo, fltp, 128 kb/s"
            var streamMatch = System.Text.RegularExpressions.Regex.Match(line, @"Stream\s+#\d+:(\d+)(?:\[0x[0-9a-fA-F]+\])?(?:\((\w+)\))?:\s+(\w+):");

            if (streamMatch.Success)
            {
                // 保存前一个流（如果有）
                if (currentStream != null)
                {
                    streams.Add(currentStream);
                }

                // 创建新流
                currentStream = new VideoStreamInfo
                {
                    Index = int.Parse(streamMatch.Groups[1].Value),
                    Type = ParseStreamType(streamMatch.Groups[3].Value),
                    Language = streamMatch.Groups[2].Value
                };

                // 提取编解码器名称
                var codecMatch = System.Text.RegularExpressions.Regex.Match(line, @"Stream\s+#\d+:\d+(?:\(\w+\))?:\s+\w+:\s*(\w+)");
                if (codecMatch.Success)
                {
                    currentStream.CodecName = codecMatch.Groups[1].Value;
                }
            }
            else if (currentStream != null)
            {
                // 解析视频流的其他属性
                if (line.Contains("Video:"))
                {
                    var sizeMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)x(\d+)");
                    if (sizeMatch.Success)
                    {
                        currentStream.Width = int.Parse(sizeMatch.Groups[1].Value);
                        currentStream.Height = int.Parse(sizeMatch.Groups[2].Value);
                    }

                    var fpsMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.?\d*)\s*fps");
                    if (fpsMatch.Success)
                    {
                        currentStream.FrameRate = double.Parse(fpsMatch.Groups[1].Value);
                    }
                }

                // 解析音频流的其他属性
                if (line.Contains("Audio:"))
                {
                    var sampleRateMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*Hz");
                    if (sampleRateMatch.Success)
                    {
                        currentStream.SampleRate = int.Parse(sampleRateMatch.Groups[1].Value);
                    }

                    var channelsMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*channel");
                    if (channelsMatch.Success)
                    {
                        currentStream.Channels = int.Parse(channelsMatch.Groups[1].Value);
                    }
                }

                // 解析比特率
                var bitrateMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*kb/s");
                if (bitrateMatch.Success)
                {
                    currentStream.BitRate = int.Parse(bitrateMatch.Groups[1].Value) * 1000;
                }
            }
        }

        // 添加最后一个流
        if (currentStream != null)
        {
            streams.Add(currentStream);
        }

        return streams;
    }

    /// <summary>
    /// 将视频文件的所有流分离为单独的文件(无需预先检测,像解压一样直接提取)
    /// </summary>
    /// <param name="ffmpeg">FFmpeg服务实例</param>
    /// <param name="videoPath">视频文件路径</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <returns>分离结果</returns>
    public static async Task<StreamSeparationResult> SeparateVideoStreams(this IFFmpegService ffmpeg, string videoPath, string outputDirectory)
    {
        videoPath.ValidateFileExists();

        var result = new StreamSeparationResult
        {
            InputVideoPath = videoPath,
            Success = false
        };

        try
        {
            Directory.CreateDirectory(outputDirectory);

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoPath);
            var inputExtension = Path.GetExtension(videoPath);

            // 先探测流信息
            var streams = await ffmpeg.GetVideoStreamDetails(videoPath);
            result.Streams = streams;

            if (!streams.Any())
            {
                result.ErrorMessage = "未检测到任何流";
                return result;
            }

            // 构建批量提取命令
            var args = new List<string> { $"-i \"{videoPath}\"" };

            // 按流类型分组
            var videoStreams = streams.Where(s => s.Type == StreamType.Video).ToList();
            var audioStreams = streams.Where(s => s.Type == StreamType.Audio).ToList();
            var subtitleStreams = streams.Where(s => s.Type == StreamType.Subtitle).ToList();

            // 为每个流添加映射和输出
            for (int i = 0; i < videoStreams.Count; i++)
            {
                var stream = videoStreams[i];
                var outputPath = Path.Combine(outputDirectory, $"{fileNameWithoutExt}_video_{stream.Index}{inputExtension}");
                args.Add($"-map 0:v:{i} -c:v copy -y \"{outputPath}\"");
            }

            for (int i = 0; i < audioStreams.Count; i++)
            {
                var stream = audioStreams[i];
                var outputPath = Path.Combine(outputDirectory, $"{fileNameWithoutExt}_audio_{stream.Index}.aac");
                args.Add($"-map 0:a:{i} -c:a copy -y \"{outputPath}\"");
            }

            for (int i = 0; i < subtitleStreams.Count; i++)
            {
                var stream = subtitleStreams[i];
                var outputPath = Path.Combine(outputDirectory, $"{fileNameWithoutExt}_subtitle_{stream.Index}.srt");
                args.Add($"-map 0:s:{i} -c:s copy -y \"{outputPath}\"");
            }

            // 执行一次性提取所有流的命令
            await ffmpeg.ExecuteCommandAsync(args);

            // 验证提取的文件
            foreach (var stream in streams)
            {
                string outputPath;
                if (stream.Type == StreamType.Video)
                {
                    outputPath = Path.Combine(outputDirectory, $"{fileNameWithoutExt}_video_{stream.Index}{inputExtension}");
                }
                else if (stream.Type == StreamType.Audio)
                {
                    outputPath = Path.Combine(outputDirectory, $"{fileNameWithoutExt}_audio_{stream.Index}.aac");
                }
                else if (stream.Type == StreamType.Subtitle)
                {
                    outputPath = Path.Combine(outputDirectory, $"{fileNameWithoutExt}_subtitle_{stream.Index}.srt");
                }
                else
                {
                    continue;
                }

                if (File.Exists(outputPath))
                {
                    result.ExtractedFilePaths[stream.Index] = outputPath;
                }
            }

            result.Success = result.ExtractedFilePaths.Count > 0;

            if (result.Success)
            {
                result.ErrorMessage = null;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 快速分离视频和音频流（仅分离主视频和主音频）
    /// </summary>
    /// <param name="ffmpeg">FFmpeg服务实例</param>
    /// <param name="videoPath">视频文件路径</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <returns>分离结果，包含视频和音频文件路径</returns>
    public static async Task<List<VideoStreamInfo>> SeparateMainVideoAndAudio(
        this IFFmpegService ffmpeg,
        string videoPath,
        string outMuteVideoPath,
        string outAudioPath
        )
    {
        videoPath.ValidateFileExists();
        outMuteVideoPath.CreateDirectory();
        outAudioPath.CreateDirectory();

        var streams = await ffmpeg.GetVideoStreamDetails(videoPath);
        var inputExtension = Path.GetExtension(videoPath);

        var mainVideoStream = streams.FirstOrDefault(s => s.Type == StreamType.Video);
        if (mainVideoStream != null)
        {
            var videoIndex = streams.Where(s => s.Type == StreamType.Video).ToList().IndexOf(mainVideoStream);
            var args = $"-i \"{videoPath}\" -map 0:v:{videoIndex} -c copy -y \"{outMuteVideoPath}\"";
            await ffmpeg.ExecuteCommandAsync(args);
        }
        else 
        {
            throw new Exception($"{videoPath} 没有视频流!"); 
        }
        var mainAudioStream = streams.FirstOrDefault(s => s.Type == StreamType.Audio);
        if (mainAudioStream != null)
        {
            var audioIndex = streams.Where(s => s.Type == StreamType.Audio).ToList().IndexOf(mainAudioStream);
            var args = $"-i \"{videoPath}\" -map 0:a:{audioIndex} -c:a pcm_s16le -ar 16000 -ac 1 -y \"{outAudioPath}\"";
            await ffmpeg.ExecuteCommandAsync(args);
        }
        else
        {
            throw new Exception($"{videoPath} 没有音频流!");
        }
        return streams;
    }

    /// <summary>
    /// 提取视频流部分（仅提取视频，不包含音频）
    /// </summary>
    /// <param name="ffmpeg">FFmpeg服务实例</param>
    /// <param name="videoPath">视频文件路径</param>
    /// <param name="outputPath">输出文件路径（可选，默认在原文件同目录下添加_video后缀）</param>
    /// <returns>提取的视频文件路径</returns>
    public static async Task<string?> ExtractVideoStream(this IFFmpegService ffmpeg, string videoPath, string? outputPath = null)
    {
        videoPath.ValidateFileExists();

        #region 设置输出路径

        if (string.IsNullOrEmpty(outputPath))
        {
            var directory = Path.GetDirectoryName(videoPath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoPath);
            var extension = Path.GetExtension(videoPath);
            outputPath = Path.Combine(directory ?? "", $"{fileNameWithoutExt}_video{extension}");
        }

        #endregion

        #region 获取视频流信息

        var streams = await ffmpeg.GetVideoStreamDetails(videoPath);
        var videoStream = streams.FirstOrDefault(s => s.Type == StreamType.Video);

        if (videoStream == null)
        {
            throw new InvalidOperationException("未找到视频流");
        }

        #endregion

        #region 构建FFmpeg命令

        var args = $"-i \"{videoPath}\" -c:v copy -an -y \"{outputPath}\"";

        #endregion

        #region 执行提取

        await ffmpeg.ExecuteCommandAsync(args);

        #endregion

        return outputPath;
    }

    private static StreamType ParseStreamType(string typeString)
    {
        return typeString.ToLower() switch
        {
            "video" => StreamType.Video,
            "audio" => StreamType.Audio,
            "subtitle" => StreamType.Subtitle,
            "data" => StreamType.Data,
            _ => StreamType.Data
        };
    }
}