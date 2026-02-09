using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.SRT.Core.Models;
using System.IO;
using System.Threading;
using TrackMenuAttributes;
using VT.Core;

namespace VT.Module.BusinessObjects;

public class SRTTrackInfo : TrackInfo
{
    #region ctor
    [Obsolete("需要使用带有媒体参数的构造函数", true)]
    public SRTTrackInfo(Session s) : base(s)
    {

    }

    public SRTTrackInfo(Session s, MediaType type, VadTrackInfo vadTrack) : base(s)
    {
        this.TrackType = type;
        this.VadTrack = vadTrack;
    }
    #endregion

    /// <summary>    
    /// 此字幕是使用哪个VAD轨道生成的
    /// </summary>
    public VadTrackInfo VadTrack
    {
        get { return field; }
        set { SetPropertyValue("VadTrack", ref field, value); }
    }

    /// <summary>
    /// 生成tts时，使用此track中的分段做为参考音频
    /// </summary>
    public AudioTrackInfo AudioTrackSegment
    {
        get { return field; }
        set { SetPropertyValue("AudioTrackSegment", ref field, value); }
    }


    public override void AfterConstruction()
    {
        base.AfterConstruction();
        this.TrackType = MediaType.Subtitles;
    }

    #region 翻译
    [DisplayName("语言")]
    public Language Language
    {
        get { return field; }
        set { SetPropertyValue("Language", ref field, value); }
    }

    /// <summary>
    /// 使用Google翻译API翻译字幕
    /// </summary>
    public async Task<SRTTrackInfo> TranslateSRTByGoogle(MediaType targetType)
    {
        IServices s = this;
        var videoProject = VideoProject;

        var subtitles = Segments.OfType<SRTClip>();
        var totalCount = subtitles.Count();

        if (totalCount == 0)
        {
            throw new InvalidOperationException("没有可翻译的字幕片段");
        }

        s.ProgressService?.ShowProgress();
        s.ProgressService?.SetStatusMessage($"正在翻译 {totalCount} 个字幕...");

        var translatedSubtitles = await s.TranslationService.TranslateSubtitlesAsync(
            subtitles,
            TranslationApi.Google,
            videoProject.CustomSystemPrompt,
            totalCount,
            videoProject.TargetLanguage,
            videoProject.SourceLanguage);

        var translatedSubtitlePath = Path.Combine(videoProject.ProjectPath, $"translated_subtitle_{Guid.NewGuid():N}.srt");

        await SaveTranslatedSubtitlesAsync(translatedSubtitles, translatedSubtitlePath);

        #region srt source
        var translatedSubtitleSource = new SRTSource(Session)
        {
            FileFullName = translatedSubtitlePath,
            MediaType = MediaType.目标字幕
        };
        videoProject.MediaSources.Add(translatedSubtitleSource);
        #endregion

        #region track
        var translatedTrack = new SRTTrackInfo(Session, targetType, this.VadTrack)
        {
            Media = translatedSubtitleSource
        };
        videoProject.Tracks.Add(translatedTrack);


        int index = 1;
        foreach (var translatedSubtitle in translatedSubtitles)
        {
            var srtClip = new SRTClip(Session)
            {
                Index = index++,
                Start = translatedSubtitle.StartTime,
                End = translatedSubtitle.EndTime,
                Text = translatedSubtitle.Text
            };
            translatedTrack.Segments.Add(srtClip);
        }
        #endregion

        videoProject.TranslatedSubtitlePath = translatedSubtitlePath;

        s.ProgressService?.ResetProgress();
        return translatedTrack;
    }

