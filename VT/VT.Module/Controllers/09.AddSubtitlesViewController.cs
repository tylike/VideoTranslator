using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class AddSubtitlesViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public AddSubtitlesViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public AddSubtitlesViewController()
    {
        var addSubtitles = new AsyncSimpleAction(this, "AddSubtitles", null);
        addSubtitles.Caption = "9.添加字幕";
        addSubtitles.AsyncExecute += AddSubtitles_Execute;
    }

    private async Task AddSubtitles_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await AddSubtitles(this);
    }

    public static async Task AddSubtitles(IServices self)
    {
        var videoProject = self.GetCurrentVideoProject();
        videoProject.OutputVideoPath.ValidateFileExists( "请先合并视频");
        videoProject.TranslatedSubtitlePath.ValidateFileExists("翻译后的字幕文件不存在");

        var inputVideoPath = videoProject.OutputVideoPath;
        var subtitlePath = videoProject.TranslatedSubtitlePath;

        var outputPath = Path.Combine(videoProject.ProjectPath, "video_with_subtitles.mp4");
        var escapedPath = subtitlePath.Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'");
        var args = $"-i \"{inputVideoPath}\" -vf subtitles='{escapedPath}' -c:a copy -y \"{outputPath}\"";
        await self.FfmpegService.ExecuteCommandAsync( args);
        videoProject.OutputVideoPath = outputPath;
    }
}
