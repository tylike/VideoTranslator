using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;
using System;
using System.Linq;
using VadTimeProcessor.Models;
using VideoTranslator.SRT.Core.Extensions;
using VT.Core;

namespace VT.Module.BusinessObjects;

public abstract class Clip : VTBaseObject, ISpeechSegment
{
    [XafDisplayName("索引")]
    public int Index
    {
        get { return GetPropertyValue<int>(nameof(Index)); }
        set { SetPropertyValue(nameof(Index), value); }
    }

    [XafDisplayName("开始时间")]
    public TimeSpan Start
    {
        get { return GetPropertyValue<TimeSpan>(nameof(Start)); }
        set { SetPropertyValue(nameof(Start), value); }
    }

    [XafDisplayName("结束时间")]
    public TimeSpan End
    {
        get { return GetPropertyValue<TimeSpan>(nameof(End)); }
        set { SetPropertyValue(nameof(End), value); }
    }

    [XafDisplayName("文本")]
    [Size(-1)]
    public string Text
    {
        get { return GetPropertyValue<string>(nameof(Text)); }
        set { SetPropertyValue(nameof(Text), value); }
    }

    public virtual string DisplayText => $"{Index}\n{Text}";

    [XafDisplayName("类型")]
    public abstract MediaType Type
    {
        get;
    }

    #region 计算属性

    [XafDisplayName("下一个片段")]
    public Clip? NextClip
    {
        get
        {
            if (Track?.Segments == null)
            {
                return null;
            }
            return Track.Segments.FirstOrDefault(c => c.Index == Index + 1);
        }
    }

    [XafDisplayName("背景颜色")]
    public virtual string BackgroundColor
    {
        get { return "Transparent"; }
    }

    [XafDisplayName("工具提示信息")]
    public virtual string ToolTipInfo
    {
        get
        {
            ISpeechSegment self = this;
            var info = $"索引: {Index}\n";
            info += $"开始时间: {FormatTime(Start)}\n";
            info += $"结束时间: {FormatTime(End)}\n";
            info += $"持续时间: {FormatTime(self.Duration)} ({self.DurationSeconds:F3}秒)";

            if (self.DurationSeconds < 0)
            {
                info += "\n⚠️ 时间异常: 结束时间小于开始时间";
            }

            if (NextClip != null)
            {
                var gap = self.Next.StartSeconds - self.EndSeconds;
                info += $"\n与下一个间隔: {gap:F3}秒";
            }

            if (!string.IsNullOrEmpty(Text))
            {
                info += $"\n内容: {Text}";
            }

            return info;
        }
    }

    private static string FormatTime(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"hh\:mm\:ss\.fff");
    }

    public virtual string Validate()
    {
        if(this.Start >= this.End)
        {
            return $"{Index}:开始时间[{this.Start.ToSrtTimeString()}]大于或等于结束时间[{this.End.ToSrtTimeString()}]!";
        }
        return null;
    }

    #endregion

    [Association]
    public TrackInfo Track
    {
        get { return field; }
        set { SetPropertyValue("Track", ref field, value); }
    }

    TimeSpan ISpeechSegment.StartTime { get => this.Start; set => this.Start = value; }
    TimeSpan ISpeechSegment.EndTime { get => this.End; set => this.End = value; }
    ISpeechSegment ISpeechSegment.Next { get => this.NextClip; set => throw new NotImplementedException(); }
    ISpeechSegment ISpeechSegment.Previous { get => this.Track?.Segments.Where(x => x.Index > this.Index).OrderBy(x => x.Index).FirstOrDefault(); set => throw new NotImplementedException(); }

    public double Duration => (this as ISpeechSegment).DurationSeconds;

    public Clip(Session s) : base(s)
    {
    }
}
