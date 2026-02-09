using VadTimeProcessor.Models;
using VideoTranslator.Interfaces;
using VT.Core;

namespace VadTimeProcessor.Services;

/// <summary>
/// VAD检测器 - 负责使用whisper-vad-speech-segments.exe进行语音活动检测
/// </summary>
public static class VadDetector
{
    #region 私有字段

    /// <summary>
    /// 默认VAD可执行文件路径
    /// </summary>
    //private const string DefaultVadExecutablePath = @"d:\VideoTranslator\whisper.cpp\whisper-vad-speech-segments.exe";
    private const string DefaultVadExecutablePath = @"d:\VideoTranslator\whisper.cpp\whisper-vad-speech-segments-new.exe";
    /// <summary>
    /// 默认VAD模型路径
    /// </summary>
    private const string DefaultVadModelPath = @"d:\VideoTranslator\whisper.cpp\ggml-silero-v6.2.0-vad.bin";

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

    ///// <summary>
    ///// 执行VAD检测并返回语音段落链表
    ///// </summary>
    ///// <param name="audioPath">音频文件路径</param>
    ///// <param name="vadModelPath">VAD模型路径（可选，默认使用内置路径）</param>
    ///// <returns>语音段落链表</returns>
    //public static LinkedList<SpeechSegment> DetectSpeechSegments(
    //    string audioPath, 
    //    string? vadModelPath = null)
    //{
    //    return DetectSpeechSegments(
    //        audioPath, 
    //        vadModelPath ?? DefaultVadModelPath,
    //        threshold: 0.50,
    //        minSpeechDurationMs: 250,
    //        minSilenceDurationMs: 100,
    //        maxSpeechDurationS: double.MaxValue,
    //        speechPadMs: 30,
    //        samplesOverlap: 0.10,
    //        threads: 4,
    //        noPrints: false
    //    );
    //}

    /// <summary>
    /// 执行VAD检测并返回语音段落链表（使用自定义参数）
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="vadModelPath">VAD模型路径</param>
    /// <param name="threshold">VAD阈值（0.0-1.0，默认0.50）</param>
    /// <param name="minSpeechDurationMs">最小语音时长（毫秒，默认250）</param>
    /// <param name="minSilenceDurationMs">最小静音时长（毫秒，默认100）</param>
    /// <param name="maxSpeechDurationS">最大语音时长（秒，默认无限制）</param>
    /// <param name="speechPadMs">语音填充（毫秒，默认30）</param>
    /// <param name="samplesOverlap">段落重叠（秒，默认0.10）</param>
    /// <param name="threads">线程数（默认4）</param>
    /// <param name="useGpu">是否使用GPU加速（默认false）</param>
    /// <param name="noPrints">是否不打印额外信息（默认false）</param>
    /// <param name="isDebug">是否显示调试信息（默认false）</param>
    /// <returns>语音段落链表</returns>
    public static LinkedList<ISpeechSegment> DetectSpeechSegments(
        string audioPath,
        string vadModelPath = DefaultVadModelPath,
        double threshold = 0.50,
        int minSpeechDurationMs = 250,
        int minSilenceDurationMs = 100,
        double maxSpeechDurationS = 15,
        int speechPadMs = 30,
        double samplesOverlap = 0.10,
        int threads = 4,
        bool useGpu = false,
        bool noPrints = false,
        bool isDebug = false,
        bool autoMerge = true)
    {
        #region 验证参数

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件未找到: {audioPath}");
        }

        if (!File.Exists(vadModelPath))
        {
            throw new FileNotFoundException($"VAD模型文件未找到: {vadModelPath}");
        }

        if (!File.Exists(DefaultVadExecutablePath))
        {
            throw new FileNotFoundException($"VAD可执行文件未找到: {DefaultVadExecutablePath}");
        }

        #endregion

        #region 构建命令参数

        var arguments = new List<string>
        {
            $"-f \"{audioPath}\"",
            $"--vad-model \"{vadModelPath}\"",
            $"--vad-threshold {threshold:F2}",
            $"--vad-min-speech-duration-ms {minSpeechDurationMs}",
            $"--vad-min-silence-duration-ms {minSilenceDurationMs}",
            $"--vad-speech-pad-ms {speechPadMs}",
            $"--vad-samples-overlap {samplesOverlap:F2}",
            $"-t {threads}"
        };

        if (useGpu)
        {
            arguments.Add("-ug");
        }

        if (maxSpeechDurationS != double.MaxValue)
        {
            arguments.Add($"--vad-max-speech-duration-s {maxSpeechDurationS:F2}");
        }

        if (noPrints)
        {
            arguments.Add("-np");
        }

        if (!autoMerge)
        {
            arguments.Add("-vna");
        }

        string commandArgs = string.Join(" ", arguments);

        #endregion

        #region 执行VAD检测

