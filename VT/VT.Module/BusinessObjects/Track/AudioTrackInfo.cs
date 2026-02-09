using DevExpress.ExpressApp;
using DevExpress.Xpo;
using DevExpress.XtraSpreadsheet.Commands.Internal;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.ComponentModel;
using TrackMenuAttributes;
using VadTimeProcessor.Services;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VideoTranslator.SRT.Core.Models;
using VT.Core;
using VT.Module.BusinessObjects;

namespace VT.Module.BusinessObjects;
public class AudioTrackInfo(Session s) : TrackInfo(s)
{
    private readonly ILogger _logger = Log.ForContext<AudioTrackInfo>();

    [ContextMenuAction("用翻译字幕分段", IsAutoCommit = true)]
    public async Task SegmentByDefaultSourceSrt()
    {
        var srt = VideoProject.GetTranslatedSubtitleTrack();
        await SegmentAsync(srt);
    }

    /// <summary>
    /// 使用字幕文件对音频进行分段
    /// </summary>
    /// <param name="srt"></param>
    /// <returns></returns>
    public async Task<AudioTrackInfo> SegmentSourceAudioBySrt(SRTTrackInfo srt)
    {
        IServices self = this;
        VideoProject videoProject = VideoProject;
        videoProject.AutoSyncCurrentClip = false;
        var rst = await SegmentAsync(srt);
        videoProject.AutoSyncCurrentClip = true;
        return rst;
    }
    async Task<AudioTrackInfo> SegmentAsync(SRTTrackInfo srt)
    {
        IServices self = this;
        var progress = self.ProgressService;
        try
        {
            IEnumerable<ISrtSubtitle> subtitles = srt.Segments.OfType<SRTClip>();

            if (!subtitles.Any())
            {
                throw new InvalidOperationException("字幕文件为空或格式不正确");
            }

            progress?.Report($"正在分段，共 {subtitles.Count()} 个字幕");

            var audioSegmentsDir = Path.Combine(VideoProject.ProjectPath, "audio_segments");
            Directory.CreateDirectory(audioSegmentsDir);

            var audioPathForSegmentation = this.Media.FileFullName;

            await self.AudioService.SegmentAudioBySubtitleSinglePassAsync(
                audioPathForSegmentation,
                subtitles,
                audioSegmentsDir);
            var at = new AudioTrackInfo(Session)
            {
                Title = this.Title + "_分段",
                TrackType = MediaType.源音分段,
                Media = this.Media,RelationSrt = srt
            };
            this.VideoProject.Tracks.Add(at);

            foreach (var subtitle in subtitles)
            {
                var audioSegmentPath = Path.Combine(audioSegmentsDir, $"segment_{subtitle.Index:0000}.wav");
                if (File.Exists(audioSegmentPath))
                {
                    var ac = new AudioClip(Session)
                    {
                        Index = subtitle.Index,
                        Start = subtitle.StartTime,
                        End = subtitle.EndTime.Add(TimeSpan.FromMilliseconds(200)),
                        Text = subtitle.Text
                    };
                    await ac.SetAudioFile(audioSegmentPath);
                    var srtClip = subtitle as SRTClip;
                    if (srtClip != null)
                    {
                        srtClip.TTSReference = ac;
                    }
                    at.Segments.Add(ac);
                }
            }
            return at;

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// 在tts结果音频上，使用标准时间轴，进行调整
    /// </summary>
    /// <param name="useEndEmptyTime"></param>
    /// <returns></returns>
    public async Task<AudioTrackInfo> Adjust(bool useEndEmptyTime,SRTTrackInfo srt)
    {
        var newTrack = new AudioTrackInfo(Session)
        {
            Title = this.Title + "_调整后",
            TrackType = MediaType.调整音频段,
            Media = this.Media
        };
        this.VideoProject.Tracks.Add(newTrack);

        IServices self = this;
        var progress = self.ProgressService;

        var semaphore = new SemaphoreSlim(20);
        var totalSegments = this.Segments.Count;
        var completedCount = 0;
        this.RelationSrt = srt;
        var tasks = this.Segments.OfType<AudioClip>().Select(async item =>
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var rst = await item.Adjust(useEndEmptyTime).ConfigureAwait(false);

                return rst;
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
            tasks.Remove(completedTask);

            if (completedTask.IsCompletedSuccessfully)
            {
                var result = await completedTask.ConfigureAwait(false);
                newTrack.Segments.Add(result);

                completedCount++;
                progress?.Report($"调整音频片段: {completedCount}/{totalSegments}");
            }
        }


        return newTrack;
    }

    /// <summary>
    /// 生成完整的目标音频
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<AudioTrackInfo> GenerateTargetAudio(AudioTrackInfo backgroundAudioTrackInfo)
    {
        IServices self = this;
        var videoProject = VideoProject;
        videoProject.ValidateSourceAudio();

        var outputPath = Path.Combine(videoProject.ProjectPath, "merged_audio.wav");

        var audioInfo = await self.AudioService.GetAudioInfoAsync(videoProject.SourceAudioPath);

        TimeSpan GetEnd(AudioClip clip)
        {
            if (clip.NextClip == null)
                return clip.End;
            return clip.NextClip.Start;
        }

        var audioClipsWithTime = this.Segments.OfType<AudioClip>()
            .Where(c => !string.IsNullOrEmpty(c.FilePath) &&
                        File.Exists(c.FilePath))
            .Select(c => new AudioClipWithTime
            {
                Index = c.Index,
                FilePath = c.FilePath,
                Start = c.Start,
                End = GetEnd(c),
                AudioDurationMs = c.Duration * 1000
            })
            .ToList();

        var backgroundAudioPath = backgroundAudioTrackInfo.Media.FileFullName;
        if (string.IsNullOrEmpty(backgroundAudioPath) || !File.Exists(backgroundAudioPath))
        {
            throw new Exception("背景音频不存在!");
        }

        if (audioClipsWithTime.Count == 0)
        {
            throw new UserFriendlyException("没有可合并的调整后音频片段，请先执行'调整音频片段'");
        }

        await self.AudioService.MergeAudioSegmentsOnTimelineAsync(
            audioClipsWithTime,
            backgroundAudioPath,
            outputPath);
        videoProject.OutputAudioPath = outputPath;
        var rst = await videoProject.CreateAudioSourceAndTrackInfo(MediaType.目标音频, true, outputPath);
        return rst.track;

    }

    #region 上下文菜单操作

    /// <summary>
    /// 播放音频
    /// </summary>
    [ContextMenuAction("播放音频", Order = 10, Group = "播放")]
    public void PlayAudio()
    {
        var audioPath = this.Media?.FileFullName;
        if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = audioPath,
                UseShellExecute = true
            });
        }
    }

    /// <summary>
    /// 导出音频
    /// </summary>
    [ContextMenuAction("导出音频", Order = 20, Group = "导出")]
    public void ExportAudio()
    {
        IServices self = this;
        var videoProject = VideoProject;
        var exportPath = Path.Combine(videoProject.ProjectPath, $"export_audio_{Guid.NewGuid():N}.wav");
        var audioPath = this.Media?.FileFullName;
        if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
        {
            File.Copy(audioPath, exportPath);
        }
    }

    /// <summary>
    /// 生成目标音频
    /// </summary>
    [ContextMenuAction("合并整段音频", Tooltip = "将轨道中的分段音频合并为整段音频，为最终使用的音频", Order = 30, Group = "音频")]
    public async Task GenerateTargetAudioMenuAction()
    {
        //如何取得背景音轨？
        var back = (AudioTrackInfo)VideoProject.Tracks.Single(x => x.TrackType == MediaType.背景音频);
        await GenerateTargetAudio(back);
    }

    /// <summary>
    /// 调整音频速度
    /// </summary>
    [ContextMenuAction("调整音频速度", Order = 40, Group = "调整")]
    public async Task AdjustAudioSpeed()
    {
        await Adjust(true,this.RelationSrt);
    }

    #endregion

    /// <summary>
    /// 人声分离
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [ContextMenuAction("人声分离", IsAutoCommit = true)]
    public async Task<(AudioTrackInfo 人声, AudioTrackInfo 背景)> SeparateAudio()
    {
        //指定项目的分离音频
        IServices self = this;
        var videoProject = VideoProject;
        var result = await self.AudioSeparationService.SeparateVocalAndBackgroundAsync(this.Media.FileFullName, videoProject.ProjectPath);
        if (!result.Success)
        {
            throw new Exception($"音频分离失败: {result.ErrorMessage}");
        }
        var rst = await videoProject.CreateAudioSourceAndTrackInfo(MediaType.说话音频, true, result.VocalAudioPath);
        var t = await videoProject.CreateAudioSourceAndTrackInfo(MediaType.背景音频, true, result.BackgroundAudioPath);
        return (rst.track, t.track);
    }

    [ContextMenuAction("查看VAD不合并", IsAutoCommit = true)]
    public VadTrackInfo GenerateVadNoMerge()
    {
        return GenerateVad(false);
    }
    [ContextMenuAction("查看VAD合并", IsAutoCommit = true)]
    public VadTrackInfo GenerateVadMerge()
    {
        return GenerateVad(true);
    }

    private VadTrackInfo GenerateVad(bool autoMerge)
    {
        var s = this.Media as AudioSource;
        IServices self = this;
        VadDetector.SetProgressService(self.progress);
        var seg = VadDetector.DetectSpeechSegments(this.Media.FileFullName, autoMerge: autoMerge);
        return s.CreateVadTrack(seg);
    }

    /// <summary>
    /// 从音频轨道创建一个字幕轨道
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [ContextMenuAction("识别字幕-VAD", Tooltip = "使用vad的分段，一段一段的识别字幕内容", IsAutoCommit = true)]
    public async Task 识别字幕()
    {
        var vad = VideoProject.Tracks.OfType<VadTrackInfo>().ToArray();
        if (vad.Length == 1)
        {
            IServices self = this;
            var vadService = self.ServiceProvider.GetRequiredService<ISpeechRecognitionServiceVad>();
            var subtitlePath = Path.Combine(VideoProject.ProjectPath, $"track_{Guid.NewGuid()}_subtitle_vad.srt");
            await vadService.RecognizeAudioAsync(this.Media.FileFullName, subtitlePath, VideoProject.SourceLanguage.ToString().ToLower(), vad.First().Segments.OfType<VadClip>());
            await VideoProject.ImportSrt(subtitlePath, MediaType.Subtitles, vad.First());
        }
        else
        {
            throw new Exception("错误,没有Vad!");
        }
    }

    [ContextMenuAction("整段识别字幕", Tooltip = "whisper.cpp整段识别字幕内容", IsAutoCommit = true)]
    public async Task 整段识别字幕()
    {
        IServices self = this;
        var videoProject = this.VideoProject;
        self.ProgressService?.ShowProgress();
        var subtitlePath = videoProject.CombinePath("source.srt");
        await self.SpeechRecognitionService.RecognizeAudioAsync(videoProject.SourceAudioPath, subtitlePath);
        videoProject.SourceSubtitlePath = subtitlePath;
        await videoProject.CreateSubtitleSource(subtitlePath, MediaType.源字幕,null);
        self.ProgressService?.ResetProgress();
    }

    [ContextMenuAction("Vosk识别字幕", Tooltip = "使用Vosk整段识别字幕内容", IsAutoCommit = true)]
    public async Task Vosk识别字幕()
    {
        IServices self = this;
        var videoProject = this.VideoProject;
        self.ProgressService?.ShowProgress();
        var subtitlePath = videoProject.CombinePath("source_vosk.srt");
        var voskService = self.ServiceProvider.GetRequiredService<VoskRecognitionService>();
        var rst = await voskService.RecognizeAsync(videoProject.SourceAudioPath, videoProject.SourceLanguage.ToString().ToLower());

        foreach (var item in rst)
        {
            item.Text = item.Text.Replace("\r\n", " ").Replace("\n", " ");
        }

        await self.SubtitleService.SaveSrtAsync(rst, subtitlePath);
        videoProject.SourceSubtitlePath = subtitlePath;
        await videoProject.CreateSubtitleSource(subtitlePath, MediaType.源字幕, null);
        self.ProgressService?.ResetProgress();
    }

    /// <summary>
    /// 说话人识别
    /// </summary>
    /// <param name="language">语言代码，如 'zh' 或 'en'</param>
    /// <returns></returns>
    [ContextMenuAction("说话人识别", Tooltip = "识别音频中的说话人并标记时间段", IsAutoCommit = true)]
    public async Task<SRTTrackInfo> 说话人识别(string language = "zh")
    {
        IServices self = this;
        var videoProject = this.VideoProject;
        var audioPath = this.Media?.FileFullName;

        if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
        {
            throw new FileNotFoundException("音频文件不存在", audioPath);
        }

        self.ProgressService?.ShowProgress();

        try
        {
            var speakerService = self.ServiceProvider.GetRequiredService<SherpaSpeakerDiarizationService>();           

            var result = await speakerService.DiarizeAsync(audioPath,language);

            var track = new SRTTrackInfo(Session, MediaType.说话人识别, null)
            {
                Title = this.Title + "_说话人识别",
                TrackType = MediaType.说话人识别,
                Media = this.Media
            };

            videoProject.Tracks.Add(track);

            foreach (var segment in result.Segments)
            {
                var clip = new SRTClip(Session)
                {
                    Index = track.Segments.Count + 1,
                    Start = TimeSpan.FromSeconds(segment.Start),
                    End = TimeSpan.FromSeconds(segment.End),
                    Speaker = segment.Speaker
                };
                track.Segments.Add(clip);
            }

            self.ProgressService?.ResetProgress();

            return track;
        }
        catch (Exception ex)
        {
            self.ProgressService?.ResetProgress();
            _logger.Error(ex, "说话人识别失败");
            throw;
        }
    }

    /// <summary>
    /// 说话人识别（中文）
    /// </summary>
    [ContextMenuAction("说话人识别(中文)", Tooltip = "识别中文音频中的说话人", IsAutoCommit = true)]
    public async Task<SRTTrackInfo> 说话人识别中文()
    {
        return await 说话人识别("zh");
    }

    /// <summary>
    /// 说话人识别（英文）
    /// </summary>
    [ContextMenuAction("说话人识别(英文)", Tooltip = "识别英文音频中的说话人", IsAutoCommit = true)]
    public async Task<SRTTrackInfo> 说话人识别英文()
    {
        return await 说话人识别("en");
    }

    /// <summary>
    /// Whisper识别字幕
    /// </summary>
    /// <param name="language">语言代码，如 'zh' 或 'en'</param>
    /// <returns></returns>
    [ContextMenuAction("Whisper识别字幕", Tooltip = "使用Whisper整段识别字幕内容", IsAutoCommit = true)]
    public async Task Whisper识别字幕(string language = "zh")
    {
        IServices self = this;
        var videoProject = this.VideoProject;
        self.ProgressService?.ShowProgress();
        var subtitlePath = videoProject.CombinePath("source_whisper.srt");
        var whisperService = self.ServiceProvider.GetRequiredService<WhisperRecognitionService>();
        var rst = await whisperService.RecognizeAsync(videoProject.SourceAudioPath, videoProject.SourceLanguage.ToString().ToLower(), null);

        foreach (var item in rst)
        {
            item.Text = item.Text.Replace("\r\n", " ").Replace("\n", " ");
        }

        await self.SubtitleService.SaveSrtAsync(rst, subtitlePath);
        videoProject.SourceSubtitlePath = subtitlePath;
        await videoProject.CreateSubtitleSource(subtitlePath, MediaType.源字幕, null);
        self.ProgressService?.ResetProgress();
    }

    /// <summary>
    /// Whisper识别字幕（中文）
    /// </summary>
    [ContextMenuAction("Whisper识别字幕(中文)", Tooltip = "识别中文音频字幕", IsAutoCommit = true)]
    public async Task Whisper识别字幕中文()
    {
        await Whisper识别字幕("zh");
    }

    /// <summary>
    /// Whisper识别字幕（英文）
    /// </summary>
    [ContextMenuAction("Whisper识别字幕(英文)", Tooltip = "识别英文音频字幕", IsAutoCommit = true)]
    public async Task Whisper识别字幕英文()
    {
        await Whisper识别字幕("en");
    }

    /// <summary>
    /// PurfviewFasterWhisper识别字幕
    /// </summary>
    /// <param name="language">语言代码，如 'zh' 或 'en'</param>
    /// <returns></returns>
    [ContextMenuAction("PurfviewFasterWhisper识别字幕", Tooltip = "使用PurfviewFasterWhisper整段识别字幕内容", IsAutoCommit = true)]
    public async Task<SRTTrackInfo> PurfviewFasterWhisper识别字幕(Language language)
    {
        IServices self = this;
        var videoProject = this.VideoProject;
        self.ProgressService?.ShowProgress();
        var subtitlePath = videoProject.CombinePath("source_purfview_whisper.srt");
        var purfviewWhisperService = self.ServiceProvider.GetRequiredService<PurfviewFasterWhisperRecognitionService>();
        var rst = await purfviewWhisperService.RecognizeAsync(videoProject.SourceAudioPath, language, null);

        foreach (var item in rst)
        {
            item.Text = item.Text.Replace("\r\n", " ").Replace("\n", " ");
        }

        await self.SubtitleService.SaveSrtAsync(rst, subtitlePath);
        videoProject.SourceSubtitlePath = subtitlePath;
        var track = await videoProject.CreateSubtitleSource(subtitlePath, MediaType.源字幕, null);
        self.ProgressService?.ResetProgress();
        return track.track;
    }

    /// <summary>
    /// PurfviewFasterWhisper识别字幕（中文）
    /// </summary>
    [ContextMenuAction("PurfviewFasterWhisper识别字幕(中文)", Tooltip = "识别中文音频字幕", IsAutoCommit = true)]
    public async Task PurfviewFasterWhisper识别字幕中文()
    {
        await PurfviewFasterWhisper识别字幕(Language.Chinese);
    }

    /// <summary>
    /// PurfviewFasterWhisper识别字幕（英文）
    /// </summary>
    [ContextMenuAction("PurfviewFasterWhisper识别字幕(英文)", Tooltip = "识别英文音频字幕", IsAutoCommit = true)]
    public async Task<SRTTrackInfo> PurfviewFasterWhisper识别字幕英文()
    {
        return await PurfviewFasterWhisper识别字幕(Language.English);
    }

    /// <summary>
    /// 关联的字幕轨道
    /// 1.如果是源音分段轨道，则关联源字幕轨道，即使用该字幕对音频进行的分段
    /// </summary>
    public SRTTrackInfo RelationSrt
    {
        get { return field; }
        set { SetPropertyValue("RelationSrt", ref field, value); }
    }

}

