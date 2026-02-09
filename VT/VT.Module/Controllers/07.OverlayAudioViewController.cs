using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class OverlayAudioViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public OverlayAudioViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public OverlayAudioViewController()
    {
        var overlayAudio = new AsyncSimpleAction(this, "OverlayAudio", null);
        overlayAudio.Caption = "7.合并音频";
        overlayAudio.AsyncExecute += OverlayAudio_Execute;
    }

    private async Task OverlayAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await OverlayAudio(this);
    }

    public static async Task OverlayAudio(IServices self)
    {
        var videoProject = self.GetCurrentVideoProject();

        videoProject.ValidateSourceAudio();       

        var outputPath = Path.Combine(videoProject.ProjectPath, "merged_audio.wav");       

        var audioInfo = await self.AudioService.GetAudioInfoAsync(videoProject.SourceAudioPath);

        var audioClipsWithTime = videoProject.Clips
            .Where(c => c.AdjustedTargetAudioClip != null &&
                        !string.IsNullOrEmpty(c.AdjustedTargetAudioClip.FilePath) &&
                        File.Exists(c.AdjustedTargetAudioClip.FilePath))
            .Select(c => new AudioClipWithTime
            {
                Index = c.Index,
                FilePath = c.AdjustedTargetAudioClip.FilePath,
                Start = c.SourceSRTClip.Start,
                End = c.SourceSRTClip.End,
                AudioDurationMs = c.AdjustedTargetAudioClip.Duration * 1000
            })
            .ToList();

        var backgroundAudioPath = videoProject.SourceBackgroundAudioPath;
        if (string.IsNullOrEmpty(backgroundAudioPath) || !File.Exists(backgroundAudioPath))
        {
            backgroundAudioPath = videoProject.SourceAudioPath;            
        }

        if (audioClipsWithTime.Count == 0)
        {
            throw new UserFriendlyException("没有可合并的调整后音频片段，请先执行'7.调整音频片段'");
        }

        await self.AudioService.MergeAudioSegmentsOnTimelineAsync(
            audioClipsWithTime,
            backgroundAudioPath,
            outputPath);

        videoProject.OutputAudioPath = outputPath;
        self.ObjectSpace.CommitChanges();

    }
}
