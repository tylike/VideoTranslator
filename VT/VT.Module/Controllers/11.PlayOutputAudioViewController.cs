using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using VideoTranslator;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class PlayViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public PlayViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public PlayViewController()
    {
        var playOutputAudio = new AsyncSimpleAction(this, "PlayOutputAudio", null,false);
        playOutputAudio.Caption = "播放合成音频";
        playOutputAudio.Execute += PlayOutputAudio_Execute;

        var playOutputVideo = new AsyncSimpleAction(this, "PlayOutputVideo", null,false);
        playOutputVideo.Caption = "播放合成视频";
        playOutputVideo.Execute += PlayOutputVideo_Execute;
    }

    private void PlayOutputAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        PlayOutputAudio(this);
    }

    public static void PlayOutputAudio(VideoProjectController self)
    {
        var videoProject = self.GetCurrentVideoProject();

        videoProject.OutputAudioPath.ValidateFileExists( $"合成音频文件不存在: {videoProject.OutputAudioPath}");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = videoProject.OutputAudioPath,
                UseShellExecute = true
            });

            self.Application.ShowViewStrategy.ShowMessage($"正在播放合成音频: {videoProject.OutputAudioPath}");   
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException($"播放音频失败: {ex.Message}");
        }
    }

    private void PlayOutputVideo_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        PlayOutputVideo(this);
    }

    public static void PlayOutputVideo(VideoProjectController self)
    {
        var videoProject = self.GetCurrentVideoProject();

        videoProject.OutputVideoPath.ValidateFileExists($"合成视频文件不存在: {videoProject.OutputVideoPath}");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = videoProject.OutputVideoPath,
                UseShellExecute = true
            });

            self.Application.ShowViewStrategy.ShowMessage($"正在播放合成视频: {videoProject.OutputVideoPath}");
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException($"播放视频失败: {ex.Message}");
        }
    }
}
