namespace VideoTranslator.Models;

/// <summary>
/// 用于音频合并时。
/// </summary>
public class AudioClipWithTime
{
    public int Index { get; set; }
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 目标开始时间，片断必须按此属性进行升序排序
    /// </summary>
    public TimeSpan Start { get; set; }
    /// <summary>
    /// 目标结束时间
    /// </summary>
    public TimeSpan End { get; set; }
    /// <summary>
    /// 此片段的目标时长
    /// </summary>
    public double DurationMs
    {
        get
        {
            return (int)(End - Start).TotalMilliseconds;
        }
    }

    /// <summary>
    /// 音频文件实际时长
    /// </summary>
    public double AudioDurationMs { get; set; }

    public double StartBgPlayTime
    {
        get
        {
            if(Index == 0)
            {
                return Start.TotalMilliseconds;
            }
            return 0;
        }
    }

    public double EndBgPlayTimeMs
    {
        get => (End.TotalMilliseconds - DurationMs);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            throw new InvalidOperationException($"片段{Index}的音频路径为空");
        }

        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException($"片段{Index}的音频文件不存在: {FilePath}", FilePath);
        }

        if (Start < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"片段{Index}的开始时间不能为负数: {Start}");
        }
        //开始和结束时间不能相等
        if (End <= Start)
        {
            throw new InvalidOperationException($"片段{Index}的结束时间必须大于开始时间: 开始时间={Start}, 结束时间={End}");
        }

    }
}
