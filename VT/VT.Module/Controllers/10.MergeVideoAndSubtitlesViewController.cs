using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;
using VT.Module.Services;

namespace VT.Module.Controllers;

public class MergeVideoAndSubtitlesViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public MergeVideoAndSubtitlesViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public MergeVideoAndSubtitlesViewController()
    {
        var mergeVideoAndSubtitles = new AsyncSimpleAction(this, "MergeVideoAndSubtitles", null);
        mergeVideoAndSubtitles.Caption = "10.生成最终视频";
        mergeVideoAndSubtitles.AsyncExecute += MergeVideoAndSubtitles_Execute;

        var publishToBilibili = new AsyncSimpleAction(this, "PublishToBilibili", null);
        publishToBilibili.Caption = "发布到B站";
        publishToBilibili.ImageName = "Action_Publish";
        publishToBilibili.AsyncExecute += PublishToBilibili_Execute;
    }

    private async Task MergeVideoAndSubtitles_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await MergeVideoAndSubtitles(this);
    }

    public static async Task MergeVideoAndSubtitles(IServices self)
    {
        var videoProject = self.GetCurrentVideoProject();

        videoProject.ValidateSourceAudio();

        var inputVideoPath = videoProject.SourceMutedVideoPath;
        var outputPath = Path.Combine(videoProject.ProjectPath, "final_video_with_subtitles.mp4");

        inputVideoPath.ValidateFileExists( $"静音视频文件不存在: {inputVideoPath}");
        videoProject.OutputAudioPath.ValidateFileExists($"合并音频文件不存在: {videoProject.OutputAudioPath}");
        videoProject.TranslatedSubtitlePath.ValidateFileExists($"翻译字幕文件不存在: {videoProject.TranslatedSubtitlePath}");
        
        var escapedChinesePath = videoProject.TranslatedSubtitlePath.EscapeForFFmpeg();
        var escapedVideoPath = inputVideoPath.EscapeForFFmpeg();
        var escapedAudioPath = videoProject.OutputAudioPath.EscapeForFFmpeg();

        var debugSubtitlePath = Path.Combine(videoProject.ProjectPath, "debug_subtitle.srt");
        videoProject.CreateDebugSubtitle(debugSubtitlePath);
        var escapedDebugPath = debugSubtitlePath.EscapeForFFmpeg();

        var videoEncoder = videoProject.GetFFmpegVideoEncoder();
        var hwaccel = videoProject.GetFFmpegHWAccel();
        var hwaccelArgs = string.IsNullOrEmpty(hwaccel) ? "" : $"-hwaccel {hwaccel} ";
        var preset = videoProject.GetFFmpegPreset();
        var crf = videoProject.GetCRFValue();
        var fastSubtitle = videoProject.FastSubtitleRendering;

        var ffmpegArgs = "";
        if (fastSubtitle)
        {
            if (!string.IsNullOrEmpty(videoProject.SourceSubtitlePath) && File.Exists(videoProject.SourceSubtitlePath))
            {
                var escapedEnglishPath = videoProject.SourceSubtitlePath.EscapeForFFmpeg();

                ffmpegArgs = $"-i \"{inputVideoPath}\" -i \"{videoProject.OutputAudioPath}\" " +
                             $"-filter_complex \"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize=16,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=50,Alignment=2'[v1];[v1]subtitles='{escapedEnglishPath}':force_style='Fontsize=14,PrimaryColour=&H00FFFF00,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=5,Alignment=2'[v]\" " +
                             $"-map \"[v]\" -map 1:a -c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -y \"{outputPath}\"";

            }
            else
            {
                ffmpegArgs = $"-i \"{inputVideoPath}\" -i \"{videoProject.OutputAudioPath}\" " +
                             $"-filter_complex \"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize=16,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=50,Alignment=2'[v]\" " +
                             $"-map \"[v]\" -map 1:a -c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -y \"{outputPath}\"";
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(videoProject.SourceSubtitlePath) && File.Exists(videoProject.SourceSubtitlePath))
            {
                var escapedEnglishPath = videoProject.SourceSubtitlePath.EscapeForFFmpeg();

                ffmpegArgs = $"-i \"{inputVideoPath}\" -i \"{videoProject.OutputAudioPath}\" " +
                             $"-filter_complex \"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize=14,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=50,Alignment=2'[v1];[v1]subtitles='{escapedEnglishPath}':force_style='Fontsize=14,PrimaryColour=&H00FFFF00,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=5,Alignment=2'[v2];[v2]subtitles='{escapedDebugPath}':force_style='Fontsize=12,PrimaryColour=&H0000FF00,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=0,Alignment=8'[v]\" " +
                             $"-map \"[v]\" -map 1:a -c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -y \"{outputPath}\"";

            }
            else
            {
                ffmpegArgs = $"-i \"{inputVideoPath}\" -i \"{videoProject.OutputAudioPath}\" " +
                             $"-filter_complex \"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize=14,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=50,Alignment=2'[v1];[v1]subtitles='{escapedDebugPath}':force_style='Fontsize=12,PrimaryColour=&H0000FF00,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=0,Alignment=8'[v]\" " +
                             $"-map \"[v]\" -map 1:a -c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -y \"{outputPath}\"";
            }
        }
                
        await self.FfmpegService.ExecuteCommandAsync(ffmpegArgs);

        videoProject.OutputVideoPath = outputPath;
        self.ObjectSpace.CommitChanges();
    }

    private async Task PublishToBilibili_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await PublishToBilibili(this);
    }

    public static async Task PublishToBilibili(VideoProjectController self)
    {
        var videoProject = self.GetCurrentVideoProject();
        
        // 验证最终视频是否存在
        if (string.IsNullOrEmpty(videoProject.OutputVideoPath) || !File.Exists(videoProject.OutputVideoPath))
        {
            self.Application.ShowViewStrategy.ShowMessage($"最终视频文件不存在，请先生成最终视频: {videoProject.OutputVideoPath}");
            return;
        }

        try
        {
            // 获取B站发布服务
            var publishService = self.Application.ServiceProvider.GetService<IBilibiliPublishService>();
            if (publishService == null)
            {
                self.Application.ShowViewStrategy.ShowMessage("B站发布服务不可用，请检查系统配置。");
                return;
            }

            // 创建发布信息
            var publishInfo = new BilibiliPublishInfo
            {
                VideoFilePath = videoProject.OutputVideoPath,
                Title = videoProject.ProjectName ?? "未命名视频",
                Type = "自制",
                Tags = new List<string> { "视频翻译", "VT" },
                Description = $"使用VideoTranslator制作的视频: {videoProject.ProjectName}",
                IsRepost = false,
                SourceAddress = "",
                EnableOriginalWatermark = false,
                EnableNoRepost = false
            };

            // 执行发布
            var result = await publishService.PublishVideoAsync(publishInfo);
            
            if (result)
            {
                self.Application.ShowViewStrategy.ShowMessage("视频已成功发布到B站！");
            }
            else
            {
                self.Application.ShowViewStrategy.ShowMessage("发布到B站失败，请检查日志信息。");
            }
        }
        catch (Exception ex)
        {
            self.Application.ShowViewStrategy.ShowMessage($"发布到B站时发生错误: {ex.Message}");
        }
    }
}
