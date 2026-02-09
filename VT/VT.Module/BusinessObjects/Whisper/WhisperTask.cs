using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using VT.Core;

namespace VT.Module.BusinessObjects.Whisper;

[NavigationItem("Whisper")]
public class WhisperTask(Session s) : VTBaseObject(s)
{
    #region 基础信息

    [XafDisplayName("任务名称")]
    [Size(200)]
    public string TaskName
    {
        get { return GetPropertyValue<string>(nameof(TaskName)); }
        set { SetPropertyValue(nameof(TaskName), value); }
    }

    [XafDisplayName("任务状态")]
    public WhisperTaskStatus Status
    {
        get { return GetPropertyValue<WhisperTaskStatus>(nameof(Status)); }
        set { SetPropertyValue(nameof(Status), value); }
    }

    [XafDisplayName("开始时间")]
    [ModelDefault("DisplayFormat", "yyyy-MM-dd HH:mm:ss")]
    public DateTime? StartTime
    {
        get { return GetPropertyValue<DateTime?>(nameof(StartTime)); }
        set { SetPropertyValue(nameof(StartTime), value); }
    }

    [XafDisplayName("结束时间")]
    [ModelDefault("DisplayFormat", "yyyy-MM-dd HH:mm:ss")]
    public DateTime? EndTime
    {
        get { return GetPropertyValue<DateTime?>(nameof(EndTime)); }
        set { SetPropertyValue(nameof(EndTime), value); }
    }

