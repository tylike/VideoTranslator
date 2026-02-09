using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using VT.Core;

namespace VT.Module.BusinessObjects.Whisper;

[NavigationItem("Whisper")]
public class WhisperConfig(Session s) : VTBaseObject(s)
{
    #region 基础信息

    [XafDisplayName("配置名称")]
    [Size(200)]
    public string ConfigName
    {
        get { return GetPropertyValue<string>(nameof(ConfigName)); }
        set { SetPropertyValue(nameof(ConfigName), value); }
    }

    [XafDisplayName("描述")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "3")]
    public string? Description
    {
        get { return GetPropertyValue<string>(nameof(Description)); }
        set { SetPropertyValue(nameof(Description), value); }
    }

    [XafDisplayName("是否为默认配置")]
    public bool IsDefault
    {
        get { return GetPropertyValue<bool>(nameof(IsDefault)); }
        set { SetPropertyValue(nameof(IsDefault), value); }
    }

    #endregion

    #region 输入输出配置

    [XafDisplayName("音频文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? AudioFilePath
    {
        get { return GetPropertyValue<string>(nameof(AudioFilePath)); }
        set { SetPropertyValue(nameof(AudioFilePath), value); }
    }

    [XafDisplayName("输出文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? OutputFilePath
    {
        get { return GetPropertyValue<string>(nameof(OutputFilePath)); }
        set { SetPropertyValue(nameof(OutputFilePath), value); }
    }

    [XafDisplayName("输出格式")]
    public WhisperOutputFormat OutputFormat
    {
        get { return GetPropertyValue<WhisperOutputFormat>(nameof(OutputFormat)); }
        set { SetPropertyValue(nameof(OutputFormat), value); }
    }

    [XafDisplayName("打印进度")]
    public bool PrintProgress
    {
        get { return GetPropertyValue<bool>(nameof(PrintProgress)); }
        set { SetPropertyValue(nameof(PrintProgress), value); }
    }

    [XafDisplayName("不打印输出")]
    public bool NoPrints
    {
        get { return GetPropertyValue<bool>(nameof(NoPrints)); }
        set { SetPropertyValue(nameof(NoPrints), value); }
    }

    #endregion

    #region 模型配置

    [XafDisplayName("模型文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string ModelPath
    {
        get { return GetPropertyValue<string>(nameof(ModelPath)); }
        set { SetPropertyValue(nameof(ModelPath), value); }
    }

    [XafDisplayName("语言")]
    public Language Language
    {
        get { return GetPropertyValue<Language>(nameof(Language)); }
        set { SetPropertyValue(nameof(Language), value); }
    }

    [XafDisplayName("自动检测语言")]
    public bool DetectLanguage
    {
        get { return GetPropertyValue<bool>(nameof(DetectLanguage)); }
        set { SetPropertyValue(nameof(DetectLanguage), value); }
    }

    [XafDisplayName("提示词")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "2")]
    public string? Prompt
    {
        get { return GetPropertyValue<string>(nameof(Prompt)); }
        set { SetPropertyValue(nameof(Prompt), value); }
    }

    [XafDisplayName("保持初始提示词")]
    public bool CarryInitialPrompt
    {
        get { return GetPropertyValue<bool>(nameof(CarryInitialPrompt)); }
        set { SetPropertyValue(nameof(CarryInitialPrompt), value); }
    }

    #endregion

    #region 音频处理配置

    [XafDisplayName("音频上下文大小")]
    public int? AudioContext
    {
        get { return GetPropertyValue<int?>(nameof(AudioContext)); }
        set { SetPropertyValue(nameof(AudioContext), value); }
    }

    [XafDisplayName("偏移量(毫秒)")]
    public int? Offset
    {
        get { return GetPropertyValue<int?>(nameof(Offset)); }
        set { SetPropertyValue(nameof(Offset), value); }
    }

    [XafDisplayName("持续时间(毫秒)")]
    public int? Duration
    {
        get { return GetPropertyValue<int?>(nameof(Duration)); }
        set { SetPropertyValue(nameof(Duration), value); }
    }

    [XafDisplayName("最大上下文")]
    public int? MaxContext
    {
        get { return GetPropertyValue<int?>(nameof(MaxContext)); }
        set { SetPropertyValue(nameof(MaxContext), value); }
    }

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

    #region 解码配置

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

    [XafDisplayName("温度")]
    public double? Temperature
    {
        get { return GetPropertyValue<double?>(nameof(Temperature)); }
        set { SetPropertyValue(nameof(Temperature), value); }
    }

    [XafDisplayName("温度增量")]
    public double? TemperatureInc
    {
        get { return GetPropertyValue<double?>(nameof(TemperatureInc)); }
        set { SetPropertyValue(nameof(TemperatureInc), value); }
    }

    [XafDisplayName("词阈值")]
    public double? WordThreshold
    {
        get { return GetPropertyValue<double?>(nameof(WordThreshold)); }
        set { SetPropertyValue(nameof(WordThreshold), value); }
    }

    [XafDisplayName("熵阈值")]
    public double? EntropyThreshold
    {
        get { return GetPropertyValue<double?>(nameof(EntropyThreshold)); }
        set { SetPropertyValue(nameof(EntropyThreshold), value); }
    }

    [XafDisplayName("对数概率阈值")]
    public double? LogProbThreshold
    {
        get { return GetPropertyValue<double?>(nameof(LogProbThreshold)); }
        set { SetPropertyValue(nameof(LogProbThreshold), value); }
    }

    [XafDisplayName("无语音阈值")]
    public double? NoSpeechThreshold
    {
        get { return GetPropertyValue<double?>(nameof(NoSpeechThreshold)); }
        set { SetPropertyValue(nameof(NoSpeechThreshold), value); }
    }

    [XafDisplayName("不使用温度回退")]
    public bool NoFallback
    {
        get { return GetPropertyValue<bool>(nameof(NoFallback)); }
        set { SetPropertyValue(nameof(NoFallback), value); }
    }

    [XafDisplayName("翻译")]
    public bool Translate
    {
        get { return GetPropertyValue<bool>(nameof(Translate)); }
        set { SetPropertyValue(nameof(Translate), value); }
    }

    #endregion

    #region 高级配置

    [XafDisplayName("调试模式")]
    public bool DebugMode
    {
        get { return GetPropertyValue<bool>(nameof(DebugMode)); }
        set { SetPropertyValue(nameof(DebugMode), value); }
    }

    [XafDisplayName("说话人分离")]
    public bool Diarize
    {
        get { return GetPropertyValue<bool>(nameof(Diarize)); }
        set { SetPropertyValue(nameof(Diarize), value); }
    }

    [XafDisplayName("TinyDiarize")]
    public bool TinyDiarize
    {
        get { return GetPropertyValue<bool>(nameof(TinyDiarize)); }
        set { SetPropertyValue(nameof(TinyDiarize), value); }
    }

    [XafDisplayName("打印特殊标记")]
    public bool PrintSpecial
    {
        get { return GetPropertyValue<bool>(nameof(PrintSpecial)); }
        set { SetPropertyValue(nameof(PrintSpecial), value); }
    }

    [XafDisplayName("打印颜色")]
    public bool PrintColors
    {
        get { return GetPropertyValue<bool>(nameof(PrintColors)); }
        set { SetPropertyValue(nameof(PrintColors), value); }
    }

    [XafDisplayName("打印置信度")]
    public bool PrintConfidence
    {
        get { return GetPropertyValue<bool>(nameof(PrintConfidence)); }
        set { SetPropertyValue(nameof(PrintConfidence), value); }
    }

    [XafDisplayName("不打印时间戳")]
    public bool NoTimestamps
    {
        get { return GetPropertyValue<bool>(nameof(NoTimestamps)); }
        set { SetPropertyValue(nameof(NoTimestamps), value); }
    }

    [XafDisplayName("记录分数")]
    public bool LogScore
    {
        get { return GetPropertyValue<bool>(nameof(LogScore)); }
        set { SetPropertyValue(nameof(LogScore), value); }
    }

    [XafDisplayName("禁用GPU")]
    public bool NoGpu
    {
        get { return GetPropertyValue<bool>(nameof(NoGpu)); }
        set { SetPropertyValue(nameof(NoGpu), value); }
    }

    [XafDisplayName("Flash Attention")]
    public bool FlashAttention
    {
        get { return GetPropertyValue<bool>(nameof(FlashAttention)); }
        set { SetPropertyValue(nameof(FlashAttention), value); }
    }

    [XafDisplayName("抑制非语音标记")]
    public bool SuppressNonSpeechTokens
    {
        get { return GetPropertyValue<bool>(nameof(SuppressNonSpeechTokens)); }
        set { SetPropertyValue(nameof(SuppressNonSpeechTokens), value); }
    }

    [XafDisplayName("抑制正则表达式")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? SuppressRegex
    {
        get { return GetPropertyValue<string>(nameof(SuppressRegex)); }
        set { SetPropertyValue(nameof(SuppressRegex), value); }
    }

    [XafDisplayName("语法")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "2")]
    public string? Grammar
    {
        get { return GetPropertyValue<string>(nameof(Grammar)); }
        set { SetPropertyValue(nameof(Grammar), value); }
    }

    [XafDisplayName("语法规则")]
    [Size(200)]
    public string? GrammarRule
    {
        get { return GetPropertyValue<string>(nameof(GrammarRule)); }
        set { SetPropertyValue(nameof(GrammarRule), value); }
    }

    [XafDisplayName("语法惩罚")]
    public double? GrammarPenalty
    {
        get { return GetPropertyValue<double?>(nameof(GrammarPenalty)); }
        set { SetPropertyValue(nameof(GrammarPenalty), value); }
    }

    #endregion

    #region VAD 配置

    [XafDisplayName("启用VAD")]
    public bool EnableVad
    {
        get { return GetPropertyValue<bool>(nameof(EnableVad)); }
        set { SetPropertyValue(nameof(EnableVad), value); }
    }

    [XafDisplayName("VAD模型路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? VadModelPath
    {
        get { return GetPropertyValue<string>(nameof(VadModelPath)); }
        set { SetPropertyValue(nameof(VadModelPath), value); }
    }

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

    [XafDisplayName("VAD最大语音时长(秒)")]
    public int? VadMaxSpeechDurationS
    {
        get { return GetPropertyValue<int?>(nameof(VadMaxSpeechDurationS)); }
        set { SetPropertyValue(nameof(VadMaxSpeechDurationS), value); }
    }

    [XafDisplayName("VAD语音填充(毫秒)")]
    public int? VadSpeechPadMs
    {
        get { return GetPropertyValue<int?>(nameof(VadSpeechPadMs)); }
        set { SetPropertyValue(nameof(VadSpeechPadMs), value); }
    }

    [XafDisplayName("VAD样本重叠")]
    public double? VadSamplesOverlap
    {
        get { return GetPropertyValue<double?>(nameof(VadSamplesOverlap)); }
        set { SetPropertyValue(nameof(VadSamplesOverlap), value); }
    }

    #endregion

    #region 其他配置

    [XafDisplayName("字体路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? FontPath
    {
        get { return GetPropertyValue<string>(nameof(FontPath)); }
        set { SetPropertyValue(nameof(FontPath), value); }
    }

    [XafDisplayName("OpenVINO设备")]
    [Size(50)]
    public string? OpenVinoDevice
    {
        get { return GetPropertyValue<string>(nameof(OpenVinoDevice)); }
        set { SetPropertyValue(nameof(OpenVinoDevice), value); }
    }

    [XafDisplayName("DTW模型")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? DtwModel
    {
        get { return GetPropertyValue<string>(nameof(DtwModel)); }
        set { SetPropertyValue(nameof(DtwModel), value); }
    }

    #endregion

    #region 关联任务

    [XafDisplayName("任务列表")]
    [Association()]
    public XPCollection<WhisperTask> Tasks
    {
        get { return GetCollection<WhisperTask>(nameof(Tasks)); }
    }

    #endregion

    #region 默认值设置

    public override void AfterConstruction()
    {
        base.AfterConstruction();

        Language = Language.Auto;
        OutputFormat = WhisperOutputFormat.JsonFull;
        PrintProgress = true;
        DetectLanguage = false;
        SplitOnWord = false;
        Translate = false;
        Diarize = false;
        TinyDiarize = false;
        DebugMode = false;
        PrintSpecial = false;
        PrintColors = false;
        PrintConfidence = false;
        NoTimestamps = false;
        LogScore = false;
        NoGpu = false;
        FlashAttention = true;
        SuppressNonSpeechTokens = false;
        NoFallback = false;
        EnableVad = false;
        CarryInitialPrompt = false;

        MaxLength = 50;
        MaxContext = 0;
        BestOf = 5;
        BeamSize = 5;
        Temperature = 0.0;
        TemperatureInc = 0.2;
        WordThreshold = 0.01;
        EntropyThreshold = 2.4;
        LogProbThreshold = -1.0;
        NoSpeechThreshold = 0.6;
        GrammarPenalty = 100.0;

        VadThreshold = 0.5;
        VadMinSpeechDurationMs = 250;
        VadMinSilenceDurationMs = 100;
        VadMaxSpeechDurationS = int.MaxValue;
        VadSpeechPadMs = 30;
        VadSamplesOverlap = 0.1;
    }

    #endregion
}
