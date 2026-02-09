using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class MergeVideoViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public MergeVideoViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public MergeVideoViewController()
    {
        var mergeVideo = new AsyncSimpleAction(this, "MergeVideo", null);
        mergeVideo.Caption = "8.合并视频";
        mergeVideo.AsyncExecute += MergeVideo_Execute;
    }

    private async Task MergeVideo_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await MergeVideo(this);
    }

    public static async Task MergeVideo(IServices self)
    {
        var videoProject = self.GetCurrentVideoProject();

        videoProject.SourceMutedVideoPath.ValidateFileExists( $"静音视频文件不存在: {videoProject.SourceMutedVideoPath}");
        videoProject.OutputAudioPath.ValidateFileExists( $"合并音频文件不存在: {videoProject.OutputAudioPath}");

        var outputPath = Path.Combine(videoProject.ProjectPath, "final_video.mp4");


        var videoEncoder = videoProject.GetFFmpegVideoEncoder();
        var preset = videoProject.GetFFmpegPreset();
        var crf = videoProject.GetCRFValue();
        var args = $"-i \"{videoProject.SourceMutedVideoPath}\" -i \"{videoProject.OutputAudioPath}\" -c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -map 0:v:0 -map 1:a:0 -y \"{outputPath}\"";

        await self.FfmpegService.ExecuteCommandAsync(args);

        videoProject.OutputVideoPath = outputPath;
        self.ObjectSpace.CommitChanges();


    }
}
