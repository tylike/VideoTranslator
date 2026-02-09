using System;

namespace VideoTranslator.Models;

public class VadSegment
{
    public int Index { get; set; }
    public decimal Start { get; set; }
    public decimal End { get; set; }
    public decimal Duration { get; set; }
    public bool IsSpeech { get; set; }
}

public class VadDetectionResult
{
    public string AudioPath { get; set; } = string.Empty;
    public decimal AudioDuration { get; set; }
    public List<VadSegment> Segments { get; set; } = new List<VadSegment>();
    public int SpeechSegmentCount { get; set; }
    public int SilenceSegmentCount { get; set; }
    public decimal TotalSpeechDuration { get; set; }
    public decimal TotalSilenceDuration { get; set; }
    /// <summary>
    /// 如果(有时有这样的错误)来源数据是 厘秒 需要乘以10，以转换到正确的毫秒
    /// </summary>
    public void FixTime()
    {        
        foreach (var item in Segments)
        {
            item.Start = item.Start * 10;
            item.End = item.End * 10;
            item.Duration = item.Duration * 10;
        }
        this.AudioDuration = this.AudioDuration * 10;
        this.TotalSpeechDuration = this.TotalSpeechDuration * 10;
        this.TotalSilenceDuration = this.TotalSilenceDuration * 10;
    }
}
