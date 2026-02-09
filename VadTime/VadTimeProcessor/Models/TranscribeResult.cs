using System.Collections.Generic;

namespace VadTimeProcessor.Models;

/// <summary>
/// 转录结果类 - 包含转录的所有结果和生成的文件信息
/// </summary>
public class TranscribeResult
{
    #region 构造函数

    public TranscribeResult()
    {
        GeneratedFiles = new List<GeneratedFileInfo>();
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 原始音频文件路径
    /// </summary>
    public string InputAudioPath { get; set; } = string.Empty;

    /// <summary>
    /// 转录是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 检测到的语音段落数量
    /// </summary>
    public int DetectedSegmentCount { get; set; }

    /// <summary>
    /// 合并后的段落数量
    /// </summary>
    public int MergedSegmentCount { get; set; }

    /// <summary>
    /// 提取的音频段落数量
    /// </summary>
    public int ExtractedSegmentCount { get; set; }

    /// <summary>
    /// 生成的所有文件列表
    /// </summary>
    public List<GeneratedFileInfo> GeneratedFiles { get; set; }

    /// <summary>
    /// VAD输出文件路径
    /// </summary>
    public string? VadOutputPath { get; set; }

    /// <summary>
    /// 合并后的SRT字幕文件路径
    /// </summary>
    public string? MergedSrtPath { get; set; }

    /// <summary>
    /// 音频段落输出目录
    /// </summary>
    public string? AudioSegmentsOutputDir { get; set; }

    /// <summary>
    /// SRT字幕输出目录
    /// </summary>
    public string? SrtOutputDir { get; set; }

    /// <summary>
    /// 总耗时（毫秒）
    /// </summary>
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// VAD检测耗时（毫秒）
    /// </summary>
    public long VadDetectionDurationMs { get; set; }

    /// <summary>
    /// 段落合并耗时（毫秒）
    /// </summary>
    public long MergeDurationMs { get; set; }

    /// <summary>
    /// 音频提取耗时（毫秒）
    /// </summary>
    public long ExtractDurationMs { get; set; }

    /// <summary>
    /// 转录耗时（毫秒）
    /// </summary>
    public long TranscribeDurationMs { get; set; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加生成的文件信息
    /// </summary>
    public void AddGeneratedFile(string filePath, string fileType, string description)
    {
        GeneratedFiles.Add(new GeneratedFileInfo
        {
            FilePath = filePath,
            FileType = fileType,
            Description = description,
            FileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : 0,
            CreatedTime = DateTime.Now
        });
    }

    /// <summary>
    /// 获取指定类型的文件列表
    /// </summary>
    public List<GeneratedFileInfo> GetFilesByType(string fileType)
    {
        return GeneratedFiles.FindAll(f => f.FileType == fileType);
    }

    /// <summary>
    /// 获取摘要信息
    /// </summary>
    public string GetSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"=== 转录结果摘要 ===");
        summary.AppendLine($"状态: {(IsSuccess ? "成功" : "失败")}");
        summary.AppendLine($"输入音频: {InputAudioPath}");
        summary.AppendLine($"检测段落: {DetectedSegmentCount}");
        summary.AppendLine($"合并段落: {MergedSegmentCount}");
        summary.AppendLine($"提取段落: {ExtractedSegmentCount}");
        summary.AppendLine($"生成文件数: {GeneratedFiles.Count}");
        summary.AppendLine($"总耗时: {TotalDurationMs / 1000.0:F2}秒");
        
        if (!IsSuccess && !string.IsNullOrEmpty(ErrorMessage))
        {
            summary.AppendLine($"错误信息: {ErrorMessage}");
        }

        return summary.ToString();
    }

    #endregion
}

/// <summary>
/// 生成的文件信息
/// </summary>
public class GeneratedFileInfo
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型（如：VAD、SRT、Audio等）
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 文件描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
