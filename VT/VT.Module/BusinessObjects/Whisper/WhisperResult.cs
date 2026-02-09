using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using VT.Module.BusinessObjects;

namespace VT.Module.BusinessObjects.Whisper;

[NavigationItem("Whisper")]
public class WhisperResult(Session s) : VTBaseObject(s)
{
    #region 基础信息

    [XafDisplayName("结果名称")]
    [Size(200)]
    public string ResultName
    {
        get { return GetPropertyValue<string>(nameof(ResultName)); }
        set { SetPropertyValue(nameof(ResultName), value); }
    }

    [XafDisplayName("关联任务")]
    //[Association("WhisperTask-WhisperResult")]
    public WhisperTask? Task
    {
        get { return GetPropertyValue<WhisperTask?>(nameof(Task)); }
        set { SetPropertyValue(nameof(Task), value); }
    }

    [XafDisplayName("是否成功")]
    public bool Success
    {
        get { return GetPropertyValue<bool>(nameof(Success)); }
        set { SetPropertyValue(nameof(Success), value); }
    }

    [XafDisplayName("退出代码")]
    public int ExitCode
    {
        get { return GetPropertyValue<int>(nameof(ExitCode)); }
        set { SetPropertyValue(nameof(ExitCode), value); }
    }

    [XafDisplayName("错误信息")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "10")]
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
    [ModelDefault("RowCount", "10")]
    public string? StandardError
    {
        get { return GetPropertyValue<string>(nameof(StandardError)); }
        set { SetPropertyValue(nameof(StandardError), value); }
    }

    [XafDisplayName("处理时间")]
    public TimeSpan ProcessingTime
    {
        get { return GetPropertyValue<TimeSpan>(nameof(ProcessingTime)); }
        set { SetPropertyValue(nameof(ProcessingTime), value); }
    }

    [XafDisplayName("生成的文件")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "5")]
    public string? GeneratedFiles
    {
        get { return GetPropertyValue<string>(nameof(GeneratedFiles)); }
        set { SetPropertyValue(nameof(GeneratedFiles), value); }
    }

    [XafDisplayName("输出路径")]
    [Size(SizeAttribute.Unlimited)]
    public string? OutputPath
    {
        get { return GetPropertyValue<string>(nameof(OutputPath)); }
        set { SetPropertyValue(nameof(OutputPath), value); }
    }

    #endregion

    #region 识别内容

    [XafDisplayName("识别文本")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "10")]
    public string? RecognizedText
    {
        get { return GetPropertyValue<string>(nameof(RecognizedText)); }
        set { SetPropertyValue(nameof(RecognizedText), value); }
    }

    [XafDisplayName("完整JSON内容")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "20")]
    public string? JsonContent
    {
        get { return GetPropertyValue<string>(nameof(JsonContent)); }
        set { SetPropertyValue(nameof(JsonContent), value); }
    }

    [XafDisplayName("SRT内容")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "20")]
    public string? SrtContent
    {
        get { return GetPropertyValue<string>(nameof(SrtContent)); }
        set { SetPropertyValue(nameof(SrtContent), value); }
    }

    [XafDisplayName("VTT内容")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "20")]
    public string? VttContent
    {
        get { return GetPropertyValue<string>(nameof(VttContent)); }
        set { SetPropertyValue(nameof(VttContent), value); }
    }

    [XafDisplayName("TXT内容")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "20")]
    public string? TxtContent
    {
        get { return GetPropertyValue<string>(nameof(TxtContent)); }
        set { SetPropertyValue(nameof(TxtContent), value); }
    }

    #endregion

    #region 统计信息

    [XafDisplayName("总时长(秒)")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double TotalDuration
    {
        get { return GetPropertyValue<double>(nameof(TotalDuration)); }
        set { SetPropertyValue(nameof(TotalDuration), value); }
    }

    [XafDisplayName("段落数量")]
    public int SegmentCount
    {
        get { return GetPropertyValue<int>(nameof(SegmentCount)); }
        set { SetPropertyValue(nameof(SegmentCount), value); }
    }

    [XafDisplayName("总词数")]
    public int WordCount
    {
        get { return GetPropertyValue<int>(nameof(WordCount)); }
        set { SetPropertyValue(nameof(WordCount), value); }
    }

    [XafDisplayName("平均置信度")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double? AverageConfidence
    {
        get { return GetPropertyValue<double?>(nameof(AverageConfidence)); }
        set { SetPropertyValue(nameof(AverageConfidence), value); }
    }

    #endregion

    #region 语言信息

    [XafDisplayName("检测到的语言")]
    [Size(50)]
    public string? DetectedLanguage
    {
        get { return GetPropertyValue<string>(nameof(DetectedLanguage)); }
        set { SetPropertyValue(nameof(DetectedLanguage), value); }
    }

    [XafDisplayName("语言置信度")]
    [ModelDefault("DisplayFormat", "0.000")]
    public double? LanguageConfidence
    {
        get { return GetPropertyValue<double?>(nameof(LanguageConfidence)); }
        set { SetPropertyValue(nameof(LanguageConfidence), value); }
    }

    #endregion

    #region 文件路径

    [XafDisplayName("JSON文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? JsonFilePath
    {
        get { return GetPropertyValue<string>(nameof(JsonFilePath)); }
        set { SetPropertyValue(nameof(JsonFilePath), value); }
    }

    [XafDisplayName("SRT文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? SrtFilePath
    {
        get { return GetPropertyValue<string>(nameof(SrtFilePath)); }
        set { SetPropertyValue(nameof(SrtFilePath), value); }
    }

    [XafDisplayName("VTT文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? VttFilePath
    {
        get { return GetPropertyValue<string>(nameof(VttFilePath)); }
        set { SetPropertyValue(nameof(VttFilePath), value); }
    }

    [XafDisplayName("TXT文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string? TxtFilePath
    {
        get { return GetPropertyValue<string>(nameof(TxtFilePath)); }
        set { SetPropertyValue(nameof(TxtFilePath), value); }
    }

    #endregion

    #region 关联段落

    [XafDisplayName("段落列表")]
    [Association]
    public XPCollection<WhisperSegment> Segments
    {
        get { return GetCollection<WhisperSegment>(nameof(Segments)); }
    }

    #endregion
}
