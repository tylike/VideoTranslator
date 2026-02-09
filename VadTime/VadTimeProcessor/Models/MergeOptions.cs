namespace VadTimeProcessor.Models;

/// <summary>
/// 段落合并选项 - 封装语音段落合并相关配置
/// </summary>
public class MergeOptions
{
    #region 构造函数

    public MergeOptions()
    {
        MaxGapSeconds = 2.0;
        MinDurationSeconds = 1.0;
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 最大静音间隔（秒，默认2.0）
    /// 大于此值的段落不会合并
    /// </summary>
    public double MaxGapSeconds { get; set; }

    /// <summary>
    /// 最小段落时长（秒，默认1.0）
    /// 小于此值的段落会尝试与后面的合并
    /// </summary>
    public double MinDurationSeconds { get; set; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 验证选项是否有效
    /// </summary>
    public void Validate()
    {
        if (MaxGapSeconds < 0)
        {
            throw new ArgumentException("最大静音间隔不能为负数", nameof(MaxGapSeconds));
        }

        if (MinDurationSeconds < 0)
        {
            throw new ArgumentException("最小段落时长不能为负数", nameof(MinDurationSeconds));
        }
    }

    /// <summary>
    /// 创建默认选项
    /// </summary>
    public static MergeOptions CreateDefault()
    {
        return new MergeOptions();
    }

    #endregion
}