    /// <summary>
    /// 使用LMStudio翻译API翻译字幕
    /// </summary>
    public async Task<SRTTrackInfo> TranslateSRTByLMStudio(MediaType targetType)
    {
        IServices s = this;
        var videoProject = VideoProject;

        var subtitles = Segments.OfType<SRTClip>();
        var totalCount = subtitles.Count();

        if (totalCount == 0)
        {
            throw new InvalidOperationException("没有可翻译的字幕片段");
        }

        s.ProgressService?.ShowProgress();
        s.ProgressService?.SetStatusMessage($"正在使用LMStudio翻译 {totalCount} 个字幕...");

        var translatedSubtitles = await s.TranslationService.TranslateSubtitlesAsync(
            subtitles,
            TranslationApi.LMStudio,
            videoProject.CustomSystemPrompt,
            30,
            videoProject.TargetLanguage,
            videoProject.SourceLanguage);

        var translatedSubtitlePath = Path.Combine(videoProject.ProjectPath, $"translated_subtitle_lmstudio_{Guid.NewGuid():N}.srt");

        await SaveTranslatedSubtitlesAsync(translatedSubtitles, translatedSubtitlePath);

        var translatedSubtitleSource = new SRTSource(Session)
        {
            FileFullName = translatedSubtitlePath,
            MediaType = MediaType.目标字幕
        };
        videoProject.MediaSources.Add(translatedSubtitleSource);

        var translatedTrack = new SRTTrackInfo(Session, targetType, this.VadTrack)
        {
            Media = translatedSubtitleSource
        };
        videoProject.Tracks.Add(translatedTrack);

        int index = 1;
        foreach (var translatedSubtitle in translatedSubtitles)
        {
            var srtClip = new SRTClip(Session)
            {
                Index = index++,
                Start = translatedSubtitle.StartTime,
                End = translatedSubtitle.EndTime,
                Text = translatedSubtitle.Text
            };
            translatedTrack.Segments.Add(srtClip);
        }

        videoProject.TranslatedSubtitlePath = translatedSubtitlePath;

        s.ProgressService?.ResetProgress();
        return translatedTrack;
    }

    /// <summary>
    /// 保存翻译后的字幕到文件
    /// </summary>
    private async Task SaveTranslatedSubtitlesAsync(IEnumerable<ISrtSubtitle> subtitles, string filePath)
    {
        var subtitleService = Session.ServiceProvider.GetRequiredService<ISubtitleService>();
        await subtitleService.SaveSrtAsync(subtitles.ToList(), filePath);
    }

    #endregion

    #region TTS

    /// <summary>
    /// 生成TTS音频
    /// </summary>
    [ContextMenuAction("生成TTS音频", Order = 30, Group = "音频")]
    public async Task<TTSTrackInfo> GenerateTTS()
    {
        return await GenerateTTS(false);
    }


