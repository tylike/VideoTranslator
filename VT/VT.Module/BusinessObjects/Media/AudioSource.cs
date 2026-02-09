using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Diagnostics;
using System.Windows;
using TrackMenuAttributes;
using VadTimeProcessor.Models;
using VadTimeProcessor.Services;
using VideoTranslator.Interfaces;
using VT.Core;

namespace VT.Module.BusinessObjects;

public class AudioSource(Session s) : MediaSource(s)
{
    private readonly ILogger _logger = Log.ForContext<AudioSource>();

    [XafDisplayName("音频时长（秒）")]
    public double DurationInSeconds
    {
        get { return GetPropertyValue<double>(nameof(DurationInSeconds)); }
        set { SetPropertyValue(nameof(DurationInSeconds), value); }
    }

    public int GapTip
    {
        get { return field; }
        set { SetPropertyValue("GapTip", ref field, value); }
    }

    [Association, Aggregated]
    public XPCollection<VadSegment> VadSegments
    {
        get { return GetCollection<VadSegment>(nameof(VadSegments)); }
    }
    IAudioService audioService => Session.ServiceProvider.GetRequiredService<IAudioService>();

    #region 原有方法

    public async Task AutoParseVadSegments()
    {
        var rst = VadDetector.DetectSpeechSegments(this.FileFullName);
        await CreateSegments(rst);
    }

    public async Task CreateSegments(LinkedList<ISpeechSegment> rst)
    {
        int index = 1;
        VadSegment? previousSegment = null;

        foreach (var item in rst)
        {
            var vadSegment = new VadSegment(Session)
            {
                Index = index++,
                StartMS = item.StartMS,
                EndMS = item.EndMS,
                Previous = previousSegment
            };

            VadSegments.Add(vadSegment);

            if (previousSegment != null)
            {
                previousSegment.Next = vadSegment;
            }

            previousSegment = vadSegment;
        }

        this.DurationInSeconds = await audioService.GetAudioDurationSecondsAsyn(this.FileFullName);
    }

    [EditorAlias("Waveform")]
    public AudioSource AudioWave { get => this; }

    

    public async Task<SRTTrackInfo> SpeechRecognitionWithVad(VadTrackInfo vadTrack)
    {
        IServices self = this;
        var videoProject = VideoProject;
        videoProject.ValidateSourceAudio().Validate();
        var subtitlePath = Path.Combine(videoProject.ProjectPath, "source_subtitle_vad.srt");

        var vadService = self.ServiceProvider.GetRequiredService<ISpeechRecognitionServiceVad>();

        // 传入预检测的VAD段落，避免重复检测
        var vocalAudio = this;
        var vadSegments = vadTrack.Segments.OfType<VadClip>();
        if ((vadSegments?.Count() ?? 0) < 1)
        {
            throw new Exception("没有vad内容!");
        }
        await vadService.RecognizeAudioAsync(this.FileFullName, subtitlePath, VideoProject.SourceLanguage.ToString().ToLower(), vadSegments);
        var rst = await VideoProject.CreateSubtitleSource(subtitlePath, MediaType.源字幕Vad, vadTrack);
        return rst.track;
    }
    public void CreateVadTrack()
    {
        IServices self = this;
        var vadTrack = new VadTrackInfo(Session, this)
        {
            TrackType = MediaType.Vad
        };
        foreach (var item in VadSegments)
        {
            var seg = new VadClip(Session)
            {
                Start = TimeSpan.FromMilliseconds(item.StartMS),
                End = TimeSpan.FromMilliseconds(item.EndMS),
                Index = item.Index
            };
            vadTrack.Segments.Add(seg);
        }
        VideoProject.Tracks.Add(vadTrack);
    }
    public VadTrackInfo CreateVadTrack(LinkedList<ISpeechSegment> segments)
    {
        IServices self = this;
        var vadTrack = new VadTrackInfo(Session, this)
        {
            Media = this,
            TrackType = MediaType.Vad
        };
        foreach (var item in segments)
        {
            var seg = new VadClip(Session)
            {
                Start = TimeSpan.FromMilliseconds(item.StartMS),
                End = TimeSpan.FromMilliseconds(item.EndMS),
                Index = item.Index
            };
            vadTrack.Segments.Add(seg);
        }
        VideoProject.Tracks.Add(vadTrack);
        return vadTrack;
    }

    #endregion
}

