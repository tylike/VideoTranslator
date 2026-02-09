#nullable enable
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VT.Module.BusinessObjects;

[NavigationItem]
public partial class VideoProject(Session s) : VTBaseObject(s)
{
    static string DefaultProjectPath = "D:\\VideoTranslator\\videoProjects";
    private LMStudioTranslationService? lmStudioTranslationService => Session.ServiceProvider.GetRequiredService<LMStudioTranslationService>();
    [XafDisplayName("项目名称")]
    public string ProjectName
    {
        get { return GetPropertyValue<string>(nameof(ProjectName)); }
        set { SetPropertyValue(nameof(ProjectName), value); }
    }

    [XafDisplayName("项目路径")]
    public string ProjectPath
    {
        get { return GetPropertyValue<string>(nameof(ProjectPath)); }
        set { SetPropertyValue(nameof(ProjectPath), value); }
    }

    [XafDisplayName("媒体源")]
    [Association, Aggregated]
    public XPCollection<MediaSource> MediaSources
    {
        get
        {
            return GetCollection<MediaSource>(nameof(MediaSources));
        }
    }

    [XafDisplayName("YouTube视频")]
    [Association, Aggregated]
    public XPCollection<YouTubeVideo> YouTubeVideos
    {
        get
        {
            return GetCollection<YouTubeVideo>(nameof(YouTubeVideos));
        }
    }


    [XafDisplayName("片段")]
    [Association, Aggregated]
    [Obsolete("此属性将过时，应使用Tracks属性，或其他属性实现相关功能")]
    public XPCollection<TimeLineClip> Clips
    {
        get
        {
            return GetCollection<TimeLineClip>(nameof(Clips));
        }
    }

    [XafDisplayName("当前状态")]
    public string CurrentStatus
    {
        get { return GetPropertyValue<string>(nameof(CurrentStatus)); }
        set { SetPropertyValue(nameof(CurrentStatus), value); }
    }

    public bool HasVideoSource() => MediaSources != null && MediaSources.Count > 0 && MediaSources.Any(ms => ms.MediaType == MediaType.Video);

    public VideoSource GetVideoSource() => MediaSources?.FirstOrDefault(ms => ms.MediaType == MediaType.源视频) as VideoSource;

    #region 语言设置

    [XafDisplayName("源视频语言")]
    public Language SourceLanguage
    {
        get { return GetPropertyValue<Language>(nameof(SourceLanguage)); }
        set { SetPropertyValue(nameof(SourceLanguage), value); }
    }

    [XafDisplayName("目标视频语言")]
    public Language TargetLanguage
    {
        get { return GetPropertyValue<Language>(nameof(TargetLanguage)); }
        set { SetPropertyValue(nameof(TargetLanguage), value); }
    }

    #endregion

    #region 翻译设置
    [XafDisplayName("翻译风格")]
    public TranslationStyle TranslationStyle
    {
        get { return GetPropertyValue<TranslationStyle>(nameof(TranslationStyle)); }
        set
        {
            var oldValue = GetPropertyValue<TranslationStyle>(nameof(TranslationStyle));
            if (oldValue != value)
            {
                SetPropertyValue(nameof(TranslationStyle), value);
                var prompt = lmStudioTranslationService?.GetSystemPromptOnly(value);
                if (!string.IsNullOrEmpty(prompt))
                {
                    CustomSystemPrompt = prompt;
                }
            }
        }
    }

