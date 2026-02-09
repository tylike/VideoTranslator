#nullable enable
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Core;

namespace VT.Module.BusinessObjects;

public partial class VideoProject
{
    /// <summary>
    /// 创建项目目录，自动计算可用序号
    /// </summary>
    public void Create()
    {
        ProjectPath = GetAvailableProjectPath();
        if (!Directory.Exists(ProjectPath))
        {
            Directory.CreateDirectory(ProjectPath);
        }
    }

    /// <summary>
    /// 获取可用的项目路径，自动计算序号
    /// </summary>
    /// <returns>可用的项目路径</returns>
    private string GetAvailableProjectPath()
    {
        if (!Directory.Exists(DefaultProjectPath))
        {
            Directory.CreateDirectory(DefaultProjectPath);
        }

        var existingDirectories = Directory.GetDirectories(DefaultProjectPath)
            .Select(d => Path.GetFileName(d))
            .Where(d => int.TryParse(d, out _))
            .Select(int.Parse)
            .OrderByDescending(x => x)
            .ToList();

        var nextNumber = existingDirectories.Count > 0 ? existingDirectories.First() + 1 : 1;

        while (true)
        {
            var candidatePath = Path.Combine(DefaultProjectPath, nextNumber.ToString());
            if (!Directory.Exists(candidatePath))
            {
                return candidatePath;
            }
            nextNumber++;
        }
    }

    public void ImportVideoFile(string videoFullPath)
    {
        var mediaSource = new VideoSource(Session)
        {
            MediaType = MediaType.源视频,
            VideoProject = this
        };

        var videoFileName = Path.GetFileName(videoFullPath);

        var videoCopyPath = Path.Combine(ProjectPath, videoFileName);

        if (string.Equals(videoFullPath, videoCopyPath, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[VideoProject] 源文件和目标文件相同，跳过复制: {videoFullPath}");
            mediaSource.FileFullName = videoCopyPath;
            MediaSources.Add(mediaSource);
            SourceVideoPath = videoCopyPath;
            return;
        }

        if (File.Exists(videoCopyPath))
            File.Delete(videoCopyPath);
        File.Copy(videoFullPath, videoCopyPath, overwrite: true);
        mediaSource.FileFullName = videoCopyPath;
        MediaSources.Add(mediaSource);
        SourceVideoPath = videoCopyPath;
    }

    public void ImportAudioFile(string audioFullPath)
    {
        var audioFileName = Path.GetFileName(audioFullPath);
        var audioCopyPath = Path.Combine(ProjectPath, audioFileName);

        if (string.Equals(audioFullPath, audioCopyPath, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[VideoProject] 源文件和目标文件相同，跳过复制: {audioFullPath}");
            SourceAudioPath = audioCopyPath;
            return;
        }

        if (File.Exists(audioCopyPath))
            File.Delete(audioCopyPath);
        File.Copy(audioFullPath, audioCopyPath, overwrite: true);
        SourceAudioPath = audioCopyPath;
    }

    /// <summary>
    /// 从一个视频创建项目
    /// 视频是包含音频的
    /// </summary>
    /// <param name="videoPath"></param>
    /// <param name="sourceLanguage"></param>
    /// <param name="targetLanguage"></param>
    /// <param name="audioPath">如果提供则使用</param>
    public static async Task<VideoProject> CreateProject(IObjectSpace objectSpace,
        string projectName,
        string videoPath, Language sourceLanguage, Language targetLanguage, string audioPath = null
        )
    {

        var project = objectSpace.CreateObject<VideoProject>();
        project.ProjectName = projectName;
        project.SourceLanguage = sourceLanguage;
        project.TargetLanguage = targetLanguage;
        project.Create();

        var progressService = ServiceHelper.GetMainServiceProvider().GetRequiredService<IProgressService>();
        progressService?.ShowProgress();

        var ffmpeg = ServiceHelper.GetMainServiceProvider().GetRequiredService<IFFmpegService>();

        #region 处理视频和音频

        var 提供了音频 = !string.IsNullOrEmpty(audioPath) && audioPath.ValidateFileExists();
        var 提供了视频 = !string.IsNullOrEmpty(videoPath) && videoPath.ValidateFileExists();

        if (!提供了视频 && !提供了音频)
        {
            throw new Exception("必须提供视频或音频文件!");
        }

        if (提供了视频)
        {
            project.ImportVideoFile(videoPath!);
            var videoInfo = await ffmpeg.GetVideoStreamInfo(videoPath!);

            #region 静音视频和音频的提取

            if (!videoInfo.HasAudio && videoInfo.HasVideo)
            {
                project.SourceMutedVideoPath = project.SourceVideoPath;
            }
            else
            {
                if (!videoInfo.HasVideo)
                {
                    throw new Exception($"提供的视频文件[{videoPath}]中没有视频流!");
                }
                else
                {
                    var mutedVideoPath = project.CombinePath("source_muted_video.mp4");
                    var extractedAudioPath = project.CombinePath("source_audio.wav");
                    await ffmpeg.ExtractVideoStream(videoPath!, mutedVideoPath);
                    await ffmpeg.SeparateMainVideoAndAudio(videoPath!, mutedVideoPath, extractedAudioPath);

                    if (!提供了音频)
                    {
                        audioPath = extractedAudioPath;
                        提供了音频 = true;
                    }
                }
            }

            #endregion
        }

        #endregion

        #region 导入音频

        if (提供了音频)
        {
            progressService?.SetStatusMessage("正在导入音频文件...");
            project.ImportAudioFile(audioPath!);
            await project.CreateAudioSourceAndTrackInfo(MediaType.源音频, true, audioPath);
        }
        else
        {
            throw new Exception("必须提供音频!");
        }

        #endregion

        objectSpace.CommitChanges();
        progressService?.ResetProgress();
        return project;
    }
}
