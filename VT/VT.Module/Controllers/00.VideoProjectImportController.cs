using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class VideoProjectImportController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public VideoProjectImportController(IServiceProvider serviceProvider) : this()
        => ServiceProvider = serviceProvider;
    
    protected void CreateActions()
    {
        var importVideo = new SimpleAction(this, "ImportVideoWin", null);
        importVideo.Caption = "0.导入视频";
        importVideo.ToolTip = "从本地选择视频文件导入";
        importVideo.Execute += ImportVideo_Execute;

        var importYouTube = new SimpleAction(this, "ImportYouTubeVideo", null);
        importYouTube.Caption = "0.1导入YouTube视频";
        importYouTube.ToolTip = "从YouTube下载视频并导入";
        importYouTube.Execute += ImportYouTubeVideo_Execute;
    }
    public VideoProjectImportController() : base() { CreateActions(); }

    private void ImportVideo_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var videoFilePath = self.FileDialogService?.OpenVideoFile();
        if (string.IsNullOrEmpty(videoFilePath))
        {
            return;
        }

        try
        {
            VideoProject project = null;
            if (View is DetailView)
            {
                project = ViewCurrentObject;
            }
            else
            {
                project = ObjectSpace.CreateObject<VideoProject>();
            }
            ObjectSpace.CommitChanges();
            project.Create();
            project.ImportVideoFile(videoFilePath);
            ObjectSpace.CommitChanges();

            ShowMessage($"视频导入成功: {Path.GetFileName(videoFilePath)}", InformationType.Success);
        }
        catch (Exception ex)
        {
            ShowMessage($"导入视频失败: {ex.Message}", InformationType.Error);
        }
    }

    private async void ImportYouTubeVideo_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var videoProject = GetVideoProject();
        if (videoProject == null) return;
        ObjectSpace.CommitChanges();
        videoProject.Create();
        ObjectSpace.CommitChanges();
        try
        {
            var selection = await self.YouTubeInputDialogService!.ShowUrlInputDialogAsync();
            if (selection == null || string.IsNullOrWhiteSpace(selection.Url))
            {
                ShowMessage("请输入有效的YouTube视频地址", InformationType.Warning);
                return;
            }

            VideoTranslator.Models.YouTubeVideo videoInfo;

            if (selection.VideoInfo != null)
            {
                videoInfo = selection.VideoInfo;
            }
            else
            {
                self.ProgressService?.ShowProgress(marquee: true);
                self.ProgressService?.SetStatusMessage("正在获取YouTube视频信息...");

                videoInfo = await self.YouTubeService!.GetVideoInfoAsync(selection.Url);
            }

            var youtubeVideo = ObjectSpace.CreateObject<BusinessObjects.YouTubeVideo>();
            youtubeVideo.VideoProject = videoProject;
            youtubeVideo.VideoId = videoInfo.VideoId;
            youtubeVideo.DownloadUrl = selection.Url;
            youtubeVideo.Title = videoInfo.Title;
            youtubeVideo.Description = videoInfo.Description;
            youtubeVideo.ThumbnailUrl = videoInfo.ThumbnailUrl;
            youtubeVideo.Uploader = videoInfo.Uploader;
            youtubeVideo.UploadDate = videoInfo.UploadDate;
            youtubeVideo.Duration = (int)videoInfo.Duration.TotalSeconds;
            youtubeVideo.DurationText = videoInfo.DurationText;
            youtubeVideo.ViewCount = videoInfo.ViewCount;
            youtubeVideo.LikeCount = videoInfo.LikeCount;
            youtubeVideo.CommentCount = videoInfo.CommentCount;
            youtubeVideo.Category = videoInfo.Category;
            youtubeVideo.AgeLimit = videoInfo.AgeLimit;
            youtubeVideo.IsLive = videoInfo.IsLive;
            youtubeVideo.Is4K = videoInfo.Is4K;
            youtubeVideo.Resolution = videoInfo.Resolution;
            youtubeVideo.Format = videoInfo.Format;
            youtubeVideo.FileSize = videoInfo.FileSize;
            youtubeVideo.FileSizeText = videoInfo.FileSizeText;
            youtubeVideo.DownloadStatus = YouTubeDownloadStatus.NotDownloaded;

            videoProject.ProjectName = videoInfo.Title;

            ObjectSpace.CommitChanges();

            ShowMessage($"已获取视频信息: {videoInfo.Title}", InformationType.Info);

            await DownloadYouTubeVideoAsync(videoProject, youtubeVideo, selection);
        }
        catch (Exception ex)
        {
            SetProjectError(videoProject, ex.Message);
            ShowMessage($"获取YouTube视频信息失败: {ex.Message}", InformationType.Error);
            self.ProgressService?.HideProgress();
            throw;
        }
    }

    private async Task DownloadYouTubeVideoAsync(VideoProject project, BusinessObjects.YouTubeVideo youtubeVideo, YouTubeDownloadSelection selection)
    {
        var ProgressService = self.ProgressService;
        var YouTubeService = self.YouTubeService;
        try
        {
            if (selection.DownloadVideo)
            {
                ProgressService?.ShowProgress();
                ProgressService?.SetStatusMessage("正在下载YouTube视频...");
                youtubeVideo.DownloadStatus = YouTubeDownloadStatus.Downloading;
                ObjectSpace.CommitChanges();

                var videoFileName = $"{youtubeVideo.VideoId}.mp4";
                var outputPath = Path.Combine(project.ProjectPath, videoFileName);

                await YouTubeService!.DownloadVideoAsync(selection.Url, selection.SelectedVideoStream, selection.SelectedAudioStream, outputPath);

                youtubeVideo.MarkAsCompleted(outputPath);
                project.ImportVideoFile(outputPath);
                ObjectSpace.CommitChanges();

                ShowMessage(
                    $"YouTube视频下载成功: {youtubeVideo.Title}\n文件大小: {youtubeVideo.GetFormattedFileSize()}",
                    InformationType.Success);
            }

            if (selection.DownloadAudio && selection.SelectedAudioStream != null)
            {
                ProgressService?.ShowProgress(marquee: true);
                ProgressService?.SetStatusMessage("正在下载YouTube音频...");
                var audioFileName = $"{youtubeVideo.VideoId}.wav";
                var audioPath = Path.Combine(project.ProjectPath, audioFileName);

                await YouTubeService!.DownloadAudioAsync(selection.Url, audioPath);

                ShowMessage($"YouTube音频下载成功", InformationType.Success);
            }

            foreach (var langCode in selection.SelectedSubtitleLanguages)
            {
                ProgressService?.ShowProgress(marquee: true);
                ProgressService?.SetStatusMessage($"正在下载字幕: {langCode}...");
                var subtitleFileName = $"{youtubeVideo.VideoId}_{langCode}.srt";
                var subtitlePath = Path.Combine(project.ProjectPath, subtitleFileName);

                await YouTubeService!.DownloadSubtitleAsync(selection.Url, langCode, subtitlePath);

                ShowMessage($"字幕下载成功: {langCode}", InformationType.Success);
            }

            ProgressService?.ResetProgress();
        }
        catch (Exception ex)
        {
            youtubeVideo.MarkAsFailed(ex.Message);
            ObjectSpace.CommitChanges();
            ShowMessage($"下载YouTube视频失败: {ex.Message}", InformationType.Error);
            ProgressService?.HideProgress();
            throw;
        }
    }
}
