using VadTimeProcessor.Models;
using VideoTranslator.Interfaces;
using VT.Core;

namespace VadTimeProcessor.Services;

/// <summary>
/// 音频段落提取器 - 负责从原始音频中提取单个语音段落
/// </summary>
public static class AudioSegmentExtractor
{
    #region 私有字段

    /// <summary>
    /// 进度服务实例
    /// </summary>
    private static IProgressService? _progressService;

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置进度服务实例
    /// </summary>
    /// <param name="progressService">进度服务实例</param>
    public static void SetProgressService(IProgressService progressService)
    {
        _progressService = progressService;
    }

    /// <summary>
    /// 提取单个音频段落
    /// </summary>
    /// <param name="inputAudioPath">输入音频文件路径</param>
    /// <param name="segment">语音段落</param>
    /// <param name="outputPath">输出音频文件路径</param>
    public static void ExtractSegment(string inputAudioPath, ISpeechSegment segment, string outputPath)
    {
        #region 验证参数

        if (!File.Exists(inputAudioPath))
        {
            throw new FileNotFoundException($"输入音频文件未找到: {inputAudioPath}");
        }

        if (segment == null)
        {
            throw new ArgumentNullException(nameof(segment));
        }

        #endregion

        #region 构建FFmpeg命令

        double startSeconds = segment.StartMS / 1000;
        double durationSeconds = segment.DurationMS / 1000;

        var ffmpegPath = @"d:\VideoTranslator\ffmpeg\ffmpeg.exe";
        var arguments = $"-y -i \"{inputAudioPath}\" -ss {startSeconds:F3} -t {durationSeconds:F3} -c:a pcm_s16le \"{outputPath}\"";

        #endregion

        #region 执行FFmpeg命令

        _progressService?.Report($"提取段落 {segment.Index}: {segment.StartSeconds:F2}s-{segment.EndSeconds:F2}s");
        _progressService?.Report($"输出: {outputPath}");

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("启动FFmpeg进程失败");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            _progressService?.Error($"FFmpeg错误: {error}");
            throw new InvalidOperationException($"FFmpeg执行失败，退出码: {process.ExitCode}");
        }

        _progressService?.Success($"段落 {segment.Index} 提取成功！");

        #endregion
    }

    /// <summary>
    /// 批量提取音频段落
    /// </summary>
    /// <param name="inputAudioPath">输入音频文件路径</param>
    /// <param name="segments">语音段落链表</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="prefix">输出文件名前缀</param>
    /// <returns>提取的音频文件路径列表</returns>
    public static List<string> ExtractAllSegments(
        string inputAudioPath, 
        LinkedList<ISpeechSegment> segments, 
        string outputDirectory, 
        string prefix = "segment")
    {
        #region 验证参数

        if (!File.Exists(inputAudioPath))
        {
            throw new FileNotFoundException($"输入音频文件未找到: {inputAudioPath}");
        }

        if (segments == null || segments.Count == 0)
        {
            throw new ArgumentException("没有段落需要提取");
        }

        #endregion

        #region 创建输出目录

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        #endregion

        #region 批量提取段落

        _progressService?.Title("批量提取音频段落");
        _progressService?.Report($"输入: {inputAudioPath}");
        _progressService?.Report($"输出目录: {outputDirectory}");
        _progressService?.Report($"段落数: {segments.Count}");
        _progressService?.Report();

        var extractedFiles = new List<string>();
        var currentNode = segments.First;

        while (currentNode != null)
        {
            var segment = currentNode.Value;
            string outputPath = Path.Combine(outputDirectory, $"{prefix}_{segment.Index:D3}.wav");

            ExtractSegment(inputAudioPath, segment, outputPath);
            extractedFiles.Add(outputPath);

            currentNode = currentNode.Next;
        }

        _progressService?.Report($"共提取 {extractedFiles.Count} 个音频段落");
        _progressService?.Report();

        #endregion

        return extractedFiles;
    }

    #endregion
}
