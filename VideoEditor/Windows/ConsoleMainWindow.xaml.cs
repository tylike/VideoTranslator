using System.Windows;
using VT.Module.BusinessObjects;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Serilog;
using WpfWindow = System.Windows.Window;
using System.IO;
using System.Threading;
using VT.Module;
using VideoTranslator.Models;

namespace VideoEditor.Windows;

public partial class ConsoleMainWindow : WpfWindow
{
    #region 字段

    private readonly ILogger _logger = Log.ForContext<ConsoleMainWindow>();
    private readonly IObjectSpace _objectSpace;
    private readonly IServiceProvider _serviceProvider;

    #endregion

    #region 属性

    public VideoProject? SelectedProject { get; private set; }

    #endregion

    #region 构造函数

    public ConsoleMainWindow(IObjectSpace objectSpace, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _objectSpace = objectSpace ?? throw new ArgumentNullException(nameof(objectSpace));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger.Information("ConsoleMainWindow 初始化完成");
    }

    #endregion

    #region 事件处理

    private void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("点击新建项目按钮");

            var newProjectWindow = new NewProjectWindow(_objectSpace);
            if (newProjectWindow.ShowDialog() == true)
            {
                SelectedProject = newProjectWindow.CreatedProject;
                if (SelectedProject != null)
                {
                    _logger.Information("新建项目成功: {ProjectName} (Oid: {Oid})", SelectedProject.ProjectName, SelectedProject.Oid);
                    DialogResult = true;
                    Close();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "新建项目失败");
            MessageBox.Show($"新建项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void YouTubeImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("点击从YouTube导入按钮");

            var youTubeImportWindow = new YouTubeImportWindow();
            if (youTubeImportWindow.ShowDialog() == true)
            {
                var selection = youTubeImportWindow.GetSelection();
                if (selection != null && !string.IsNullOrWhiteSpace(selection.Url))
                {
                    _logger.Information("从YouTube导入: {Url}", selection.Url);

                    await ImportFromYouTubeAsync(selection);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "从YouTube导入失败");
            MessageBox.Show($"从YouTube导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ImportFromYouTubeAsync(VideoTranslator.Models.YouTubeDownloadSelection selection)
    {
        try
        {
            _logger.Information("开始从YouTube导入视频");

            var videoInfo = selection.VideoInfo;
            if (videoInfo == null)
            {
                _logger.Warning("视频信息为空，尝试获取");
                var youtubeService = new VideoTranslator.Interfaces.YouTubeService();
                videoInfo = await youtubeService.GetVideoInfoAsync(selection.Url);
            }

            _logger.Information("准备创建项目，项目名称: {ProjectName}", videoInfo!.Title);
            var project = _objectSpace.CreateObject<VT.Module.BusinessObjects.VideoProject>();
            project.ProjectName = videoInfo.Title;
            project.SourceLanguage = selection.SourceLanguage;
            project.TargetLanguage = selection.TargetLanguage;

            #region Save YouTube metadata
            var youtubeVideo = new VT.Module.BusinessObjects.YouTubeVideo(project.Session)
            {
                VideoId = videoInfo.VideoId,
                DownloadUrl = selection.Url,
                Title = videoInfo.Title,
                Description = videoInfo.Description,
                ThumbnailUrl = videoInfo.ThumbnailUrl,
                Uploader = videoInfo.Uploader,
                UploadDate = videoInfo.UploadDate,
                Duration = videoInfo.DurationSeconds,
                DurationText = videoInfo.DurationText,
                ViewCount = videoInfo.ViewCount,
                LikeCount = videoInfo.LikeCount,
                CommentCount = videoInfo.CommentCount,
                Tags = videoInfo.Tags,
                Category = videoInfo.Category,
                AgeLimit = videoInfo.AgeLimit,
                IsLive = videoInfo.IsLive,
                Is4K = videoInfo.Is4K,
                Resolution = videoInfo.Resolution,
                Format = videoInfo.Format,
                FileSize = videoInfo.FileSize,
                FileSizeText = videoInfo.FileSizeText,
                DownloadStatus = VT.Module.BusinessObjects.YouTubeDownloadStatus.NotDownloaded
            };
            project.YouTubeVideos.Add(youtubeVideo);
            #endregion

            _objectSpace.CommitChanges();
            _logger.Information("项目已保存到数据库，Oid: {Oid}", project.Oid);

            project.Create();
            _logger.Information("项目目录已创建: {ProjectPath}", project.ProjectPath);

            await DownloadYouTubeVideoAsync(project, selection);

            SelectedProject = project;
            _logger.Information("YouTube视频导入成功: {ProjectName}", project.ProjectName);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "从YouTube导入视频时发生错误");
            throw;
        }
    }

    private async Task DownloadYouTubeVideoAsync(VT.Module.BusinessObjects.VideoProject project, VideoTranslator.Models.YouTubeDownloadSelection selection)
    {
        try
        {            
            var youtubeService = new VideoTranslator.Interfaces.YouTubeService();
            var safeProjectName = SanitizeFileName(project.ProjectName);
            var cancellationToken = CancellationToken.None;

            if (selection.DownloadVideo && selection.SelectedVideoStream != null)
            {
                _logger.Information("开始下载YouTube视频: URL={Url}, 视频质量={Quality}, 音频质量={AudioQuality}",
                    selection.Url, selection.SelectedVideoStream.QualityLabel,
                    selection.SelectedAudioStream?.Bitrate);

                var videoFileName = $"{Path.GetFileNameWithoutExtension(safeProjectName)}.mp4";
                var outputPath = Path.Combine(project.ProjectPath, videoFileName);

                _logger.Information("视频输出路径: {OutputPath}", outputPath);
                await youtubeService.DownloadVideoAsync(selection.Url, selection.SelectedVideoStream, selection.SelectedAudioStream, outputPath, cancellationToken);

                _logger.Information("开始导入视频到项目");
                project.ImportVideoFile(outputPath);
                _objectSpace.CommitChanges();

                _logger.Information("YouTube视频下载成功: {OutputPath}", outputPath);
            }
            else
            {
                _logger.Information("跳过视频下载: DownloadVideo={DownloadVideo}, VideoStream={HasVideoStream}",
                    selection.DownloadVideo, selection.SelectedVideoStream != null);
            }

            if (selection.DownloadAudio && selection.SelectedAudioStream != null)
            {
                _logger.Information("开始下载YouTube音频: URL={Url}, 音频质量={Bitrate}kbps",
                    selection.Url, selection.SelectedAudioStream.Bitrate);

                var audioFileName = $"{Path.GetFileNameWithoutExtension(safeProjectName)}.wav";
                var outputPath = Path.Combine(project.ProjectPath, audioFileName);

                _logger.Information("音频输出路径: {OutputPath}", outputPath);
                await youtubeService.DownloadAudioAsync(selection.Url, outputPath, cancellationToken);

                _logger.Information("YouTube音频下载成功: {OutputPath}", outputPath);
            }
            else
            {
                _logger.Information("跳过音频下载: DownloadAudio={DownloadAudio}, AudioStream={HasAudioStream}",
                    selection.DownloadAudio, selection.SelectedAudioStream != null);
            }

            if (selection.SelectedSubtitleLanguages.Count > 0)
            {
                _logger.Information("开始下载YouTube字幕: 字幕数量={Count}", selection.SelectedSubtitleLanguages.Count);

                foreach (var languageCode in selection.SelectedSubtitleLanguages)
                {
                    var subtitleFileName = $"{Path.GetFileNameWithoutExtension(safeProjectName)}_{languageCode}.srt";
                    var outputPath = Path.Combine(project.ProjectPath, subtitleFileName);

                    _logger.Information("字幕输出路径: {OutputPath}", outputPath);
                    await youtubeService.DownloadSubtitleAsync(selection.Url, languageCode, outputPath);

                    _logger.Information("YouTube字幕下载成功: {OutputPath}", outputPath);
                }
            }
            else
            {
                _logger.Information("跳过字幕下载: 字幕数量={Count}", selection.SelectedSubtitleLanguages.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "下载YouTube视频时发生错误");
            throw;
        }
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars));
    }

    private void SelectProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("点击选择项目按钮");

            var projectSelectionWindow = new ProjectSelectionWindow(_objectSpace);
            if (projectSelectionWindow.ShowDialog() == true)
            {
                SelectedProject = projectSelectionWindow.SelectedProject;
                if (SelectedProject != null)
                {
                    _logger.Information("选择项目: {ProjectName} (Oid: {Oid})", SelectedProject.ProjectName, SelectedProject.Oid);
                    DialogResult = true;
                    Close();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "选择项目失败");
            MessageBox.Show($"选择项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("点击退出按钮");
        DialogResult = false;
        Close();
    }

    #endregion
}
