using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VT.Module.BusinessObjects;

public class TimeLineClipViewController : ObjectViewController<ObjectView, TimeLineClip>
{
    private SoundPlayer soundPlayer;
    IServiceProvider ServiceProvider { get; set; }
    ITTSService ttsService => ServiceProvider.GetRequiredService<ITTSService>();
    IAudioService audioService => ServiceProvider.GetRequiredService<IAudioService>();
    ISubtitleService subtitleService => ServiceProvider.GetRequiredService<ISubtitleService>();
    ITranslationService translationService => ServiceProvider.GetRequiredService<ITranslationService>();

    public TimeLineClipViewController()
    {
        var playSourceAudio = new SimpleAction(this, "PlaySourceAudio", null);
        playSourceAudio.Caption = "播放原始音频";
        playSourceAudio.Execute += PlaySourceAudio_Execute;

        var playTargetAudio = new SimpleAction(this, "PlayTargetAudio", null);
        playTargetAudio.Caption = "播放目标音频";
        playTargetAudio.Execute += PlayTargetAudio_Execute;

        var playAdjustedTargetAudio = new SimpleAction(this, "PlayAdjustedTargetAudio", null);
        playAdjustedTargetAudio.Caption = "播放调整后音频";
        playAdjustedTargetAudio.Execute += PlayAdjustedTargetAudio_Execute;

        var stopAudio = new SimpleAction(this, "StopAudio", null);
        stopAudio.Caption = "停止播放";
        stopAudio.Execute += StopAudio_Execute;

        var generateTTS = new SimpleAction(this, "GenerateTTSForSegment", null);
        generateTTS.Caption = "TTS";
        generateTTS.Execute += GenerateTTSForSegment_Execute;

        var adjustAudio = new SimpleAction(this, "AdjustAudioForSegment", null);
        adjustAudio.Caption = "调整音频片段";
        adjustAudio.Execute += AdjustAudioForSegment_Execute;

        var translateSubtitle = new SimpleAction(this, "TranslateSubtitle", null);
        translateSubtitle.Caption = "翻译";
        translateSubtitle.Execute += TranslateSubtitle_Execute;

        var computesSourceAudioDuration = new SimpleAction(this, "TimeClip_ComputeSourceAudioDuration", null);
        computesSourceAudioDuration.Caption = "计算原始音频时长";
        computesSourceAudioDuration.Execute += ComputesSourceAudioDuration_Execute;
    }

