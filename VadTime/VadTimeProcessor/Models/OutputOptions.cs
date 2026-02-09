namespace VadTimeProcessor.Models;

/// <summary>
/// 输出选项 - 封装文件输出相关配置
/// </summary>
public class OutputOptions
{
    #region 构造函数

    public OutputOptions()
    {
        OutputDirectory = string.Empty;
        KeepIntermediateFiles = true;
        EnableConsoleOutput = true;
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 输出目录（可选，默认为输入音频所在目录）
    /// </summary>
    public string OutputDirectory { get; set; }

    /// <summary>
    /// 是否保留中间文件（默认true）
    /// 如果为false，则转录完成后会删除中间文件（segments和segments_srt目录）
    /// </summary>
    public bool KeepIntermediateFiles { get; set; }

    /// <summary>
    /// 是否启用控制台输出（默认true）
    /// </summary>
    public bool EnableConsoleOutput { get; set; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 验证选项是否有效
    /// </summary>
    public void Validate()
    {
    }

    /// <summary>
    /// 创建默认选项
    /// </summary>
    public static OutputOptions CreateDefault()
    {
        return new OutputOptions();
    }

    #endregion
}