        _progressService?.Title("执行VAD检测");
        _progressService?.Report($"音频: {audioPath}");
        _progressService?.Report($"模型: {vadModelPath}");
        _progressService?.Report($"阈值: {threshold:F2}");
        _progressService?.Report($"最小语音时长: {minSpeechDurationMs}ms");
        _progressService?.Report($"最小静音时长: {minSilenceDurationMs}ms");
        _progressService?.Report($"线程数: {threads}");
        _progressService?.Report($"命令参数: {DefaultVadExecutablePath} {commandArgs}");

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = DefaultVadExecutablePath,
            Arguments = commandArgs,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("启动VAD进程失败");
        }

        _progressService?.Report("VAD进程已启动，正在读取输出...");

        #region 异步读取输出

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                if (isDebug)
                    _progressService?.Report($"[STDOUT] {e.Data}", MessageType.Debug);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                if (isDebug)
                    _progressService?.Report($"[STDERR] {e.Data}", MessageType.Debug);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        #endregion

        string output = outputBuilder.ToString();
        string error = errorBuilder.ToString();

        _progressService?.Report($"VAD进程已退出，退出码: {process.ExitCode}");
        _progressService?.Report($"标准输出长度: {output?.Length ?? 0}");
        _progressService?.Report($"错误输出长度: {error?.Length ?? 0}");

        if (process.ExitCode != 0)
        {
            _progressService?.Error($"VAD错误: {error}");
            throw new InvalidOperationException($"VAD执行失败，退出码: {process.ExitCode}");
        }

        #region 合并输出

        string fullOutput = string.IsNullOrEmpty(output) ? error : (output + error);

        if (string.IsNullOrWhiteSpace(fullOutput))
        {
            throw new InvalidOperationException("VAD未返回任何输出");
        }

        #endregion

        _progressService?.Report($"VAD输出总长度: {fullOutput.Length}");

        #endregion

        _progressService?.Success("VAD检测完成！");

        #region 解析输出并构建段落链表

        var segments = ParseVadOutput(fullOutput);
        _progressService?.Report($"解析到 {segments.Count} 个语音段落");

        return BuildLinkedList(segments);

        #endregion
    }

    /// <summary>
    /// 执行VAD检测并保存结果到文件
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="vadModelPath">VAD模型路径（可选，默认使用内置路径）</param>
    /// <returns>语音段落链表</returns>
    public static LinkedList<ISpeechSegment> DetectSpeechSegmentsAndSave(
        string audioPath,
        string outputPath,
        string? vadModelPath = null)
    {
        var segments = DetectSpeechSegments(
            audioPath,
            vadModelPath ?? DefaultVadModelPath,
            threshold: 0.50,
            minSpeechDurationMs: 250,
            minSilenceDurationMs: 100,
            maxSpeechDurationS: double.MaxValue,
            speechPadMs: 30,
            samplesOverlap: 0.10,
            threads: 4,
            noPrints: false
        );

        #region 保存VAD输出到文件

        var lines = new List<string>();
        var currentNode = segments.First;

        while (currentNode != null)
        {
            var segment = currentNode.Value;
            double startSeconds = segment.StartMS / 1000;
            double endSeconds = segment.EndMS / 1000;
            lines.Add($"Speech segment {segment.Index}: start = {startSeconds:F2}, end = {endSeconds:F2}");
            currentNode = currentNode.Next;
        }

        File.WriteAllLines(outputPath, lines);
        _progressService?.Report($"VAD输出已保存到: {outputPath}");

        #endregion

        return segments;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 解析VAD输出文本
    /// </summary>
    /// <param name="vadOutput">VAD输出文本</param>
    /// <returns>语音段落列表</returns>
    private static List<ISpeechSegment> ParseVadOutput(string vadOutput)
    {
        var segments = new List<ISpeechSegment>();
        var lines = vadOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var segment = ParseLine(line);
            if (segment != null)
            {
                segments.Add(segment);
            }
        }

        return segments;
    }

    /// <summary>
    /// 解析单行VAD输出
    /// </summary>
    /// <param name="line">VAD输出行</param>
    /// <returns>语音段落对象</returns>
    private static SpeechSegment? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            line,
            @"Speech segment (\d+): start = ([\d.]+), end = ([\d.]+)"
        );

        if (!match.Success)
        {
            return null;
        }

        int index = int.Parse(match.Groups[1].Value);
        double start = double.Parse(match.Groups[2].Value) * 10;
        double end = double.Parse(match.Groups[3].Value) * 10;

        return new SpeechSegment(index, start, end);
    }

    /// <summary>
    /// 构建语音段落链表并建立导航链接
    /// </summary>
    /// <param name="segments">语音段落列表</param>
    /// <returns>语音段落链表</returns>
    private static LinkedList<ISpeechSegment> BuildLinkedList(List<ISpeechSegment> segments)
    {
        var linkedList = new LinkedList<ISpeechSegment>();

        #region 添加段落到链表

        foreach (var segment in segments)
        {
            linkedList.AddLast(segment);
        }

        #endregion

        #region 建立导航链接

        var currentNode = linkedList.First;
        while (currentNode != null)
        {
            var segment = currentNode.Value;

            if (currentNode.Previous != null)
            {
                segment.Previous = currentNode.Previous.Value;
            }

            if (currentNode.Next != null)
            {
                segment.Next = currentNode.Next.Value;
            }

            currentNode = currentNode.Next;
        }

        #endregion

        return linkedList;
    }

    /// <summary>
    /// 保存VAD输出到文件
    /// </summary>
    /// <param name="segments">语音段落链表</param>
    /// <param name="outputPath">输出文件路径</param>
    private static void SaveVadOutput(LinkedList<ISpeechSegment> segments, string outputPath)
    {
        var lines = new List<string>();

        var currentNode = segments.First;
        while (currentNode != null)
        {
            var segment = currentNode.Value;
            double startSeconds = segment.StartMS / 1000;
            double endSeconds = segment.EndMS / 1000;
            lines.Add($"Speech segment {segment.Index}: start = {startSeconds:F2}, end = {endSeconds:F2}");
            currentNode = currentNode.Next;
        }

        File.WriteAllLines(outputPath, lines);
        _progressService?.Report($"VAD输出已保存到: {outputPath}");
    }

    #endregion
}
