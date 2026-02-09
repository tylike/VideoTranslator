using DevExpress.Xpo;
using TrackMenuAttributes;

namespace VT.Module.BusinessObjects;

public class VadTrackInfo : TrackInfo
{
    [Obsolete("代码中必须使用audioSource来源的构造",true)]
    public VadTrackInfo(Session s) : base(s)
    {
    }

    public VadTrackInfo(Session s, AudioSource audioSource) : base(s)
    {
        this.Media = audioSource;
    }

    /// <summary>
    /// 使用此vad的内容和音频内容识别字幕
    /// </summary>
    /// <returns></returns>
    [ContextMenuAction("识别字幕", Tooltip = "使用此vad的内容和音频内容识别字幕", IsAutoCommit = true)]
    public async Task<SRTTrackInfo> GenerateSrt()
    {
        var s = (this.Media as AudioSource);
        return await s.SpeechRecognitionWithVad(this);
    }
}

