using System.IO;

namespace VadTimeProcessor.Models;

/// <summary>
/// VAD检测选项 - 封装语音活动检测相关配置
/// </summary>
public class VadOptions
{
    #region 构造函数

    public VadOptions()
    {
        ModelPath = @"d:\VideoTranslator\whisper.cpp\ggml-silero-v6.2.0-vad.bin";
        Threshold = 0.50;
        MinSpeechDurationMs = 250;
        MinSilenceDurationMs = 100;
        SpeechPadMs = 30;
        SamplesOverlap = 0.10;
        Threads = 4;
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// VAD模型路径（默认：d:\VideoTranslator\whisper.cpp\ggml-silero-v6.2.0-vad.bin）
    /// </summary>
    public string ModelPath { get; set; }

    /// <summary>
    /// VAD阈值（0.0-1.0，默认0.50）
    /// 值越大，对语音的检测越严格，只检测到更明显的语音
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// 最小语音时长（毫秒，默认250）
    /// 小于此值的语音段落会被过滤掉
    /// </summary>
    public int MinSpeechDurationMs { get; set; }

    /// <summary>
    /// 最小静音时长（毫秒，默认100）
    /// 小于此值的静音不会被识别为段落分隔
    /// </summary>
    public int MinSilenceDurationMs { get; set; }

    /// <summary>
    /// 最大语音时长（秒，默认无限制）
    /// 单个段落的最大时长限制
    /// </summary>
    public double? MaxSpeechDurationS { get; set; }

    /// <summary>
    /// 语音填充（毫秒，默认30）
    /// 在检测到的语音前后添加的填充时间
    /// </summary>
    public int SpeechPadMs { get; set; }

    /// <summary>
    /// 段落重叠（秒，默认0.10）
    /// 相邻段落之间的重叠时间
    /// </summary>
    public double SamplesOverlap { get; set; }

    /// <summary>
    /// 线程数（默认4）
    /// VAD检测使用的线程数
    /// </summary>
    public int Threads { get; set; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 验证选项是否有效
    /// </summary>
    public void Validate()
    {
        if (!string.IsNullOrEmpty(ModelPath) && !File.Exists(ModelPath))
        {
            throw new FileNotFoundException($"VAD模型文件不存在: {ModelPath}");
        }

        if (Threshold < 0.0 || Threshold > 1.0)
        {
            throw new ArgumentException("VAD阈值必须在0.0到1.0之间", nameof(Threshold));
        }

        if (MinSpeechDurationMs < 0)
        {
            throw new ArgumentException("最小语音时长不能为负数", nameof(MinSpeechDurationMs));
        }

        if (MinSilenceDurationMs < 0)
        {
            throw new ArgumentException("最小静音时长不能为负数", nameof(MinSilenceDurationMs));
        }

        if (Threads < 1)
        {
            throw new ArgumentException("线程数必须大于0", nameof(Threads));
        }
    }

    /// <summary>
    /// 创建默认选项
    /// </summary>
    public static VadOptions CreateDefault()
    {
        return new VadOptions();
    }

    #endregion
}
