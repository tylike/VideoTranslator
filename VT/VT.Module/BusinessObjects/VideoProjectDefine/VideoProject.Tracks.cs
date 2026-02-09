#nullable enable
using DevExpress.Xpo;
using VT.Core;

namespace VT.Module.BusinessObjects;

public partial class VideoProject
{
    [Association, Aggregated]
    public XPCollection<TrackInfo> Tracks
    {
        get
        {
            return GetCollection<TrackInfo>(nameof(Tracks));
        }
    }

    //源视频,
    public VideoSource? GetSourceVideo()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.源视频) as VideoSource;
    }
    //源音频,
    public AudioSource? GetAudioSource()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.源音频) as AudioSource;
    }
    public AudioTrackInfo? GetSourceAudioTrack()
    {
        return Tracks.FirstOrDefault(x => x.Media.MediaType == MediaType.源音频) as AudioTrackInfo;
    }
    //说话音频,
    public AudioSource? GetVocalsdAudio()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.说话音频) as AudioSource;
    }
    public AudioTrackInfo? GetVocalsAudioTrack()
    {
        return Tracks.FirstOrDefault(x => x.Media.MediaType == MediaType.说话音频) as AudioTrackInfo;
    }

    //背景音频,
    public AudioSource? GetBackgrounddAudio()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.背景音频) as AudioSource;
    }
    //静音视频,
    public VideoSource? GetBackgroundAudio()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.静音视频) as VideoSource;
    }
    //源字幕,
    public SRTSource? GetSubtitleSource()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.源字幕) as SRTSource;
    }
    //目标字幕,
    public SRTSource? GetTranslatedSubtitleSource()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.目标字幕) as SRTSource;
    }

    public SRTTrackInfo GetTranslatedSubtitleTrack() => Tracks.FirstOrDefault(x => x.TrackType == MediaType.目标字幕) as SRTTrackInfo;
    //源字幕Vad,
    public SRTSource? GetSubtitleVadSource()
    {
        return MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.源字幕Vad) as SRTSource;
    }
    //源字幕下载,
    public SRTTrackInfo GetVadSubtitleTrackInfo()
    {
        return (SRTTrackInfo)Tracks.FirstOrDefault(x => x.TrackType == MediaType.源字幕Vad);
    }

    public AudioTrackInfo GetVocalsAudioSegmensTrack()
    {
        return this.Tracks.OfType<AudioTrackInfo>().FirstOrDefault(x => x.TrackType == MediaType.源音分段);
    }
}