    private async void ComputesSourceAudioDuration_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        foreach (TimeLineClip item in e.SelectedObjects)
        {
            await item.SourceAudioClip.ComputeDuration();
        }
        ObjectSpace.CommitChanges();
    }

    [ActivatorUtilitiesConstructor]
    public TimeLineClipViewController(IServiceProvider serviceProvider = null) : this()
    {
        ServiceProvider = serviceProvider;
    }

    private async void PlaySourceAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await ExecuteAudioActionAsync(sender, "原始音频", timeLineClip => timeLineClip.SourceAudioClip?.FilePath);
    }

    private async void PlayTargetAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await ExecuteAudioActionAsync(sender, "目标音频", timeLineClip => timeLineClip.TargetAudioClip?.FilePath);
    }

    private async void PlayAdjustedTargetAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await ExecuteAudioActionAsync(sender, "调整后音频", timeLineClip => timeLineClip.AdjustedTargetAudioClip?.FilePath);
    }

    private async Task ExecuteAudioActionAsync(object sender, string audioType, Func<TimeLineClip, string> getAudioPath)
    {
        var action = sender as SimpleAction;
        if (action == null) return;

        var timeLineClip = GetCurrentTimeLineClip();
        var audioPath = getAudioPath(timeLineClip);
        if (string.IsNullOrEmpty(audioPath))
        {
            throw new UserFriendlyException($"该片段没有{audioType}");
        }

        try
        {
            action.Enabled["Processing"] = false;
            PlayAudio(audioPath, audioType);
        }
        finally
        {
            action.Enabled["Processing"] = true;
        }
    }

    private TimeLineClip GetCurrentTimeLineClip()
    {
        var timeLineClip = View.CurrentObject as TimeLineClip;
        if (timeLineClip == null)
        {
            throw new UserFriendlyException("未选择时间线片段");
        }
        return timeLineClip;
    }

    private void PlayAudio(string filePath, string audioType)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new UserFriendlyException($"{audioType}文件路径为空");
        }

        if (!File.Exists(filePath))
        {
            throw new UserFriendlyException($"{audioType}文件不存在: {filePath}");
        }

        try
        {
            StopPlayback();

            soundPlayer = new SoundPlayer(filePath);
            soundPlayer.Play();

            Application.ShowViewStrategy.ShowMessage($"正在播放{audioType}: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException($"播放{audioType}失败: {ex.Message}");
        }
    }

    private void StopAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        StopPlayback();
        Application.ShowViewStrategy.ShowMessage("已停止播放");
    }

    private void StopPlayback()
    {
        if (soundPlayer != null)
        {
            soundPlayer.Stop();
            soundPlayer.Dispose();
            soundPlayer = null;
        }
    }

    protected override void OnDeactivated()
    {
        StopPlayback();
        base.OnDeactivated();
    }

    private async void GenerateTTSForSegment_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        //var action = sender as SimpleAction;
        //if (action == null) return;

        //var timeLineClip = GetCurrentTimeLineClip();

        //if (timeLineClip.SourceAudioClip == null)
        //{
        //    throw new UserFriendlyException("该片段没有原始音频");
        //}

        //if (timeLineClip.TargetSRTClip == null)
        //{
        //    throw new UserFriendlyException("该片段没有目标字幕，请先翻译字幕");
        //}

        //if (string.IsNullOrEmpty(timeLineClip.TargetSRTClip.Text))
        //{
        //    throw new UserFriendlyException("目标字幕文本为空");
        //}

        //await ExecuteActionAsync(action, timeLineClip, "开始生成翻译音频", async () =>
        //{
        //    Application.ShowViewStrategy.ShowMessage("正在生成翻译音频...");

        //    var sourceAudioPath = timeLineClip.SourceAudioClip.FilePath;
        //    var targetText = timeLineClip.TargetSRTClip.Text;

        //    var outputDirectory = Path.GetDirectoryName(sourceAudioPath);
        //    var outputFileName = $"segment_{timeLineClip.Index:D4}_target.wav";
        //    var outputPath = Path.Combine(outputDirectory ?? "", outputFileName);

        //    UpdateProgressAndCommit("正在调用 TTS 服务...");

        //    var generatedAudioPath = await ttsService.GenerateTTSForSingleSegmentAsync(
        //        targetText,
        //        sourceAudioPath,
        //        outputPath,
        //        "http://192.168.1.154:8000/generate",
        //        "0");

        //    await timeLineClip.TargetAudioClip.SetAudioFile(generatedAudioPath);
        //    timeLineClip.SetCompleted("翻译音频生成成功");
        //    timeLineClip.LastOutput = $"生成文件: {outputFileName}";
        //    timeLineClip.Save();

        //    Application.ShowViewStrategy.ShowMessage($"翻译音频生成成功: {outputFileName}");
        //});
    }

    private async Task ExecuteActionAsync(SimpleAction action, TimeLineClip timeLineClip, string startMessage, Func<System.Threading.Tasks.Task> actionFunc)
    {
        try
        {
            action.Enabled["Processing"] = false;
            timeLineClip.SetProcessing(startMessage);
            timeLineClip.Save();

            await actionFunc();
        }
        catch (Exception ex)
        {
            timeLineClip.SetError(ex.Message);
            timeLineClip.Save();
            throw new UserFriendlyException($"操作失败: {ex.Message}");
        }
        finally
        {
            action.Enabled["Processing"] = true;
        }
    }

    private void UpdateProgressAndCommit(string message)
    {
        var timeLineClip = View.CurrentObject as TimeLineClip;
        if (timeLineClip != null)
        {
            timeLineClip.ProgressMessage = message;
            timeLineClip.Save();
        }
    }

    private async void AdjustAudioForSegment_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var action = sender as SimpleAction;
        if (action == null) return;

        var timeLineClip = GetCurrentTimeLineClip();

        if (timeLineClip.TargetAudioClip == null)
        {
            throw new UserFriendlyException("该片段没有目标音频，请先生成翻译音频");
        }

        if (timeLineClip.SourceSRTClip == null)
        {
            throw new UserFriendlyException("该片段没有源字幕");
        }

        if (timeLineClip.VideoProject == null)
        {
            throw new UserFriendlyException("该片段没有关联的视频项目");
        }

        await ExecuteActionAsync(action, timeLineClip, "开始调整音频片段", async () =>
        {
            var videoProject = timeLineClip.VideoProject;

            if (string.IsNullOrEmpty(videoProject.SourceAudioPath) || !File.Exists(videoProject.SourceAudioPath))
            {
                throw new UserFriendlyException("视频项目的源音频文件不存在");
            }

            var adjustedDir = Path.Combine(videoProject.ProjectPath, "adjusted_segments");
            Directory.CreateDirectory(adjustedDir);

            UpdateProgressAndCommit("正在获取源音频信息...");

            var audioInfo = await audioService.GetAudioInfoAsync(videoProject.SourceAudioPath);

            UpdateProgressAndCommit("正在调整音频...");

            await timeLineClip.Adjust(adjustedDir, audioInfo);

            timeLineClip.Save();

            Application.ShowViewStrategy.ShowMessage($"音频调整成功: {Path.GetFileName(timeLineClip.AdjustedTargetAudioClip?.FilePath ?? "")}");
        });
    }

    private async void TranslateSubtitle_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var action = sender as SimpleAction;
        if (action == null) return;

        var selectedClips = e.SelectedObjects.OfType<TimeLineClip>().ToList();
        if (selectedClips.Count == 0)
        {
            throw new UserFriendlyException("未选择任何片段");
        }

        var firstClip = selectedClips[0];
        if (firstClip.SourceSRTClip == null)
        {
            throw new UserFriendlyException("选中的片段没有源字幕");
        }

        if (firstClip.VideoProject == null)
        {
            throw new UserFriendlyException("选中的片段没有关联的视频项目");
        }

        var videoProject = firstClip.VideoProject;

        await ExecuteActionAsync(action, firstClip, "开始翻译字幕", async () =>
        {
            UpdateProgressAndCommit("正在准备翻译...");

            var subtitles = selectedClips.Select(x => x.SourceSRTClip.ToSubtitle()).ToList();
            for (int i = 0; i < subtitles.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(subtitles[i].Text))
                {
                    throw new UserFriendlyException($"第{i + 1}条源字幕文本为空");
                }
            }

            UpdateProgressAndCommit($"正在翻译 {subtitles.Count} 条字幕...");

            var systemPrompt = !string.IsNullOrEmpty(videoProject.CustomSystemPrompt)
                ? videoProject.CustomSystemPrompt
                : null;

            var translatedSubtitles = await translationService.TranslateSubtitlesAsync(
                subtitles,
                TranslationApi.LMStudio,
                systemPrompt,
                null,
                videoProject.TargetLanguage,
                videoProject.SourceLanguage);

            if (translatedSubtitles == null || translatedSubtitles.Count == 0)
            {
                throw new UserFriendlyException("翻译服务返回空结果");
            }

            if (translatedSubtitles.Count != selectedClips.Count)
            {
                throw new UserFriendlyException($"翻译结果数量不匹配: 期望{selectedClips.Count}条, 实际{translatedSubtitles.Count}条");
            }

            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < selectedClips.Count; i++)
            {
                var clip = selectedClips[i];
                var translated = translatedSubtitles[i];

                if (string.IsNullOrWhiteSpace(translated.Text))
                {
                    clip.SetError($"第{i + 1}条翻译结果为空");
                    failCount++;
                    continue;
                }

                if (translated.Text.StartsWith("[翻译失败"))
                {
                    clip.SetError($"第{i + 1}条{translated.Text}");
                    failCount++;
                    continue;
                }

                var targetSrtClip = clip.TargetSRTClip;
                targetSrtClip.Index = clip.SourceSRTClip.Index;
                targetSrtClip.Start = clip.SourceSRTClip.Start;
                targetSrtClip.End = clip.SourceSRTClip.End;                
                targetSrtClip.Text = translated.Text;
                clip.SetCompleted("字幕翻译成功");
                successCount++;
            }

            ObjectSpace.CommitChanges();

            var message = $"翻译完成: 成功{successCount}条";
            if (failCount > 0)
            {
                message += $", 失败{failCount}条";
            }
            Application.ShowViewStrategy.ShowMessage(message);
        });
    }
}
