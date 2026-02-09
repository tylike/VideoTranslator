﻿﻿﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VadTimeProcessor.Models;
using VadTimeProcessor.Services;
using VideoTranslator.Interfaces;
using VT.Core;

namespace VadTimeProcessor.Services;

/// <summary>
/// 音频转录器 - 负责完整的音频转录流程
/// </summary>
public class AudioTranscriber
{
    #region 私有字段

    private readonly TranscribeOptions _options;
    private readonly Stopwatch _totalStopwatch;
    private readonly WhisperServerManager _serverManager;
    private readonly IEnumerable<ISpeechSegment>? _preDetectedSegments;
    private IProgressService? _progressService;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">转录选项</param>
    /// <param name="preDetectedSegments">预检测的语音段落，如果提供则跳过VAD检测</param>
    public AudioTranscriber(TranscribeOptions options, IEnumerable<ISpeechSegment>? preDetectedSegments = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _preDetectedSegments = preDetectedSegments;
        _totalStopwatch = new Stopwatch();
        _serverManager = WhisperServerManager.CreateFromOptions(_options);

        WhisperServerClient.SetServerUrl(_options.Whisper.ServerUrl);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置进度服务实例
    /// </summary>
    /// <param name="progressService">进度服务实例</param>
    public void SetProgressService(IProgressService progressService)
    {
        _progressService = progressService;
        _serverManager.SetProgressService(progressService);
    }

    /// <summary>
    /// 执行完整的转录流程
    /// </summary>
    /// <returns>转录结果</returns>
    public async Task<TranscribeResult> TranscribeAsync()
    {
        var result = new TranscribeResult
        {
            InputAudioPath = _options.InputAudioPath
        };

        _totalStopwatch.Start();

        try
        {
            #region 验证选项

            _options.Validate();

            #endregion

            #region 确保Whisper服务器运行

            if (_options.Whisper.AutoStartServer)
            {
                LogInfo("=== 检查Whisper服务器 ===");
                var serverAvailable = await _serverManager.EnsureServerRunningAsync();

                if (!serverAvailable)
                {
                    throw new InvalidOperationException("无法启动Whisper服务器，请检查配置");
                }

                LogInfo();
            }

            #endregion

            #region 执行VAD检测

            var vadStopwatch = Stopwatch.StartNew();
            LinkedList<ISpeechSegment> segments;

            if (_preDetectedSegments != null)
            {
                LogInfo("=== 使用预检测的VAD段落 ===");
                segments = ConvertToLinkedList(_preDetectedSegments);
                result.DetectedSegmentCount = segments.Count;
                LogInfo($"使用 {segments.Count} 个预检测的语音段落");
                LogInfo();
            }
            else
            {
                segments = await ExecuteVadDetectionAsync(result);
            }

            vadStopwatch.Stop();
            result.VadDetectionDurationMs = vadStopwatch.ElapsedMilliseconds;

            #endregion

            #region 合并段落

            var mergeStopwatch = Stopwatch.StartNew();
            LinkedList<ISpeechSegment> mergedSegments = segments;

            //= ExecuteSegmentMerge(segments);
            mergeStopwatch.Stop();
            result.MergeDurationMs = mergeStopwatch.ElapsedMilliseconds;

            #endregion

            #region 提取音频段落并转录

            var transcribeStopwatch = Stopwatch.StartNew();
            await ExtractAndTranscribeSegmentsAsync(mergedSegments, result);
            transcribeStopwatch.Stop();
            result.TranscribeDurationMs = transcribeStopwatch.ElapsedMilliseconds;

            #endregion

            #region 标记成功

            result.IsSuccess = true;

            #endregion
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            LogError($"转录失败: {ex.Message}");
        }
        finally
        {
            _totalStopwatch.Stop();
            result.TotalDurationMs = _totalStopwatch.ElapsedMilliseconds;

            #region 停止Whisper服务器（如果需要）

            if (_options.Whisper.StopServerAfterTranscribe && _serverManager.IsManagedByUs)
            {
                LogInfo("=== 停止Whisper服务器 ===");
                _serverManager.StopServer();
                LogInfo();
            }

            #endregion
        }

        return result;
    }

    #endregion

    #region 私有方法

    #region VAD检测

    /// <summary>
    /// 执行VAD检测
    /// </summary>
    private async Task<LinkedList<ISpeechSegment>> ExecuteVadDetectionAsync(TranscribeResult result)
    {
        LogInfo("=== 执行VAD检测 ===");
        LogInfo($"音频: {_options.InputAudioPath}");
        LogInfo($"模型: {_options.Vad.ModelPath}");
        LogInfo($"阈值: {_options.Vad.Threshold:F2}");
        LogInfo($"最小语音时长: {_options.Vad.MinSpeechDurationMs}ms");
        LogInfo($"最小静音时长: {_options.Vad.MinSilenceDurationMs}ms");
        LogInfo();

        var outputDir = _options.GetOutputDirectory();
        var vadOutputPath = Path.Combine(outputDir, "vad_output.txt");

        var segments = VadDetector.DetectSpeechSegmentsAndSave(
            _options.InputAudioPath,
            vadOutputPath,
            _options.Vad.ModelPath
        );

        result.DetectedSegmentCount = segments.Count;
        result.VadOutputPath = vadOutputPath;
        result.AddGeneratedFile(vadOutputPath, "VAD", "VAD检测结果");

        LogInfo($"检测到 {segments.Count} 个语音段落");
        LogInfo($"VAD输出已保存到: {vadOutputPath}");
        LogInfo();

        return segments;
    }

    #endregion

    #region 段落合并

    /// <summary>
    /// 执行段落合并
    /// </summary>
    private LinkedList<ISpeechSegment> ExecuteSegmentMerge(LinkedList<ISpeechSegment> segments)
    {
        LogInfo("=== 智能合并段落 ===");
        LogInfo($"最大静音间隔: {_options.Merge.MaxGapSeconds}s");
        LogInfo($"最小段落时长: {_options.Merge.MinDurationSeconds}s");
        LogInfo();

        var mergedSegments = VadSegmentMerger.MergeSegmentsByGap(
            segments,
            _options.Merge.MaxGapSeconds,
            _options.Merge.MinDurationSeconds
        );

        LogInfo($"合并后段落数: {mergedSegments.Count}");
        LogInfo();

        return mergedSegments;
    }

    #endregion

    #region 音频提取和转录

    /// <summary>
    /// 提取音频段落并转录
    /// </summary>
    private async Task ExtractAndTranscribeSegmentsAsync(LinkedList<ISpeechSegment> mergedSegments, TranscribeResult result)
    {
        var outputDir = _options.GetOutputDirectory();
        var segmentsOutputDir = Path.Combine(outputDir, "segments");
        var srtOutputDir = Path.Combine(outputDir, "segments_srt");
        var mergedSrtPath = Path.Combine(outputDir, "transcribed.srt");

        result.AudioSegmentsOutputDir = segmentsOutputDir;
        result.SrtOutputDir = srtOutputDir;
        result.MergedSrtPath = mergedSrtPath;

        #region 判断是否需要分段提取

        List<string> audioFilesToTranscribe;

        if (mergedSegments.Count == 1)
        {
            LogInfo("合并后只有1个段落，整个音频是连续的，无需分段截取");
            LogInfo($"直接使用原始音频文件: {_options.InputAudioPath}");
            LogInfo();

            audioFilesToTranscribe = new List<string> { _options.InputAudioPath };
            result.ExtractedSegmentCount = 0;
        }
        else
        {
            #region 提取音频段落

            LogInfo("=== 提取音频段落 ===");
            LogInfo($"输出目录: {segmentsOutputDir}");
            LogInfo();

            var extractStopwatch = Stopwatch.StartNew();
            var extractedFiles = AudioSegmentExtractor.ExtractAllSegments(
                _options.InputAudioPath,
                mergedSegments,
                segmentsOutputDir,
                "merged_segment"
            );
            extractStopwatch.Stop();
            result.ExtractDurationMs = extractStopwatch.ElapsedMilliseconds;

            audioFilesToTranscribe = extractedFiles;
            result.ExtractedSegmentCount = extractedFiles.Count;

            #region 记录提取的文件

            foreach (var file in extractedFiles)
            {
                result.AddGeneratedFile(file, "Audio", $"提取的音频段落");
            }

            #endregion

            LogInfo($"共提取 {extractedFiles.Count} 个音频段落");
            LogInfo();

            #endregion
        }

        #endregion

        #region 批量转录

        LogInfo("=== 批量转录音频段落 ===");
        LogInfo($"音频文件数: {audioFilesToTranscribe.Count}");
        LogInfo($"输出目录: {srtOutputDir}");
        LogInfo($"温度参数: {_options.Whisper.Temperature}");
        LogInfo();

        var transcribeResults = await WhisperTranscriber.TranscribeSegmentsAndParseWithServerAsync(
            audioFilesToTranscribe,
            srtOutputDir,
            _options.Whisper.Temperature
        );

        #region 记录转录的文件

        foreach (var (audioPath, subtitles) in transcribeResults)
        {
            var srtPath = Path.Combine(srtOutputDir, Path.GetFileNameWithoutExtension(audioPath) + ".srt");
            if (File.Exists(srtPath))
            {
                result.AddGeneratedFile(srtPath, "SRT", $"转录的字幕文件");
            }
        }

        #endregion

        LogInfo($"成功转录 {transcribeResults.Count}/{audioFilesToTranscribe.Count} 个音频段落");
        LogInfo();

        #endregion

        #region 合并所有转录结果

        LogInfo("=== 合并转录结果 ===");
        LogInfo($"输出文件: {mergedSrtPath}");
        LogInfo();

        var subtitleLists = transcribeResults.Select(r => r.subtitles).ToList();
        WhisperTranscriber.MergeSrtFiles(subtitleLists, mergedSegments, mergedSrtPath);

        result.AddGeneratedFile(mergedSrtPath, "SRT", "合并后的最终字幕文件");

        LogInfo($"合并完成，输出文件: {mergedSrtPath}");
        LogInfo();

        #endregion

        #region 清理中间文件

        if (!_options.Output.KeepIntermediateFiles && mergedSegments.Count > 1)
        {
            LogInfo("=== 清理中间文件 ===");
            LogInfo();

            try
            {
                if (Directory.Exists(segmentsOutputDir))
                {
                    Directory.Delete(segmentsOutputDir, recursive: true);
                    LogInfo($"已删除目录: {segmentsOutputDir}");
                }

                if (Directory.Exists(srtOutputDir))
                {
                    Directory.Delete(srtOutputDir, recursive: true);
                    LogInfo($"已删除目录: {srtOutputDir}");
                }

                LogInfo();
            }
            catch (Exception ex)
            {
                LogError($"清理中间文件失败: {ex.Message}");
            }
        }

        #endregion
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 将 IEnumerable<ISpeechSegment> 转换为 LinkedList<SpeechSegment>
    /// </summary>
    private LinkedList<ISpeechSegment> ConvertToLinkedList(IEnumerable<ISpeechSegment> segments)
    {
        var linkedList = new LinkedList<ISpeechSegment>();

        foreach (var segment in segments)
        {
            var speechSegment = new SpeechSegment(segment.Index, segment.StartMS, segment.EndMS);
            linkedList.AddLast(speechSegment);
        }

        // 设置 Next 和 Previous 引用
        var node = linkedList.First;
        ISpeechSegment? previousSegment = null;

        while (node != null)
        {
            node.Value.Previous = previousSegment;

            if (node.Next != null)
            {
                node.Value.Next = node.Next.Value;
            }

            previousSegment = node.Value;
            node = node.Next;
        }

        return linkedList;
    }

    #endregion

    #region 日志方法

    /// <summary>
    /// 记录信息
    /// </summary>
    private void LogInfo(string message = "")
    {
        if (_options.Output.EnableConsoleOutput)
        {
            _progressService?.Report(message);
        }
    }

    /// <summary>
    /// 记录错误
    /// </summary>
    private void LogError(string message)
    {
        if (_options.Output.EnableConsoleOutput)
        {
            _progressService?.Error(message);
        }
    }

    #endregion

    #endregion

    #region 静态工厂方法

    /// <summary>
    /// 使用默认选项创建转录器
    /// </summary>
    /// <param name="inputAudioPath">输入音频路径</param>
    /// <returns>音频转录器实例</returns>
    public static AudioTranscriber Create(string inputAudioPath)
    {
        var options = TranscribeOptions.CreateDefault(inputAudioPath);
        return new AudioTranscriber(options);
    }

    /// <summary>
    /// 使用自定义选项创建转录器
    /// </summary>
    /// <param name="options">转录选项</param>
    /// <param name="preDetectedSegments">预检测的语音段落，如果提供则跳过VAD检测</param>
    /// <returns>音频转录器实例</returns>
    public static AudioTranscriber Create(TranscribeOptions options, IEnumerable<ISpeechSegment>? preDetectedSegments = null)
    {
        return new AudioTranscriber(options, preDetectedSegments);
    }

    #endregion

    #region 便捷静态方法

    /// <summary>
    /// 快速转录音频（使用默认配置）
    /// </summary>
    /// <param name="inputAudioPath">输入音频路径</param>
    /// <returns>转录结果</returns>
    public static async Task<TranscribeResult> TranscribeQuickAsync(string inputAudioPath)
    {
        var transcriber = Create(inputAudioPath);
        return await transcriber.TranscribeAsync();
    }

    /// <summary>
    /// 快速转录音频（使用自定义配置）
    /// </summary>
    /// <param name="options">转录选项</param>
    /// <returns>转录结果</returns>
    public static async Task<TranscribeResult> TranscribeQuickAsync(TranscribeOptions options)
    {
        var transcriber = Create(options);
        return await transcriber.TranscribeAsync();
    }

    #endregion
}
