using DevExpress.Persistent.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT.Module.BusinessObjects;

public partial class VideoProject
{
    [EditorAlias("VideoPlayer")]
    public VideoProject Player { get => this; }

    public event EventHandler VideoMutePlayChanged;
    public event EventHandler PlayAdjustedAudioChanged;
    public event EventHandler PlayBackgroundAudioChanged;

    public static Action<VideoProject, MediaType?>? OnTrackChanged;

    private bool _videoMutePlay;
    /// <summary>
    /// 视频自带的音频是否静音播放
    /// </summary>
    public bool VideoMutePlay
    {
        get { return _videoMutePlay; }
        set 
        {
            SetPropertyValue(nameof(VideoMutePlay), ref _videoMutePlay, value);            
        }
    }

    private bool _playAdjustedAudio;
    /// <summary>
    /// 是否播放调整后的音频
    /// </summary>
    public bool PlayAdjustedAudio
    {
        get { return _playAdjustedAudio; }
        set 
        {
            SetPropertyValue("PlayAdjustedAudio", ref _playAdjustedAudio, value);            
        }
    }

    /// <summary>
    /// 用于在分段阶段或是不想让播放器自动定位时使用
    /// </summary>
    public bool AutoSyncCurrentClip
    {
        get { return field; }
        set { SetPropertyValue("AutoSeekToCurrentClip", ref field, value); }
    }


    private bool _playBackgroundAudio;
    /// <summary>
    /// 是否播放背景音频
    /// </summary>
    public bool PlayBackgroundAudio
    {
        get { return _playBackgroundAudio; }
        set 
        { 
            SetPropertyValue("PlayBackgroundAudio", ref _playBackgroundAudio, value);            
        }
    }
    protected override void OnChanged(string propertyName, object oldValue, object newValue)
    {
        base.OnChanged(propertyName, oldValue, newValue);
        if (!IsLoading && !IsSaving)
        {
            Console.WriteLine($"[VideoProject] {propertyName} {oldValue}=>{newValue}");

            if (nameof(VideoMutePlay) == propertyName) { OnVideoMutePlayChanged(); }
            if(nameof(PlayAdjustedAudio) == propertyName) { OnPlayAdjustedAudioChanged(); }
            if(nameof(PlayBackgroundAudio) == propertyName) {OnPlayBackgroundAudioChanged(); }
        }
    }
    private void OnVideoMutePlayChanged() => VideoMutePlayChanged?.Invoke(this, EventArgs.Empty);

    private void OnPlayAdjustedAudioChanged() => PlayAdjustedAudioChanged?.Invoke(this, EventArgs.Empty);

    private void OnPlayBackgroundAudioChanged() => PlayBackgroundAudioChanged?.Invoke(this, EventArgs.Empty);
}
