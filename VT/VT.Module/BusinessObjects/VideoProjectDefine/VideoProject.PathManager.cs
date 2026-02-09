#nullable enable
using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;

namespace VT.Module.BusinessObjects;

public partial class VideoProject
{
    public string CombinePath(string fileName)
    {
        fileName.CreateDirectory();
        return Path.Combine(this.ProjectPath, fileName);
    }

    #region 路径
    [XafDisplayName("源视频路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string SourceVideoPath
    {
        get { return GetPropertyValue<string>(nameof(SourceVideoPath)); }
        set { SetPropertyValue(nameof(SourceVideoPath), value); }
    }

    [XafDisplayName("源音频路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string SourceAudioPath
    {
        get { return GetPropertyValue<string>(nameof(SourceAudioPath)); }
        set { SetPropertyValue(nameof(SourceAudioPath), value); }
    }

    [XafDisplayName("静音视频路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string SourceMutedVideoPath
    {
        get { return GetPropertyValue<string>(nameof(SourceMutedVideoPath)); }
        set { SetPropertyValue(nameof(SourceMutedVideoPath), value); }
    }

    [XafDisplayName("人声音频")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string SourceVocalAudioPath
    {
        get { return GetPropertyValue<string>(nameof(SourceVocalAudioPath)); }
        set { SetPropertyValue(nameof(SourceVocalAudioPath), value); }
    }

    [XafDisplayName("背景音频")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string SourceBackgroundAudioPath
    {
        get { return GetPropertyValue<string>(nameof(SourceBackgroundAudioPath)); }
        set { SetPropertyValue(nameof(SourceBackgroundAudioPath), value); }
    }

    [XafDisplayName("源字幕路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string SourceSubtitlePath
    {
        get { return GetPropertyValue<string>(nameof(SourceSubtitlePath)); }
        set { SetPropertyValue(nameof(SourceSubtitlePath), value); }
    }

    [XafDisplayName("翻译字幕路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string TranslatedSubtitlePath
    {
        get { return GetPropertyValue<string>(nameof(TranslatedSubtitlePath)); }
        set { SetPropertyValue(nameof(TranslatedSubtitlePath), value); }
    }

    [XafDisplayName("输出音频路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string OutputAudioPath
    {
        get { return GetPropertyValue<string>(nameof(OutputAudioPath)); }
        set { SetPropertyValue(nameof(OutputAudioPath), value); }
    }

    [XafDisplayName("输出视频路径")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string OutputVideoPath
    {
        get { return GetPropertyValue<string>(nameof(OutputVideoPath)); }
        set { SetPropertyValue(nameof(OutputVideoPath), value); }
    }
    #endregion

    #region 验证
    public ValidateResult ValidateSourceVideo() => new ValidateFileResult(this.SourceVideoPath, "输入视频文件");
    public ValidateResult ValidateSourceAudio() => new ValidateFileResult(this.SourceAudioPath, "输入音频文件");
    public ValidateResult ValidateSourceSubtitle() => new ValidateFileResult(this.SourceSubtitlePath, "输入字幕文件");
    public ValidateResult ValidateTranslatedSubtitle() => new ValidateFileResult(this.TranslatedSubtitlePath, "输出字幕文件");
    public ValidateResult ValidateOutputAudio() => new ValidateFileResult(this.OutputAudioPath, "输出音频文件");
    public ValidateResult ValidateOutputVideo() => new ValidateFileResult(this.OutputVideoPath, "输出视频文件"); 
    #endregion
}