    [XafDisplayName("系统提示词")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "6")]
    public string CustomSystemPrompt
    {
        get { return GetPropertyValue<string>(nameof(CustomSystemPrompt)); }
        set { SetPropertyValue(nameof(CustomSystemPrompt), value); }
    }     
    #endregion
    internal void SyncTimes()
    {
        foreach (var item in Clips)
        {
            item.TargetSRTClip.Start = item.SourceSRTClip.Start;
            item.TargetSRTClip.End = item.SourceSRTClip.End;

            item.SourceAudioClip.Start = item.SourceSRTClip.Start;
            item.SourceAudioClip.End = item.SourceSRTClip.End;

            item.TargetAudioClip.Start = item.SourceSRTClip.Start;

        }
    }

    #region 字幕翻译相关方法

    /// <summary>
    /// 翻译字幕
    /// </summary>
    public async Task TranslateSRT()
    {
        IServices s = this;
        ValidateSourceSubtitle().Validate();
        var translatedSubtitlePath = Path.Combine(ProjectPath, "source_subtitle_zh.srt");

        var sourceSubtitles = Clips.OrderBy(x => x.Index).Select(x => x.SourceSRTClip.ToSubtitle()).ToList();
        var totalSubtitles = sourceSubtitles.Count;

        var translatedSubtitles = await s.TranslationService.TranslateSubtitlesAsync(
            sourceSubtitles,
            TranslationApi.LMStudio,
            CustomSystemPrompt,
            null,
            TargetLanguage,
            SourceLanguage);

        await s.SubtitleService.SaveSrtAsync(translatedSubtitles, translatedSubtitlePath);

        TranslatedSubtitlePath = translatedSubtitlePath;

        var updatedCount = UpdateAllClipsWithTranslation(translatedSubtitles);
        s.ObjectSpace.CommitChanges();

    }

    /// <summary>
    /// 应用已翻译的字幕文件
    /// </summary>
    public async Task ApplyTranslatedSRT()
    {
        IServices s = this;
        ValidateTranslatedSubtitle().Validate();
        var translatedSubtitlePath = TranslatedSubtitlePath;

        var translatedSubtitles = await s.SubtitleService.ParseSrtAsync(translatedSubtitlePath);
        var totalSubtitles = translatedSubtitles.Count;
        var clips = Clips.ToList();
        var updatedCount = 0;

        foreach (var subtitle in translatedSubtitles)
        {
            var clip = clips.FirstOrDefault(x => x.SourceSRTClip.Index == subtitle.Index);
            if (clip != null)
            {
                clip.TargetSRTClip.Index = subtitle.Index;
                clip.TargetSRTClip.Start = TimeSpan.FromSeconds(subtitle.StartSeconds);
                clip.TargetSRTClip.End = TimeSpan.FromSeconds(subtitle.EndSeconds);
                clip.TargetSRTClip.Text = subtitle.Text;
                updatedCount++;
            }
        }

        s.ObjectSpace.CommitChanges();

    }

    /// <summary>
    /// 保存字幕文件
    /// </summary>
    public void SaveSRT()
    {
        IServices s = this;
        if (SourceSubtitlePath == null)
        {
            SourceSubtitlePath = Path.Combine(
                ProjectPath,
                "subtitle.srt"
                );
        }
        s.SubtitleService.SaveSrtAsync(
            Clips.OrderBy(x => x.Index).Select(x => x.SourceSRTClip.ToSubtitle()).ToList(),
            SourceSubtitlePath
            );

        if (TranslatedSubtitlePath == null)
        {
            TranslatedSubtitlePath = Path.Combine(
                ProjectPath,
                "subtitle_target.srt"
                );
        }
        SyncTimes();

        s.SubtitleService.SaveSrtAsync(
            Clips.OrderBy(x => x.Index).Select(x => x.TargetSRTClip.ToSubtitle()).ToList(),
            TranslatedSubtitlePath
            );
    }

    /// <summary>
    /// 更新所有片段的翻译内容
    /// </summary>
    /// <param name="translatedSubtitles">翻译后的字幕列表</param>
    /// <returns>更新的片段数量</returns>
    private int UpdateAllClipsWithTranslation(List<ISrtSubtitle> translatedSubtitles)
    {
        var updatedCount = 0;
        foreach (var clip in Clips)
        {
            if (clip.SourceSRTClip != null)
            {
                var subtitle = translatedSubtitles.FirstOrDefault(s => s.Index == clip.SourceSRTClip.Index);
                if (subtitle != null)
                {
                    var targetSRTClip = clip.TargetSRTClip;
                    targetSRTClip.Index = subtitle.Index;
                    targetSRTClip.Start = TimeSpan.FromSeconds(subtitle.StartSeconds);
                    targetSRTClip.End = TimeSpan.FromSeconds(subtitle.EndSeconds);
                    targetSRTClip.Text = subtitle.Text;
                    updatedCount++;
                }
            }
        }
        return updatedCount;
    }

    #endregion

    #region TTS相关方法
    /// <summary>
    /// 从文件恢复TTS音频
    /// </summary>
    /// <returns>恢复的TTS片段数量</returns>
    public async Task<int> RestoreTTSFromFile()
    {
        IServices s = this;
        var path = $@"{ProjectPath}\tts\";

        if (!Directory.Exists(path))
        {
            throw new Exception($"TTS 目录不存在: {path}");
        }

        var files = Directory.GetFiles(path, "*.wav");
        var successCount = 0;
        var skipCount = 0;
        var errorCount = 0;

        foreach (var file in files)
        {
            try
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(file);

                if (!int.TryParse(nameWithoutExt, out var index))
                {
                    s.progress.Report($"跳过文件（文件名不是数字）: {Path.GetFileName(file)}");
                    errorCount++;
                    continue;
                }

                var clip = Clips.SingleOrDefault(x => x.Index == index);
                if (clip == null)
                {
                    s.progress.Report($"跳过文件（找不到对应的 Clip）: {Path.GetFileName(file)} (Index: {index})");
                    skipCount++;
                    continue;
                }

                if (!string.IsNullOrEmpty(clip.TargetAudioClip.FilePath))
                {
                    s.progress.Report($"跳过 Clip（已有音频文件）: Index {index}");
                    skipCount++;
                    continue;
                }

                await clip.TargetAudioClip.SetAudioFile(file);
                successCount++;
                s.progress.Report($"恢复成功: {Path.GetFileName(file)} -> Clip {index}");
            }
            catch (Exception ex)
            {
                s.progress.Error($"恢复文件失败: {Path.GetFileName(file)}, 错误: {ex.Message}");
                errorCount++;
            }
        }

        return successCount;
    }
       

    #endregion

    public async Task<SRTTrackInfo> ImportSrt(string srtFile,MediaType type,VadTrackInfo vad)
    {
        var rst = await this.CreateSubtitleSource(srtFile,type,vad);
        rst.track.Title = Path.GetFileNameWithoutExtension( srtFile);
        return rst.track;
    }
}
