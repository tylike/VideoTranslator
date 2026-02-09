using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using VT.Module.BusinessObjects;

namespace VT.Module.BusinessObjects.Whisper;

[NavigationItem("Whisper")]
public class WhisperSegment(Session s) : VTBaseObject(s)
{
    #region 基础信息

    [XafDisplayName("关联结果")]
    [Association()]
    public WhisperResult? Result
    {
        get { return GetPropertyValue<WhisperResult?>(nameof(Result)); }
        set { SetPropertyValue(nameof(Result), value); }
    }

    [XafDisplayName("索引")]
    public int Index
    {
        get { return GetPropertyValue<int>(nameof(Index)); }
        set { SetPropertyValue(nameof(Index), value); }
    }

    #endregion

    #region 时间信息

    [XafDisplayName("开始时间(秒)")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double StartTime
    {
        get { return GetPropertyValue<double>(nameof(StartTime)); }
        set { SetPropertyValue(nameof(StartTime), value); }
    }

    [XafDisplayName("结束时间(秒)")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double EndTime
    {
        get { return GetPropertyValue<double>(nameof(EndTime)); }
        set { SetPropertyValue(nameof(EndTime), value); }
    }

    [XafDisplayName("时长(秒)")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double Duration
    {
        get { return GetPropertyValue<double>(nameof(Duration)); }
        set { SetPropertyValue(nameof(Duration), value); }
    }

    #endregion

    #region 文本内容

    [XafDisplayName("识别文本")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "3")]
    public string? Text
    {
        get { return GetPropertyValue<string>(nameof(Text)); }
        set { SetPropertyValue(nameof(Text), value); }
    }

    [XafDisplayName("翻译文本")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "3")]
    public string? TranslatedText
    {
        get { return GetPropertyValue<string>(nameof(TranslatedText)); }
        set { SetPropertyValue(nameof(TranslatedText), value); }
    }

    #endregion

    #region 置信度信息

    [XafDisplayName("置信度")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double? Confidence
    {
        get { return GetPropertyValue<double?>(nameof(Confidence)); }
        set { SetPropertyValue(nameof(Confidence), value); }
    }

    [XafDisplayName("无语音概率")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double? NoSpeechProb
    {
        get { return GetPropertyValue<double?>(nameof(NoSpeechProb)); }
        set { SetPropertyValue(nameof(NoSpeechProb), value); }
    }

    #endregion

    #region 说话人信息

    [XafDisplayName("说话人ID")]
    [Size(50)]
    public string? SpeakerId
    {
        get { return GetPropertyValue<string>(nameof(SpeakerId)); }
        set { SetPropertyValue(nameof(SpeakerId), value); }
    }

    [XafDisplayName("说话人置信度")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double? SpeakerConfidence
    {
        get { return GetPropertyValue<double?>(nameof(SpeakerConfidence)); }
        set { SetPropertyValue(nameof(SpeakerConfidence), value); }
    }

    #endregion

    #region 词级信息

    [XafDisplayName("词数量")]
    public int WordCount
    {
        get { return GetPropertyValue<int>(nameof(WordCount)); }
        set { SetPropertyValue(nameof(WordCount), value); }
    }

    [XafDisplayName("词详情")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "5")]
    public string? WordsDetail
    {
        get { return GetPropertyValue<string>(nameof(WordsDetail)); }
        set { SetPropertyValue(nameof(WordsDetail), value); }
    }

    #endregion
}
