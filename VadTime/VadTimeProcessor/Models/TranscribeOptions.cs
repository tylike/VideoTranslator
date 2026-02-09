﻿﻿﻿using System;
using System.IO;

namespace VadTimeProcessor.Models;

/// <summary>
/// 转录选项类 - 封装所有转录配置参数
/// </summary>
public class TranscribeOptions
{
    #region 构造函数

    public TranscribeOptions()
    {
        InputAudioPath = string.Empty;
        Vad = new VadOptions();
        Merge = new MergeOptions();
        Whisper = new WhisperOptions();
        Output = new OutputOptions();
    }

    #endregion

    #region 基础配置

    /// <summary>
    /// 输入音频文件路径（必填）
    /// </summary>
    public string InputAudioPath { get; set; }

    #endregion

    #region VAD检测配置

    /// <summary>
    /// VAD检测选项
    /// </summary>
    public VadOptions Vad { get; set; }

    #endregion

    #region 段落合并配置

    /// <summary>
    /// 段落合并选项
    /// </summary>
    public MergeOptions Merge { get; set; }

    #endregion

    #region Whisper转录配置

    /// <summary>
    /// Whisper转录选项
    /// </summary>
    public WhisperOptions Whisper { get; set; }

    #endregion

    #region 输出配置

    /// <summary>
    /// 输出选项
    /// </summary>
    public OutputOptions Output { get; set; }

    #endregion
    
    #region 公共方法

    /// <summary>
    /// 验证选项是否有效
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(InputAudioPath))
        {
            throw new ArgumentException("输入音频路径不能为空", nameof(InputAudioPath));
        }

        if (!File.Exists(InputAudioPath))
        {
            throw new FileNotFoundException($"输入音频文件不存在: {InputAudioPath}");
        }

        Vad.Validate();
        Merge.Validate();
        Whisper.Validate();
        Output.Validate();
    }

    /// <summary>
    /// 获取输出目录（如果未指定则使用输入音频所在目录）
    /// </summary>
    public string GetOutputDirectory()
    {
        if (string.IsNullOrEmpty(Output.OutputDirectory))
        {
            return Path.GetDirectoryName(InputAudioPath) ?? string.Empty;
        }
        return Output.OutputDirectory;
    }

    /// <summary>
    /// 创建默认选项
    /// </summary>
    public static TranscribeOptions CreateDefault(string inputAudioPath)
    {
        return new TranscribeOptions
        {
            InputAudioPath = inputAudioPath
        };
    }

    #endregion
}
