﻿using System.Text;
using VadTimeProcessor.Models;
using VideoTranslator.Interfaces;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VadTimeProcessor.Services;

/// <summary>
/// Whisper转录器 - 负责使用Whisper进行语音识别和字幕生成
/// </summary>
public static class WhisperTranscriber
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
    /// 使用Whisper生成SRT字幕文件
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="outputPath">输出文件路径（不含扩展名）</param>
    /// <param name="modelPath">Whisper模型文件路径</param>
    /// <param name="language">语言代码，默认为auto自动检测</param>
    /// <returns>生成的SRT文件路径</returns>
    public static string GenerateSrtWithWhisper(
        string audioPath,
        string outputPath,
        string modelPath = @"d:\VideoTranslator\whisper.cpp\ggml-large-v3-turbo-q8_0.bin",
        string language = "auto")
    {
        #region 验证参数

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件未找到: {audioPath}");
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Whisper模型文件未找到: {modelPath}");
        }

        #endregion

        #region 构建Whisper命令

        var whisperPath = @"d:\VideoTranslator\whisper.cpp\whisper-cli.exe";
        var arguments = $"-f \"{audioPath}\" -osrt -l {language} -m \"{modelPath}\" -of \"{outputPath}\" -pp";

        #endregion

        #region 执行Whisper命令

        _progressService?.Title("使用Whisper生成SRT字幕");
        _progressService?.Report($"音频: {audioPath}");
        _progressService?.Report($"模型: {modelPath}");
        _progressService?.Report($"语言: {language}");
        _progressService?.Report($"输出: {outputPath}.srt");

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = whisperPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("启动Whisper进程失败");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            _progressService?.Error($"Whisper错误: {error}");
            throw new InvalidOperationException($"Whisper执行失败，退出码: {process.ExitCode}");
        }

        string srtPath = $"{outputPath}.srt";
        _progressService?.Success($"SRT字幕生成成功: {srtPath}");

        #endregion

        return srtPath;
    }

    /// <summary>
    /// 解析SRT字幕文件
    /// </summary>
    /// <param name="srtPath">SRT文件路径</param>
    /// <returns>字幕列表</returns>
    public static IEnumerable<ISrtSubtitle> ParseSrtFile(string srtPath)
    {
        #region 使用SRT.Core库解析

        var srtFile = SrtFile.Read(srtPath);

        return srtFile.Subtitles;

        #endregion
    }

    /// <summary>
    /// 保存SRT字幕文件
    /// </summary>
    /// <param name="subtitles">字幕列表</param>
    /// <param name="outputPath">输出文件路径</param>
    public static void SaveSrtFile(IEnumerable<ISrtSubtitle> subtitles, string outputPath)
    {
        #region 使用SRT.Core库保存

        if (subtitles == null || !subtitles.Any())
        {
            throw new ArgumentException("没有字幕需要保存");
        }

        var srtFile = new SrtFile(subtitles);
        srtFile.Write(outputPath);

        #endregion
    }

    /// <summary>
    /// 修正SRT字幕时间戳，将合并后音频的字幕时间戳映射回原始音频时间轴
    /// </summary>
    /// <param name="subtitles">字幕列表</param>
    /// <param name="originalSegments">原始语音段落链表</param>
    /// <param name="mergedSegments">合并后的语音段落链表</param>
    /// <returns>修正后的字幕列表</returns>
    public static IEnumerable<ISrtSubtitle> CorrectSrtTimestamps(
        IEnumerable<ISrtSubtitle> subtitles,
        LinkedList<SpeechSegment> originalSegments,
        LinkedList<SpeechSegment> mergedSegments)
    {
        #region 验证参数

        if (subtitles == null || !subtitles.Any())
        {
            throw new ArgumentException("没有字幕需要修正");
        }

        if (originalSegments == null || originalSegments.Count == 0)
        {
            throw new ArgumentException("未提供原始段落");
        }

        if (mergedSegments == null || mergedSegments.Count == 0)
        {
            throw new ArgumentException("未提供合并后的段落");
        }

        #endregion

        #region 构建合并段落到原始段落的映射

        var mergedToOriginalMap = new Dictionary<SpeechSegment, List<SpeechSegment>>();
        var originalNode = originalSegments.First;
        var mergedNode = mergedSegments.First;

        while (mergedNode != null && originalNode != null)
        {
            var mergedSegment = mergedNode.Value;
            var originalSegmentsForMerged = new List<SpeechSegment>();

            #region 收集此合并段落中的所有原始段落

            while (originalNode != null)
            {
                var originalSegment = originalNode.Value;

                originalSegmentsForMerged.Add(originalSegment);

                #region 检查是否为合并组中的最后一个段落

                if (originalNode.Next == null)
                {
                    originalNode = originalNode.Next;
                    break;
                }

                var gap = originalSegment.GetGapToNext() / 1000;
                if (gap >= 1.0)
                {
                    originalNode = originalNode.Next;
                    break;
                }

                originalNode = originalNode.Next;

                #endregion
            }

            #endregion

            mergedToOriginalMap[mergedSegment] = originalSegmentsForMerged;
            mergedNode = mergedNode.Next;
        }

        #endregion

        #region 计算每个合并段落的相对时间和偏移量

        var mergedSegmentInfo = new List<(SpeechSegment segment, double relativeStart, double relativeEnd, double originalStart)>();
        double currentTime = 0;

        _progressService?.Title("构建合并段落信息");

        foreach (var mergedSegment in mergedSegments)
        {
            double duration = mergedSegment.DurationMS;
            double relativeStart = currentTime;
            double relativeEnd = currentTime + duration;

            if (!mergedToOriginalMap.TryGetValue(mergedSegment, out var originalSegmentsForMerged) || originalSegmentsForMerged.Count == 0)
            {
                _progressService?.Warning($"合并段落 {mergedSegment.Index} 没有对应的原始段落");
                continue;
            }

            double originalStart = originalSegmentsForMerged[0].StartMS;
            double timeOffset = originalStart - relativeStart;

            _progressService?.Report($"合并段落 {mergedSegment.Index}: 原始起始 {originalStart:F0}ms, 相对时间 {relativeStart:F0}ms-{relativeEnd:F0}ms, 偏移量 {timeOffset:F0}ms");

            mergedSegmentInfo.Add((mergedSegment, relativeStart, relativeEnd, originalStart));
            currentTime = relativeEnd;
        }


        #endregion

        #region 修正每个字幕的时间戳

        var correctedSubtitles = new List<ISrtSubtitle>();

        foreach (var subtitle in subtitles)
        {
            double subtitleStartMs = subtitle.StartTime.TotalMilliseconds;
            double subtitleEndMs = subtitle.EndTime.TotalMilliseconds;

            #region 查找字幕所属的合并段落

            var targetMergedInfo = mergedSegmentInfo.FirstOrDefault(
                info => subtitleStartMs >= info.relativeStart && subtitleStartMs <= info.relativeEnd
            );

            if (targetMergedInfo.segment == null)
            {
                _progressService?.Warning($"无法找到字幕 {subtitle.Index} 所属的合并段落，跳过修正");
                correctedSubtitles.Add(subtitle);
                continue;
            }

            #endregion

            #region 计算修正后的时间戳

            double correctedStartMs = subtitleStartMs + (targetMergedInfo.originalStart - targetMergedInfo.relativeStart);
            double correctedEndMs = subtitleEndMs + (targetMergedInfo.originalStart - targetMergedInfo.relativeStart);

            var correctedSubtitle = new SrtSubtitle(
                subtitle.Index,
                TimeSpan.FromMilliseconds(correctedStartMs),
                TimeSpan.FromMilliseconds(correctedEndMs),
                subtitle.Text
            );

            correctedSubtitles.Add(correctedSubtitle);

            #endregion
        }

        #endregion

        return correctedSubtitles;
    }

    /// <summary>
    /// 使用Whisper生成SRT字幕文件（服务器模式）
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="outputPath">输出文件路径（不含扩展名）</param>
    /// <param name="temperature">温度参数（默认0.0）</param>
    /// <returns>生成的SRT文件路径</returns>
    public static async Task<string> GenerateSrtWithWhisperServerAsync(
        string audioPath,
        string outputPath,
        double temperature = 0.0)
    {
        return await WhisperServerClient.GenerateSrtWithWhisperServerAsync(
            audioPath,
            outputPath,
            temperature
        );
    }

    /// <summary>
    /// 批量转录音频段落（服务器模式）
    /// </summary>
    /// <param name="audioFilePaths">音频文件路径列表</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="temperature">温度参数（默认0.0）</param>
    /// <returns>转录结果列表（包含音频文件路径和SRT文件路径）</returns>
    public static async Task<List<(string audioPath, string srtPath)>> TranscribeSegmentsWithServerAsync(
        List<string> audioFilePaths,
        string outputDirectory,
        double temperature = 0.0)
    {
        #region 验证参数

        if (audioFilePaths == null || audioFilePaths.Count == 0)
        {
            throw new ArgumentException("没有音频文件需要转录");
        }

        #endregion

        #region 创建输出目录

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        #endregion

        #region 批量转录

        _progressService?.Title("批量转录音频段落（服务器模式）");
        _progressService?.Report($"音频文件数: {audioFilePaths.Count}");
        _progressService?.Report($"输出目录: {outputDirectory}");

        var results = new List<(string audioPath, string srtPath)>();
        var tasks = new List<Task<(string audioPath, string srtPath)>>();

        foreach (var audioPath in audioFilePaths)
        {
            if (!File.Exists(audioPath))
            {
                _progressService?.Warning($"音频文件不存在，跳过: {audioPath}");
                continue;
            }

            #region 生成输出文件名

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(audioPath);
            string outputPath = Path.Combine(outputDirectory, fileNameWithoutExt);

            #endregion

            #region 添加转录任务

            var task = TranscribeSingleSegmentWithServerAsync(audioPath, outputPath, temperature);
            tasks.Add(task);

            #endregion
        }

        #region 等待所有任务完成

        var taskResults = await Task.WhenAll(tasks);

        #endregion

        #region 收集结果

        foreach (var result in taskResults)
        {
            results.Add(result);
        }

        #endregion

        _progressService?.Report($"成功转录 {results.Count}/{audioFilePaths.Count} 个音频段落");

        #endregion

        return results;
    }

    /// <summary>
    /// 转录单个音频段落（服务器模式）
    /// </summary>
    private static async Task<(string audioPath, string srtPath)> TranscribeSingleSegmentWithServerAsync(
        string audioPath,
        string outputPath,
        double temperature)
    {
        try
        {
            string srtPath = await WhisperServerClient.GenerateSrtWithWhisperServerAsync(
                audioPath,
                outputPath,
                temperature
            );
            return (audioPath, srtPath);
        }
        catch (Exception ex)
        {
            _progressService?.Error($"转录失败: {audioPath}");
            _progressService?.Error($"错误: {ex.Message}");
            return (audioPath, string.Empty);
        }
    }

    /// <summary>
    /// 批量转录音频段落并解析SRT（服务器模式）
    /// </summary>
    /// <param name="audioFilePaths">音频文件路径列表</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="temperature">温度参数（默认0.0）</param>
    /// <returns>转录结果列表（包含音频文件路径和字幕列表）</returns>
    public static async Task<List<(string audioPath, IEnumerable<ISrtSubtitle> subtitles)>> TranscribeSegmentsAndParseWithServerAsync(
        List<string> audioFilePaths,
        string outputDirectory,
        double temperature = 0.0)
    {
        #region 批量转录

        var transcribeResults = await TranscribeSegmentsWithServerAsync(
            audioFilePaths,
            outputDirectory,
            temperature
        );

        #endregion

        #region 解析SRT文件

        var results = new List<(string audioPath, IEnumerable<ISrtSubtitle> subtitles)>();

        foreach (var (audioPath, srtPath) in transcribeResults)
        {
            if (string.IsNullOrEmpty(srtPath) || !File.Exists(srtPath))
            {
                continue;
            }

            try
            {
                var subtitles = ParseSrtFile(srtPath);
                results.Add((audioPath, subtitles));
            }
            catch (Exception ex)
            {
                _progressService?.Error($"解析SRT失败: {srtPath}");
                _progressService?.Error($"错误: {ex.Message}");
            }
        }

        #endregion

        return results;
    }

    #endregion

    #region 批量转录方法

    /// <summary>
    /// 批量转录音频段落
    /// </summary>
    /// <param name="audioFilePaths">音频文件路径列表</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="modelPath">Whisper模型文件路径</param>
    /// <param name="language">语言代码，默认为auto自动检测</param>
    /// <returns>转录结果列表（包含音频文件路径和SRT文件路径）</returns>
    public static List<(string audioPath, string srtPath)> TranscribeSegments(
        List<string> audioFilePaths,
        string outputDirectory,
        string modelPath = @"d:\VideoTranslator\whisper.cpp\ggml-large-v3-turbo-q8_0.bin",
        string language = "auto")
    {
        #region 验证参数

        if (audioFilePaths == null || audioFilePaths.Count == 0)
        {
            throw new ArgumentException("没有音频文件需要转录");
        }

        #endregion

        #region 创建输出目录

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        #endregion

        #region 批量转录

        _progressService?.Title("批量转录音频段落");
        _progressService?.Report($"音频文件数: {audioFilePaths.Count}");
        _progressService?.Report($"输出目录: {outputDirectory}");

        var results = new List<(string audioPath, string srtPath)>();

        foreach (var audioPath in audioFilePaths)
        {
            if (!File.Exists(audioPath))
            {
                _progressService?.Warning($"音频文件不存在，跳过: {audioPath}");
                continue;
            }

            #region 生成输出文件名

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(audioPath);
            string outputPath = Path.Combine(outputDirectory, fileNameWithoutExt);

            #endregion

            #region 转录音频

            try
            {
                string srtPath = GenerateSrtWithWhisper(audioPath, outputPath, modelPath, language);
                results.Add((audioPath, srtPath));
            }
            catch (Exception ex)
            {
                _progressService?.Error($"转录失败: {audioPath}");
                _progressService?.Error($"错误: {ex.Message}");
            }

            #endregion
        }

        _progressService?.Report($"成功转录 {results.Count}/{audioFilePaths.Count} 个音频段落");

        #endregion

        return results;
    }

    /// <summary>
    /// 批量转录音频段落并解析SRT
    /// </summary>
    /// <param name="audioFilePaths">音频文件路径列表</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="modelPath">Whisper模型文件路径</param>
    /// <param name="language">语言代码，默认为auto自动检测</param>
    /// <returns>转录结果列表（包含音频文件路径和字幕列表）</returns>
    public static List<(string audioPath, IEnumerable<ISrtSubtitle> subtitles)> TranscribeSegmentsAndParse(
        List<string> audioFilePaths,
        string outputDirectory,
        string modelPath = @"d:\VideoTranslator\whisper.cpp\ggml-large-v3-turbo-q8_0.bin",
        string language = "auto")
    {
        #region 批量转录

        var transcribeResults = TranscribeSegments(audioFilePaths, outputDirectory, modelPath, language);

        #endregion

        #region 解析SRT文件

        var results = new List<(string audioPath, IEnumerable<ISrtSubtitle> subtitles)>();

        foreach (var (audioPath, srtPath) in transcribeResults)
        {
            try
            {
                var subtitles = ParseSrtFile(srtPath);
                results.Add((audioPath, subtitles));
            }
            catch (Exception ex)
            {
                _progressService?.Error($"解析SRT失败: {srtPath}");
                _progressService?.Error($"错误: {ex.Message}");
            }
        }

        #endregion

        return results;
    }

    #endregion

    #region 合并SRT方法

    /// <summary>
    /// 合并多个SRT字幕列表到一个文件
    /// </summary>
    /// <param name="subtitleLists">字幕列表列表（每个列表对应一个音频段落）</param>
    /// <param name="segments">对应的音频段落列表</param>
    /// <param name="outputPath">输出SRT文件路径</param>
    public static void MergeSrtFiles(
        List<IEnumerable<ISrtSubtitle>> subtitleLists,
        LinkedList<ISpeechSegment> segments,
        string outputPath)
    {
        #region 验证参数

        if (subtitleLists == null || subtitleLists.Count == 0)
        {
            throw new ArgumentException("没有字幕需要合并");
        }

        if (segments == null || segments.Count == 0)
        {
            throw new ArgumentException("未提供段落信息");
        }

        if (subtitleLists.Count != segments.Count)
        {
            throw new ArgumentException($"字幕列表数量({subtitleLists.Count})与段落数量({segments.Count})不匹配");
        }

        #endregion

        #region 合并字幕并调整时间戳

        _progressService?.Title("合并SRT字幕");
        _progressService?.Report($"字幕列表数: {subtitleLists.Count}");
        _progressService?.Report($"输出文件: {outputPath}");

        var mergedSubtitles = new List<SrtSubtitle>();
        int globalIndex = 1;
        int segmentIndex = 0;

        var segmentNode = segments.First;
        foreach (var subtitleList in subtitleLists)
        {
            if (segmentNode == null)
            {
                break;
            }

            var segment = segmentNode.Value;
            double segmentStartMs = segment.StartMS;
            double segmentEndMs = segment.EndMS;

            _progressService?.Report($"处理段落 {segmentIndex}: {segment.StartSeconds:F2}s-{segment.EndSeconds:F2}s, 字幕数: {subtitleList.Count()}");

            #region 合并同一VAD段落内的所有字幕文本

            var mergedText = new StringBuilder();
            foreach (var subtitle in subtitleList)
            {
                if (mergedText.Length > 0)
                {
                    mergedText.Append(" ");
                }
                mergedText.Append(subtitle.Text);
            }

            #endregion

            #region 使用VAD的开始和结束时间作为字幕时间戳

            var mergedSubtitle = new SrtSubtitle(
                globalIndex,
                TimeSpan.FromMilliseconds(segmentStartMs),
                TimeSpan.FromMilliseconds(segmentEndMs),
                mergedText.ToString()
            );

            mergedSubtitles.Add(mergedSubtitle);
            globalIndex++;

            #endregion

            segmentNode = segmentNode.Next;
            segmentIndex++;
        }

        _progressService?.Report($"合并后总字幕数: {mergedSubtitles.Count} (与VAD段落数一致)");

        #endregion

        #region 保存合并后的SRT文件

        SaveSrtFile(mergedSubtitles, outputPath);

        _progressService?.Success($"合并SRT已保存到: {outputPath}");
        #endregion

    }

    #endregion
}