    /// <summary>
    /// 为SRT字幕生成TTS音频
    /// </summary>
    /// <param name="regenerate">是否重新生成已存在的音频</param>
    /// <returns>生成的TTS片段数量</returns>
    public async Task<TTSTrackInfo> GenerateTTS(bool regenerate)
    {

        #region 准备
        IServices s = this;
        var videoProject = VideoProject;
        var subtitles = Segments.OfType<SRTClip>().ToList();
        var totalCount = subtitles.Count();

        if (totalCount == 0)
        {
            throw new InvalidOperationException("没有可生成TTS的字幕片段");
        }

        var total = 0;
        #endregion

        #region 先创建所有AudioClip对象（FilePath为空，显示"待生成"标识）
        var ttsAudios = new AudioSource(Session)
        {
            Name = "TTS目标音频",
            MediaType = MediaType.TTS分段
        };

        videoProject.MediaSources.Add(ttsAudios);

        var ttsTrack = new TTSTrackInfo(Session)
        {
            Media = ttsAudios,
            VideoProject = videoProject,
            TrackType = MediaType.TTS分段,
            RelationSrt = this
        };

        videoProject.Tracks.Add(ttsTrack);
        ttsTrack.Save();

        foreach (var subtitle in subtitles)
        {
            var audioClip = new TTSClip(Session)
            {
                Index = subtitle.Index,
                Start = subtitle.Start,
                End = subtitle.End,
                Text = subtitle.Text,
                VadSrtClip = subtitle
            };
            ttsTrack.Segments.Add(audioClip);
            audioClip.Save();
        }

        videoProject.Save();
        try
        {
            VideoProject.OnTrackChanged?.Invoke(videoProject, MediaType.TTS分段);
        }
        catch (Exception ex)
        {
            s.progress?.Error($"触发 OnTrackChanged 事件时发生错误: {ex.Message}");
        }
        #endregion

        #region 配置TTS输出目录
        var segmentsSourceDir = Path.Combine(videoProject.ProjectPath, "audio_segments");
        var segmentsTargetDir = $@"{videoProject.ProjectPath}\tts_srt_{Guid.NewGuid():N}\";
        if (!Directory.Exists(segmentsTargetDir))
        {
            Directory.CreateDirectory(segmentsTargetDir);
        }
        #endregion

        #region 准备参考音频片断
        //1.找到说话人音频
        var speakerTrack = videoProject.GetVocalsAudioTrack();
        
        if (this.AudioTrackSegment == null)
        {
            this.AudioTrackSegment = videoProject.GetVocalsAudioSegmensTrack();             
        }
        if(this.AudioTrackSegment == null)
        {
            this.AudioTrackSegment = await speakerTrack.SegmentSourceAudioBySrt(this);
        }

        var speakerSegmentTrack = this.AudioTrackSegment;

        string GetReferenceAudioPath(SRTClip subtitle)
        {
            if (!string.IsNullOrEmpty(subtitle.TTSReference?.FilePath))
            {
                return subtitle.TTSReference.FilePath;
            }

            var speakerSegment = speakerSegmentTrack.Segments.OfType<AudioClip>()
                .FirstOrDefault(ac => ac.Index == subtitle.Index);
            if (speakerSegment != null)
            {
                return speakerSegment.FilePath;
            }
            throw new FileNotFoundException($"未找到说话人音频段落，字幕索引: {subtitle.Index}");
        }
        #endregion

        #region 创建TTS命令列表
        //方法1:使用tts reference的音频作为参考
        //方法2:tts reference还没有拆分，使用srt的内容拆分
        var ttsCommands = subtitles.Select(sub => new TTSCommand
        {
            Index = sub.Index,
            Text = sub.Text,
            ReferenceAudio = GetReferenceAudioPath(sub),
            OutputAudio = Path.Combine(segmentsTargetDir, $"tts_{Guid.NewGuid()}_{sub.Index:0000}.wav")
        }).ToList();
        #endregion

        s.progress.SetStatusMessage(string.Join("\n", subtitles.Select(x => $"{x.TTSReference.FilePath}:{x.TTSReference.Duration}")));

        #region 使用支持服务切换的TTS生成方法
        var ttsSegments = await s.ttsService.GenerateTTSAsync(
            ttsCommands,
            onSegmentCompleted: async (current, totalCount, segment) =>
            {
                try
                {
                    var currentTotal = Interlocked.Increment(ref total);
                    var progressValue = totalCount > 0 ? (double)currentTotal / totalCount * 100 : 0;
                    s.progress.Report($"{segment.Index}-{segment.AudioPath}:{segment.TextTarget}");
                    s.progress.ReportProgress(progressValue);
                    s.progress?.Debug($"TTS进度: {currentTotal}/{totalCount} ({progressValue:F0}%)");
                    var subtitle = subtitles.FirstOrDefault(sub => sub.Index == segment.Index);
                    if (subtitle != null)
                    {
                        var audioClip = ttsTrack.Segments.OfType<AudioClip>().FirstOrDefault(ac => ac.Index == segment.Index);
                        if (audioClip != null)
                        {
                            await audioClip.SetAudioFile(segment.AudioPath);
                            audioClip.Save();

                            try
                            {
                                VideoProject.OnTrackChanged?.Invoke(videoProject, MediaType.TTS分段);
                            }
                            catch (Exception ex)
                            {
                                s.progress?.Error($"触发 OnTrackChanged 事件时发生错误: {ex.Message}");
                                s.progress?.Error($"异常类型: {ex.GetType().Name}");
                                s.progress?.Error($"堆栈跟踪: {ex.StackTrace}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    s.progress?.Error($"处理TTS片段 {segment?.Index} 时发生错误: {ex.Message}");
                    s.progress?.Error($"异常类型: {ex.GetType().Name}");
                    s.progress?.Error($"堆栈跟踪: {ex.StackTrace}");
                }
            },
            cleanOld: regenerate,
            limit: -1
        );
        #endregion

        return ttsTrack;
    }
    #endregion

    #region 上下文菜单操作

    /// <summary>
    /// 翻译字幕
    /// </summary>
    [ContextMenuAction("翻译字幕", Order = 10, Group = "翻译")]
    public async Task TranslateSubtitle()
    {
        await TranslateSRTByGoogle(MediaType.目标字幕);
    }

    /// <summary>
    /// 使用LMStudio翻译字幕
    /// </summary>
    [ContextMenuAction("使用LMStudio翻译字幕", Order = 20, Group = "翻译")]
    public async Task TranslateWithLMStudio()
    {
        await TranslateSRTByLMStudio(MediaType.目标字幕);
    }



    /// <summary>
    /// 导出为SRT文件
    /// </summary>
    [ContextMenuAction("导出为SRT文件", Order = 40, Group = "导出")]
    public async Task ExportSrt()
    {
        var videoProject = VideoProject;
        var exportPath = Path.Combine(videoProject.ProjectPath, $"export_{DateTime.Now:yyMMddHHmmssfff}.srt");
        var subtitleService = Session.ServiceProvider.GetRequiredService<ISubtitleService>();
        var subtitles = Segments.OfType<SRTClip>().Select(s => new SrtSubtitle
        {
            Index = s.Index,
            StartTime = s.Start,
            EndTime = s.End,
            Text = s.Text
        }).Cast<ISrtSubtitle>().ToList();
        await subtitleService.SaveSrtAsync(subtitles, exportPath);
    }

    /// <summary>
    /// 导出为SRT文件
    /// </summary>
    [ContextMenuAction("导出为TXT文件", Order = 40, Group = "导出")]
    public async Task ExportTXT()
    {
        var videoProject = VideoProject;
        var exportPath = Path.Combine(videoProject.ProjectPath, $"export_{DateTime.Now:yyMMddHHmmssfff}.txt");
        var txt = string.Join("\n", Segments.OfType<SRTClip>().OrderBy(x=>x.Index).Select(s => s.Text));
        File.WriteAllText(exportPath, txt);
    }

    #endregion

    #region 合并,优化字幕
    const int maxGap = 1000;//毫秒
    const int maxSegmentDuration = 15000;//15秒

    #region 辅助方法

    private static readonly HashSet<char> SentenceEndPunctuation = new HashSet<char>
    {
        '。', '！', '？', '.', '!', '?'
    };

    private bool IsSentenceEnd(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var trimmed = text.TrimEnd();
        return trimmed.Length > 0 && SentenceEndPunctuation.Contains(trimmed[trimmed.Length - 1]);
    }

    #endregion

    /// <summary>
    /// 合并连续的字幕片段
    /// </summary>
    [ContextMenuAction("合并连续字幕", Tooltip = "合并优化字幕")]
    public async Task<SRTTrackInfo> MergeSegment(MediaType targetMediaType)
    {
        #region 获取并排序字幕片段
        var segs = this.Segments.OfType<SRTClip>().OrderBy(x => x.Index).ToList();
        if (segs.Count == 0)
        {
            throw new Exception("此轨道没有段落!");
        }
        #endregion

        #region 合并逻辑
        var mergedSegments = new List<ISrtSubtitle>();
        int currentIndex = 1;

        int i = 0;
        while (i < segs.Count)
        {
            var current = segs[i];
            var mergedStart = current.Start;
            var mergedEnd = current.End;
            var mergedText = current.Text;

            while (i + 1 < segs.Count)
            {
                var next = segs[i + 1];
                var gap = (next.Start - mergedEnd).TotalMilliseconds;

                #region 检查是否可以合并
                bool canMerge = true;

                if (gap >= maxGap)
                {
                    canMerge = false;
                }

                //if (IsSentenceEnd(mergedText))
                //{
                //    canMerge = false;
                //}

                var newDuration = (next.End - mergedStart).TotalMilliseconds;
                if (newDuration > maxSegmentDuration)
                {
                    canMerge = false;
                }
                #endregion

                if (canMerge)
                {
                    mergedText = $"{mergedText} {next.Text}";
                    mergedEnd = next.End;
                    i++;
                }
                else
                {
                    break;
                }
            }

            mergedSegments.Add(new SrtSubtitle(currentIndex++, mergedStart, mergedEnd, mergedText));
            i++;
        }
        #endregion

        #region 保存为SRT文件
        var mergedSubtitlePath = Path.Combine(VideoProject.ProjectPath, $"merged_subtitle_track_{Guid.NewGuid()}.srt");
        var subtitleService = Session.ServiceProvider.GetRequiredService<ISubtitleService>();
        var subtitles = mergedSegments;
        await subtitleService.SaveSrtAsync(subtitles, mergedSubtitlePath);
        #endregion

        return await VideoProject.ImportSrt(mergedSubtitlePath, targetMediaType, this.VadTrack);

    }
    #endregion

    //编辑轨道功能
    //执行后，应该打开一个编辑窗口
    //编辑完成后,再保存回轨道中的信息
    //1.不会改变时间、索引顺序
    //2.改变时间
}

