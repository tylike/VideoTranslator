using DevExpress.Xpo;
using TrackMenuAttributes;

namespace VT.Module.BusinessObjects;

[Obsolete("说话人分离功能已废弃", true)]
public class SpeakerDiarizationTrackInfo : TrackInfo
{
    [Obsolete("代码中必须使用audioSource来源的构造", true)]
    public SpeakerDiarizationTrackInfo(Session s) : base(s)
    {
    }

    public SpeakerDiarizationTrackInfo(Session s, AudioSource audioSource) : base(s)
    {
        this.Media = audioSource;
    }
}
