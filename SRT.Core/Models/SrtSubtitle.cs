using System;
using VT.Core;

namespace VideoTranslator.SRT.Core.Models;

public class SrtSubtitle : ISrtSubtitle
{
    #region 属性

    public int Index { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string Text { get; set; } = string.Empty;
    ISpeechSegment? ISpeechSegment.Next { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    ISpeechSegment? ISpeechSegment.Previous { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    #endregion



    #region 构造函数

    public SrtSubtitle()
    {
    }

    public SrtSubtitle(int index, TimeSpan startTime, TimeSpan endTime, string text)
    {
        Index = index;
        StartTime = startTime;
        EndTime = endTime;
        Text = text;
    }

    #endregion

}
//public class Subtitle
//{
//    public int Index { get; set; }
//    public string StartTime { get; set; } = string.Empty;
//    public string EndTime { get; set; } = string.Empty;
//    public string Text { get; set; } = string.Empty;
//    public string TimeRange { get; set; } = string.Empty;

//    public double Duration => EndSeconds - StartSeconds;
//}