using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Xpo;
using System.Collections.Specialized;
using System.ComponentModel;
using VadTimeProcessor.Models;
using VT.Core;

namespace VT.Module.BusinessObjects;
[Appearance("间隔大于", AppearanceItemType = "ViewItem", TargetItems = nameof(NextGapMS), Criteria = $"{nameof(Audio)}.{nameof(Audio.GapTip)} < {nameof(NextGapMS)}", BackColor ="Red")]
public class VadSegment(Session s) : VTBaseObject(s), ISpeechSegment    
{
    [Association]
    public AudioSource Audio
    {
        get { return field; }
        set { SetPropertyValue("Audio", ref field, value); }
    }

    #region Properties

    [XafDisplayName("索引")]
    public int Index
    {
        get => GetPropertyValue<int>(nameof(Index));
        set => SetPropertyValue(nameof(Index), value);
    }

    /// <summary>
    /// 单位: 毫秒
    /// </summary>
    [XafDisplayName("开始时间")]
    public double StartMS
    {
        get => GetPropertyValue<double>(nameof(StartMS));
        set => SetPropertyValue(nameof(StartMS), value);
    }


    [XafDisplayName("结束时间")]
    public double EndMS
    {
        get => GetPropertyValue<double>(nameof(EndMS));
        set => SetPropertyValue(nameof(EndMS), value);
    }


    public double NextGapMS => this.GetGapToNext();

    #endregion

    #region Navigation Properties

    public VadSegment Previous
    {
        get => GetPropertyValue<VadSegment>(nameof(Previous));
        set => SetPropertyValue(nameof(Previous), value);
    }

    public VadSegment Next
    {
        get => GetPropertyValue<VadSegment>(nameof(Next));
        set => SetPropertyValue(nameof(Next), value);
    }

    #endregion

    #region Public Methods
    
    ISpeechSegment ISpeechSegment.Next { get => Next; set => throw new NotImplementedException(); }
    ISpeechSegment ISpeechSegment.Previous { get => Previous; set => throw new NotImplementedException(); }
    TimeSpan ISpeechSegment.StartTime { get => TimeSpan.FromMilliseconds(this.StartMS); set => this.StartMS = value.TotalMilliseconds; }
    TimeSpan ISpeechSegment.EndTime { get => TimeSpan.FromMilliseconds(this.EndMS); set => this.EndMS = value.TotalMilliseconds; }

    public override string ToString()
    {
        return $"{Index}: {StartMS / 1000:F2}s - {EndMS / 1000:F2}s (时长: {this.DurationMS / 1000:F2}s)";
    }

    #endregion
}