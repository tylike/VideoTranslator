using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using VT.Core;

namespace VT.Module.BusinessObjects.Whisper;

[NavigationItem("Whisper")]
public class WhisperPreset(Session s) : VTBaseObject(s)
{
    #region 基础信息

    [XafDisplayName("预设名称")]
    [Size(200)]
    public string PresetName
    {
        get { return GetPropertyValue<string>(nameof(PresetName)); }
        set { SetPropertyValue(nameof(PresetName), value); }
    }

    [XafDisplayName("描述")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "3")]
    public string? Description
    {
        get { return GetPropertyValue<string>(nameof(Description)); }
        set { SetPropertyValue(nameof(Description), value); }
    }

    [XafDisplayName("分类")]
    [Size(100)]
    public string? Category
    {
        get { return GetPropertyValue<string>(nameof(Category)); }
        set { SetPropertyValue(nameof(Category), value); }
    }

    [XafDisplayName("是否为系统预设")]
    public bool IsSystemPreset
    {
        get { return GetPropertyValue<bool>(nameof(IsSystemPreset)); }
        set { SetPropertyValue(nameof(IsSystemPreset), value); }
    }

    #endregion

    #region 快速配置

    [XafDisplayName("语言")]
    public Language Language
    {
        get { return GetPropertyValue<Language>(nameof(Language)); }
        set { SetPropertyValue(nameof(Language), value); }
    }

    [XafDisplayName("输出格式")]
    public WhisperOutputFormat OutputFormat
    {
        get { return GetPropertyValue<WhisperOutputFormat>(nameof(OutputFormat)); }
        set { SetPropertyValue(nameof(OutputFormat), value); }
    }

    [XafDisplayName("启用VAD")]
    public bool EnableVad
    {
        get { return GetPropertyValue<bool>(nameof(EnableVad)); }
        set { SetPropertyValue(nameof(EnableVad), value); }
    }

    [XafDisplayName("启用翻译")]
    public bool EnableTranslation
    {
        get { return GetPropertyValue<bool>(nameof(EnableTranslation)); }
        set { SetPropertyValue(nameof(EnableTranslation), value); }
    }

    [XafDisplayName("启用说话人分离")]
    public bool EnableDiarization
    {
        get { return GetPropertyValue<bool>(nameof(EnableDiarization)); }
        set { SetPropertyValue(nameof(EnableDiarization), value); }
    }

    #endregion

    #region 精度配置

    [XafDisplayName("温度")]
    public double? Temperature
    {
        get { return GetPropertyValue<double?>(nameof(Temperature)); }
        set { SetPropertyValue(nameof(Temperature), value); }
    }

    [XafDisplayName("最佳候选数")]
    public int? BestOf
    {
        get { return GetPropertyValue<int?>(nameof(BestOf)); }
        set { SetPropertyValue(nameof(BestOf), value); }
    }

    [XafDisplayName("Beam Size")]
    public int? BeamSize
    {
        get { return GetPropertyValue<int?>(nameof(BeamSize)); }
        set { SetPropertyValue(nameof(BeamSize), value); }
    }

    #endregion

    #region 段落配置

    [XafDisplayName("最大段长度")]
    public int? MaxLength
    {
        get { return GetPropertyValue<int?>(nameof(MaxLength)); }
        set { SetPropertyValue(nameof(MaxLength), value); }
    }

    [XafDisplayName("按词分割")]
    public bool SplitOnWord
    {
        get { return GetPropertyValue<bool>(nameof(SplitOnWord)); }
        set { SetPropertyValue(nameof(SplitOnWord), value); }
    }

    #endregion

    #region VAD 配置

    [XafDisplayName("VAD阈值")]
    public double? VadThreshold
    {
        get { return GetPropertyValue<double?>(nameof(VadThreshold)); }
        set { SetPropertyValue(nameof(VadThreshold), value); }
    }

    [XafDisplayName("VAD最小语音时长(毫秒)")]
    public int? VadMinSpeechDurationMs
    {
        get { return GetPropertyValue<int?>(nameof(VadMinSpeechDurationMs)); }
        set { SetPropertyValue(nameof(VadMinSpeechDurationMs), value); }
    }

    [XafDisplayName("VAD最小静音时长(毫秒)")]
    public int? VadMinSilenceDurationMs
    {
        get { return GetPropertyValue<int?>(nameof(VadMinSilenceDurationMs)); }
        set { SetPropertyValue(nameof(VadMinSilenceDurationMs), value); }
    }

    #endregion

    #region 使用统计

    [XafDisplayName("使用次数")]
    public int UsageCount
    {
        get { return GetPropertyValue<int>(nameof(UsageCount)); }
        set { SetPropertyValue(nameof(UsageCount), value); }
    }

    [XafDisplayName("最后使用时间")]
    [ModelDefault("DisplayFormat", "yyyy-MM-dd HH:mm:ss")]
    public DateTime? LastUsedTime
    {
        get { return GetPropertyValue<DateTime?>(nameof(LastUsedTime)); }
        set { SetPropertyValue(nameof(LastUsedTime), value); }
    }

    #endregion

    #region 默认值设置

    public override void AfterConstruction()
    {
        base.AfterConstruction();

        Language = Language.Auto;
        OutputFormat = WhisperOutputFormat.JsonFull;
        EnableVad = false;
        EnableTranslation = false;
        EnableDiarization = false;
        IsSystemPreset = false;

        Temperature = 0.0;
        BestOf = 5;
        BeamSize = 5;

        MaxLength = 50;
        SplitOnWord = false;

        VadThreshold = 0.5;
        VadMinSpeechDurationMs = 250;
        VadMinSilenceDurationMs = 100;

        UsageCount = 0;
    }

    #endregion
}