    [XafDisplayName("处理时长(秒)")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double? ProcessingDuration
    {
        get { return GetPropertyValue<double?>(nameof(ProcessingDuration)); }
        set { SetPropertyValue(nameof(ProcessingDuration), value); }
    }

    #endregion

    #region 输入输出配置

    [XafDisplayName("音频文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string AudioFilePath
    {
        get { return GetPropertyValue<string>(nameof(AudioFilePath)); }
        set { SetPropertyValue(nameof(AudioFilePath), value); }
    }

    [XafDisplayName("输出文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string OutputFilePath
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

    #endregion

    #region 音频处理配置

    [XafDisplayName("最大段长度")]
    public int? MaxLength
    {
        get { return GetPropertyValue<int?>(nameof(MaxLength)); }
        set { SetPropertyValue(nameof(MaxLength), value); }
    }

    [XafDisplayName("最大上下文")]
    public int? MaxContext
    {
        get { return GetPropertyValue<int?>(nameof(MaxContext)); }
        set { SetPropertyValue(nameof(MaxContext), value); }
    }

    [XafDisplayName("按词分割")]
    public bool SplitOnWord
    {
        get { return GetPropertyValue<bool>(nameof(SplitOnWord)); }
        set { SetPropertyValue(nameof(SplitOnWord), value); }
    }

    [XafDisplayName("打印进度")]
    public bool PrintProgress
    {
        get { return GetPropertyValue<bool>(nameof(PrintProgress)); }
        set { SetPropertyValue(nameof(PrintProgress), value); }
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

    [XafDisplayName("无回退")]
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

    [XafDisplayName("调试模式")]
    public bool DebugMode
    {
        get { return GetPropertyValue<bool>(nameof(DebugMode)); }
        set { SetPropertyValue(nameof(DebugMode), value); }
    }

    [XafDisplayName("打印特殊字符")]
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

    [XafDisplayName("无时间戳")]
    public bool NoTimestamps
    {
        get { return GetPropertyValue<bool>(nameof(NoTimestamps)); }
        set { SetPropertyValue(nameof(NoTimestamps), value); }
    }

    [XafDisplayName("日志分数")]
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
    public string? SuppressRegex
    {
        get { return GetPropertyValue<string>(nameof(SuppressRegex)); }
        set { SetPropertyValue(nameof(SuppressRegex), value); }
    }

    [XafDisplayName("语法")]
    public string? Grammar
    {
        get { return GetPropertyValue<string>(nameof(Grammar)); }
        set { SetPropertyValue(nameof(Grammar), value); }
    }

    [XafDisplayName("语法规则")]
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

    [XafDisplayName("VAD采样重叠")]
    public double? VadSamplesOverlap
    {
        get { return GetPropertyValue<double?>(nameof(VadSamplesOverlap)); }
        set { SetPropertyValue(nameof(VadSamplesOverlap), value); }
    }

    [XafDisplayName("VAD模型路径")]
    public string? VadModelPath
    {
        get { return GetPropertyValue<string>(nameof(VadModelPath)); }
        set { SetPropertyValue(nameof(VadModelPath), value); }
    }

    #endregion

    #region 其他配置

    [XafDisplayName("字体路径")]
    public string? FontPath
    {
        get { return GetPropertyValue<string>(nameof(FontPath)); }
        set { SetPropertyValue(nameof(FontPath), value); }
    }

    [XafDisplayName("OpenVino设备")]
    public string? OpenVinoDevice
    {
        get { return GetPropertyValue<string>(nameof(OpenVinoDevice)); }
        set { SetPropertyValue(nameof(OpenVinoDevice), value); }
    }

    [XafDisplayName("DTW模型")]
    public string? DtwModel
    {
        get { return GetPropertyValue<string>(nameof(DtwModel)); }
        set { SetPropertyValue(nameof(DtwModel), value); }
    }

    #endregion

    #region 执行结果

    [XafDisplayName("退出代码")]
    public int ExitCode
    {
        get { return GetPropertyValue<int>(nameof(ExitCode)); }
        set { SetPropertyValue(nameof(ExitCode), value); }
    }

    [XafDisplayName("是否成功")]
    public bool IsSuccess
    {
        get { return GetPropertyValue<bool>(nameof(IsSuccess)); }
        set { SetPropertyValue(nameof(IsSuccess), value); }
    }

    [XafDisplayName("错误信息")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "5")]
    public string? ErrorMessage
    {
        get { return GetPropertyValue<string>(nameof(ErrorMessage)); }
        set { SetPropertyValue(nameof(ErrorMessage), value); }
    }

    [XafDisplayName("标准输出")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "10")]
    public string? StandardOutput
    {
        get { return GetPropertyValue<string>(nameof(StandardOutput)); }
        set { SetPropertyValue(nameof(StandardOutput), value); }
    }

    [XafDisplayName("标准错误")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "5")]
    public string? StandardError
    {
        get { return GetPropertyValue<string>(nameof(StandardError)); }
        set { SetPropertyValue(nameof(StandardError), value); }
    }

    [XafDisplayName("生成的文件列表")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "5")]
    public string? GeneratedFiles
    {
        get { return GetPropertyValue<string>(nameof(GeneratedFiles)); }
        set { SetPropertyValue(nameof(GeneratedFiles), value); }
    }

    #endregion

    #region 关联配置

    [XafDisplayName("使用的配置")]
    [Association]
    public WhisperConfig? Config
    {
        get { return GetPropertyValue<WhisperConfig?>(nameof(Config)); }
        set { SetPropertyValue(nameof(Config), value); }
    }

    #endregion

    #region 计算属性

    [XafDisplayName("进度百分比")]
    [ModelDefault("DisplayFormat", "0")]
    public int ProgressPercentage
    {
        get { return GetPropertyValue<int>(nameof(ProgressPercentage)); }
        set { SetPropertyValue(nameof(ProgressPercentage), value); }
    }

    #endregion

    #region 关联结果

    [XafDisplayName("识别结果")]
    //[Association("WhisperTask-WhisperResult")]
    public WhisperResult? Result
    {
        get { return GetPropertyValue<WhisperResult?>(nameof(Result)); }
        set { SetPropertyValue(nameof(Result), value); }
    }

    #endregion

    #region 默认值设置

    public override void AfterConstruction()
    {
        base.AfterConstruction();

        Status = WhisperTaskStatus.Pending;
        Language = Language.Auto;
        OutputFormat = WhisperOutputFormat.JsonFull;
        DetectLanguage = false;
        SplitOnWord = false;
        Translate = false;
        Diarize = false;
        TinyDiarize = false;
        NoGpu = false;
        EnableVad = false;

        MaxLength = 50;
        BestOf = 5;
        BeamSize = 5;
        Temperature = 0.0;

        VadThreshold = 0.5;
        VadMinSpeechDurationMs = 250;
        VadMinSilenceDurationMs = 100;

        ExitCode = 0;
        IsSuccess = false;
        ProgressPercentage = 0;
    }

    #endregion
}
