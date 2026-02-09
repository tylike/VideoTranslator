using DevExpress.ExpressApp;
using Microsoft.Win32;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using TimeLine.Models;
using TimeLine.Services;
using VideoEditor.Services;
using VideoEditor.ViewModels;
using VideoEditor.Windows;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VT.Module;
using VT.Module.BusinessObjects;
using WpfWindow = System.Windows.Window;

namespace VideoEditor;

public partial class MainWindow : WpfWindow
{
    VideoEditorContext Context { get; set; } = new VideoEditorContext();

    #region 构造函数

    public MainWindow()
    {
        try
        {
            _logger.Information("MainWindow 构造函数开始");
            InitializeComponent();
            InitializeViewModel();
            InitializeServices();
            this.timeLinePanel.Context = this.Context;
            _logger.Information("MainWindow 构造函数完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "MainWindow 构造函数失败");
            MessageBox.Show($"初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    #endregion

    #region 窗体事件

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            _logger.Information("MainWindow 正在关闭，开始导出可视化树");

            #region 取消订阅事件

            if (_currentProject != null)
            {
                _currentProject.Changed -= OnObjectChanged;
                _logger.Information("已取消订阅 VideoProject 对象变更事件");
            }

            #endregion

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logFilePath = Path.Combine(@"d:\VideoTranslator\logs", $"visualtree_{timestamp}.txt");

            Helpers.VisualTreeHelperEx.DumpVisualTreeToFile(this, logFilePath);

            _logger.Information("可视化树已导出到: {FilePath}", logFilePath);

            _audioPlayerService?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出可视化树时发生错误");
        }
    }

    #endregion

    #region 字段

    private VideoEditorViewModel? _viewModel;
    private IAudioPlayerService? _audioPlayerService;
    private IProgressService? _progressService;
    private static readonly ILogger _logger = LoggerService.ForContext<MainWindow>();

    private IObjectSpace? _objectSpace { get => this.Context.ObjectSpace; set => this.Context.ObjectSpace = value; }

    private VideoProject? _currentProject;


    private const bool DEBUG_MODE = false;
    private const string DEBUG_VIDEO_PATH = @"D:\VideoTranslator\videoProjects\23008\4RswhZEY9rg.mp4";



    #endregion

    #region 初始化

    private void InitializeViewModel()
    {
        try
        {
            _logger.Information("开始初始化 ViewModel");
            _viewModel = new VideoEditorViewModel();

            DataContext = _viewModel;

            _logger.Information("ViewModel 初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化 ViewModel 失败");
            MessageBox.Show($"初始化 ViewModel 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void InitializeServices()
    {
        try
        {
            _logger.Information("开始初始化服务");

            _audioPlayerService = new AudioPlayerService();
            timeLinePanel.AudioPlayerService = _audioPlayerService;
            timeLinePanel.SegmentPlayRequested += OnSegmentPlayRequested;

            _objectSpace = ServiceHelper.CreateObjectSpace();
            
            _progressService = ServiceHelper.GetService<IProgressService>();

            _logger.Information("服务初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化服务失败");
            MessageBox.Show($"初始化服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    #endregion

    #region 状态栏更新

    private void UpdateStatus(string message)
    {
        _progressService?.SetStatusMessage(message, VideoTranslator.Interfaces.MessageType.Info);
    }

    private void UpdateProgressBar(bool isIndeterminate)
    {
        if (isIndeterminate)
        {
            _progressService?.ShowProgress(true);
        }
        else
        {
            _progressService?.HideProgress();
        }
    }

    #endregion

    #region 窗口事件

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.Information("MainWindow Loaded 事件触发");
        try
        {
            this.Activate();
            this.Focus();
            _logger.Information("MainWindow 已激活并聚焦");

            await AutoOpenLastProject();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "MainWindow Loaded 事件处理失败");
        }
    }

    #region 自动打开项目

    private async Task AutoOpenLastProject()
    {
        try
        {
            _logger.Information("开始自动打开最后一个项目");

            if (_objectSpace == null)
            {
                _logger.Warning("ObjectSpace 未初始化，无法自动打开项目");
                return;
            }

            //var projects = _objectSpace.GetObjectsQuery<VideoProject>().ToList();
            //_logger.Information("从数据库加载了 {Count} 个项目", projects.Count);

            //if (projects.Count == 0)
            //{
            //    _logger.Information("数据库中没有项目，跳过自动打开");
            //    return;
            //}

            //var lastProject = projects.OrderByDescending(p => p.Oid).First();
            //_logger.Information("找到最后一个项目: {ProjectName} (Oid: {Oid})", lastProject.ProjectName, lastProject.Oid);
            //LoadProject(lastProject);
            //_logger.Information("自动打开项目完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "自动打开项目失败");
        }
    }

    #endregion

    #endregion

    #region 菜单事件处理


    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CloseProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("关闭项目菜单项被点击");

            if (_currentProject == null)
            {
                _logger.Warning("当前没有打开的项目");
                MessageBox.Show("当前没有打开的项目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定要关闭项目 \"{_currentProject.ProjectName}\" 吗？", "确认关闭", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _logger.Information("用户确认关闭项目: {ProjectName}", _currentProject.ProjectName);

                #region 取消订阅事件

                _currentProject.Changed -= OnObjectChanged;
                UnsubscribeTracksCollectionChanged(_currentProject);
                _logger.Information("已取消订阅项目事件");

                #endregion

                #region 清空时间线

                timeLinePanel.Tracks = null;
                _logger.Information("已清空时间线");

                #endregion

                #region 清空视频播放器

                videoPlayerControl.OriginalVideoPath = string.Empty;
                videoPlayerControl.MutedVideoPath = string.Empty;
                videoPlayerControl.MergedAudioPath = string.Empty;
                videoPlayerControl.FinalVideoPath = string.Empty;
                //videoPlayerControl.ClearVideo();
                //videoPlayerControl.ClearSubtitle();
                _logger.Information("已清空视频播放器");

                #endregion

                #region 清空当前项目

                _currentProject = null;
                _logger.Information("已清空当前项目");

                #endregion

                #region 更新状态

                UpdateStatus("项目已关闭");
                _logger.Information("项目关闭完成");

                #endregion
            }
            else
            {
                _logger.Information("用户取消了关闭项目");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "关闭项目时发生错误");
            MessageBox.Show($"关闭项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MediaSource_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("媒体源按钮被点击");

            if (_currentProject == null)
            {
                _logger.Warning("当前没有打开的项目");
                MessageBox.Show("请先打开一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mediaSourceWindow = new Windows.MediaSourceWindow(_currentProject, this._objectSpace)
            {
                Owner = this
            };

            _logger.Information("显示媒体源对话框（非模态）");
            mediaSourceWindow.Show();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "打开媒体源对话框时发生错误");
            MessageBox.Show($"打开媒体源对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void VoskTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("Vosk语音识别测试菜单项被点击");

            var voskTestWindow = new Windows.VoskTestWindow
            {
                Owner = this
            };

            _logger.Information("显示Vosk语音识别测试对话框");
            voskTestWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "打开Vosk语音识别测试对话框时发生错误");
            MessageBox.Show($"打开Vosk语音识别测试对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DownloadVideo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("下载视频菜单项被点击");

            var videoDownloadWindow = new Windows.VideoDownloadWindow(this._objectSpace)
            {
                Owner = this
            };

            _logger.Information("显示视频下载对话框");
            var result = videoDownloadWindow.ShowDialog();
            _logger.Information("视频下载对话框关闭，结果: {Result}", result);
            
            if (videoDownloadWindow.CreatedProject != null)
                this.LoadProject(videoDownloadWindow.CreatedProject);

            if (result == true)
            {
                _logger.Information("视频下载成功");
                if (!string.IsNullOrEmpty(videoDownloadWindow.DownloadedVideoPath))
                {
                    MessageBox.Show($"视频下载成功: {videoDownloadWindow.DownloadedVideoPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (!string.IsNullOrEmpty(videoDownloadWindow.DownloadedAudioPath))
                {
                    MessageBox.Show($"音频下载成功: {videoDownloadWindow.DownloadedAudioPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "打开视频下载对话框时发生错误");
            MessageBox.Show($"打开视频下载对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportFromYouTube_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("从YouTube导入菜单项被点击");

            var youTubeImportWindow = new YouTubeImportWindow
            {
                Owner = this
            };

            _logger.Information("显示YouTube导入对话框");
            var result = youTubeImportWindow.ShowDialog();
            _logger.Information("YouTube导入对话框关闭，结果: {Result}", result);

            if (result == true)
            {
                _logger.Information("用户确认导入，开始获取选择结果");
                var selection = youTubeImportWindow.GetSelection();

                if (selection == null || string.IsNullOrWhiteSpace(selection.Url))
                {
                    _logger.Warning("选择结果为空或URL无效");
                    MessageBox.Show("请输入有效的YouTube视频地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _logger.Information("开始导入YouTube视频: {Url}, 下载视频={DownloadVideo}, 下载音频={DownloadAudio}, 字幕数量={SubtitleCount}",
                    selection.Url, selection.DownloadVideo, selection.DownloadAudio, selection.SelectedSubtitleLanguages.Count);

                await ImportFromYouTubeAsync(selection);
            }
            else
            {
                _logger.Information("用户取消了YouTube导入");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "从YouTube导入失败");
            MessageBox.Show($"从YouTube导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ImportFromYouTubeAsync(YouTubeDownloadSelection selection)
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
            var project = _objectSpace!.CreateObject<VideoProject>();
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

            LoadProject(project);
            await ProcessProjectAudio(project);

            _logger.Information("YouTube视频导入完成");
            MessageBox.Show($"YouTube视频导入成功: {videoInfo.Title}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "从YouTube导入视频时发生错误");
            throw;
        }
    }

    private async Task DownloadYouTubeVideoAsync(VideoProject project, YouTubeDownloadSelection selection)
    {
        try
        {
            var youtubeService = new VideoTranslator.Interfaces.YouTubeService();
            var safeProjectName = SanitizeFileName(project.ProjectName);

            if (selection.DownloadVideo && selection.SelectedVideoStream != null)
            {
                _logger.Information("开始下载YouTube视频: URL={Url}, 视频质量={Quality}, 音频质量={AudioQuality}",
                    selection.Url, selection.SelectedVideoStream.QualityLabel,
                    selection.SelectedAudioStream?.Bitrate);
                UpdateStatus("正在下载YouTube视频...");
                UpdateProgressBar(true);

                var videoFileName = $"{Path.GetFileNameWithoutExtension(safeProjectName)}.mp4";
                var outputPath = Path.Combine(project.ProjectPath, videoFileName);

                _logger.Information("视频输出路径: {OutputPath}", outputPath);
                await youtubeService.DownloadVideoAsync(selection.Url, selection.SelectedVideoStream, selection.SelectedAudioStream, outputPath);

                _logger.Information("开始导入视频到项目");
                project.ImportVideoFile(outputPath);
                _objectSpace.CommitChanges();

                _logger.Information("YouTube视频下载成功: {OutputPath}", outputPath);
                UpdateStatus($"视频下载成功: {Path.GetFileName(outputPath)}");
                UpdateProgressBar(false);
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
                UpdateStatus("正在下载YouTube音频...");

                var audioFileName = $"{Path.GetFileNameWithoutExtension(safeProjectName)}.wav";
                var audioPath = Path.Combine(project.ProjectPath, audioFileName);

                _logger.Information("音频输出路径: {AudioPath}", audioPath);
                await youtubeService.DownloadAudioAsync(selection.Url, audioPath);

                _logger.Information("YouTube音频下载成功: {AudioPath}", audioPath);
            }
            else
            {
                _logger.Information("跳过音频下载: DownloadAudio={DownloadAudio}, AudioStream={HasAudioStream}",
                    selection.DownloadAudio, selection.SelectedAudioStream != null);
            }

            _logger.Information("开始下载字幕，共{Count}个语言", selection.SelectedSubtitleLanguages.Count);

            foreach (var langCode in selection.SelectedSubtitleLanguages)
            {
                _logger.Information("开始下载字幕: {LanguageCode}", langCode);
                UpdateStatus($"正在下载字幕: {langCode}...");

                var subtitleFileName = $"{Path.GetFileNameWithoutExtension(safeProjectName)}_{langCode}.srt";
                var subtitlePath = Path.Combine(project.ProjectPath, subtitleFileName);

                _logger.Information("字幕输出路径: {SubtitlePath}", subtitlePath);
                await youtubeService.DownloadSubtitleAsync(selection.Url, langCode, subtitlePath);

                _logger.Information("字幕下载成功: {SubtitlePath}", subtitlePath);
            }

            _logger.Information("所有下载任务完成");
            UpdateStatus("就绪");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "下载YouTube视频失败");
            _objectSpace.CommitChanges();
            UpdateStatus("就绪");
            UpdateProgressBar(false);
            throw;
        }
    }

    private async void MergeVideoAndSubtitles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("生成最终视频菜单项被点击");

            if (_currentProject == null)
            {
                _logger.Warning("当前没有打开的项目");
                MessageBox.Show("请先打开一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var videoProject = _currentProject;

            _logger.Information("准备生成最终视频: {ProjectName}", videoProject.ProjectName);

            var audioValidate = videoProject.ValidateSourceAudio();
            if (!audioValidate.Success)
            {
                _logger.Warning("源音频验证失败: {ErrorMessage}", audioValidate.ErrorMessage);
                MessageBox.Show(audioValidate.ErrorMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var inputVideoPath = videoProject.SourceMutedVideoPath;
            var outputPath = Path.Combine(videoProject.ProjectPath, "final_video_with_subtitles.mp4");

            if (string.IsNullOrEmpty(inputVideoPath) || !File.Exists(inputVideoPath))
            {
                _logger.Warning("静音视频文件不存在: {InputVideoPath}", inputVideoPath);
                MessageBox.Show($"静音视频文件不存在: {inputVideoPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(videoProject.OutputAudioPath) || !File.Exists(videoProject.OutputAudioPath))
            {
                _logger.Warning("合并音频文件不存在: {OutputAudioPath}", videoProject.OutputAudioPath);
                MessageBox.Show($"合并音频文件不存在: {videoProject.OutputAudioPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(videoProject.TranslatedSubtitlePath) || !File.Exists(videoProject.TranslatedSubtitlePath))
            {
                _logger.Warning("翻译字幕文件不存在: {TranslatedSubtitlePath}", videoProject.TranslatedSubtitlePath);
                MessageBox.Show($"翻译字幕文件不存在: {videoProject.TranslatedSubtitlePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var escapedChinesePath = videoProject.TranslatedSubtitlePath.EscapeForFFmpeg();
            var escapedVideoPath = inputVideoPath.EscapeForFFmpeg();
            var escapedAudioPath = videoProject.OutputAudioPath.EscapeForFFmpeg();

            var videoEncoder = videoProject.GetFFmpegVideoEncoder();
            var hwaccel = videoProject.GetFFmpegHWAccel();
            var hwaccelArgs = string.IsNullOrEmpty(hwaccel) ? "" : $"-hwaccel {hwaccel} ";
            var preset = videoProject.GetFFmpegPreset();
            var crf = videoProject.GetCRFValue();
            var fastSubtitle = videoProject.FastSubtitleRendering;

            var writeSourceSubtitle = videoProject.WriteSourceSubtitle;
            var writeTargetSubtitle = videoProject.WriteTargetSubtitle;
            var sourceSubtitleType = videoProject.SourceSubtitleType;
            var targetSubtitleType = videoProject.TargetSubtitleType;

            var ffmpegArgs = "";
            var inputArgs = $"-i \"{inputVideoPath}\" -i \"{videoProject.OutputAudioPath}\" ";
            var filterComplexArgs = "";
            var mapArgs = "";
            var subtitleMapArgs = "";
            var hasHardSubtitle = false;
            var subtitleIndex = 0;

            if (writeTargetSubtitle && !string.IsNullOrEmpty(videoProject.TranslatedSubtitlePath) && File.Exists(videoProject.TranslatedSubtitlePath))
            {
                if (targetSubtitleType == SubtitleType.HardBurn)
                {
                    hasHardSubtitle = true;
                    var fontSize = fastSubtitle ? 16 : 14;
                    filterComplexArgs = $"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=50,Alignment=2'[v]";
                }
                else
                {
                    inputArgs += $"-i \"{videoProject.TranslatedSubtitlePath}\" ";
                    subtitleMapArgs += $"-map 2:s:{subtitleIndex} -c:s:{subtitleIndex} mov_text ";
                    subtitleIndex++;
                }
            }

            if (writeSourceSubtitle && !string.IsNullOrEmpty(videoProject.SourceSubtitlePath) && File.Exists(videoProject.SourceSubtitlePath))
            {
                var escapedEnglishPath = videoProject.SourceSubtitlePath.EscapeForFFmpeg();

                if (sourceSubtitleType == SubtitleType.HardBurn)
                {
                    hasHardSubtitle = true;
                    if (hasHardSubtitle)
                    {
                        var fontSize = fastSubtitle ? 14 : 14;
                        filterComplexArgs += $";[v]subtitles='{escapedEnglishPath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFF00,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=5,Alignment=2'[v]";
                    }
                    else
                    {
                        var fontSize = fastSubtitle ? 16 : 14;
                        filterComplexArgs = $"[0:v]subtitles='{escapedEnglishPath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=50,Alignment=2'[v]";
                    }
                }
                else
                {
                    inputArgs += $"-i \"{videoProject.SourceSubtitlePath}\" ";
                    subtitleMapArgs += $"-map 2:s:{subtitleIndex} -c:s:{subtitleIndex} mov_text ";
                    subtitleIndex++;
                }
            }

            if (hasHardSubtitle)
            {
                filterComplexArgs = $"-filter_complex \"{filterComplexArgs}\" ";
                mapArgs = "-map \"[v]\" -map 1:a ";
            }
            else
            {
                mapArgs = "-map 0:v -map 1:a ";
            }

            ffmpegArgs = $"{inputArgs}{filterComplexArgs}{mapArgs}{subtitleMapArgs}-c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -y \"{outputPath}\"";

            _logger.Information("开始执行FFmpeg命令生成最终视频");
            UpdateStatus("正在生成最终视频...");
            UpdateProgressBar(true);

            var ffmpegService = ServiceHelper.GetService<IFFmpegService>();
            await ffmpegService.ExecuteCommandAsync(ffmpegArgs);

            videoProject.OutputVideoPath = outputPath;
            _objectSpace?.CommitChanges();

            #region 生成B站发布信息
            _logger.Information("开始生成B站发布信息");
            UpdateStatus("正在生成B站发布信息...");

            try
            {
                var publishHelper = new Services.BilibiliPublishHelper(this._progressService);
                var publishInfo = await publishHelper.GeneratePublishInfoAsync(videoProject, forceRegenerate: true);

                videoProject.BilibiliPublishTitle = videoProject.ProjectName ?? "未命名视频";
                videoProject.BilibiliPublishTitleChinese = publishInfo.Title;
                videoProject.BilibiliPublishDescription = publishInfo.Description;
                videoProject.BilibiliPublishTags = string.Join(", ", publishInfo.Tags);
                videoProject.BilibiliPublishType = publishInfo.Type;
                videoProject.BilibiliPublishIsRepost = publishInfo.IsRepost;
                videoProject.BilibiliPublishSourceAddress = publishInfo.SourceAddress;
                videoProject.BilibiliPublishEnableOriginalWatermark = publishInfo.EnableOriginalWatermark;
                videoProject.BilibiliPublishEnableNoRepost = publishInfo.EnableNoRepost;

                _objectSpace?.CommitChanges();
                _logger.Information("B站发布信息生成完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "生成B站发布信息时发生错误，但不影响视频生成");
            }
            #endregion

            UpdateProgressBar(false);
            UpdateStatus("就绪");

            _logger.Information("最终视频生成成功: {OutputPath}", outputPath);
            MessageBox.Show($"最终视频生成成功: {outputPath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateVideoPlayerPlayModes(videoProject);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "生成最终视频时发生错误");
            UpdateStatus("就绪");
            UpdateProgressBar(false);
            MessageBox.Show($"生成最终视频时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MergeVideoAndSubtitlesHighQuality_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("高质量生成最终视频菜单项被点击");

            if (_currentProject == null)
            {
                _logger.Warning("当前没有打开的项目");
                MessageBox.Show("请先打开一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var videoProject = _currentProject;

            _logger.Information("准备高质量生成最终视频: {ProjectName}", videoProject.ProjectName);

            var audioValidate = videoProject.ValidateSourceAudio();
            if (!audioValidate.Success)
            {
                _logger.Warning("源音频验证失败: {ErrorMessage}", audioValidate.ErrorMessage);
                MessageBox.Show(audioValidate.ErrorMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var inputVideoPath = videoProject.SourceMutedVideoPath;
            var outputPath = Path.Combine(videoProject.ProjectPath, "final_video_with_subtitles_hq.mp4");

            if (string.IsNullOrEmpty(inputVideoPath) || !File.Exists(inputVideoPath))
            {
                _logger.Warning("静音视频文件不存在: {InputVideoPath}", inputVideoPath);
                MessageBox.Show($"静音视频文件不存在: {inputVideoPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(videoProject.OutputAudioPath) || !File.Exists(videoProject.OutputAudioPath))
            {
                _logger.Warning("合并音频文件不存在: {OutputAudioPath}", videoProject.OutputAudioPath);
                MessageBox.Show($"合并音频文件不存在: {videoProject.OutputAudioPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(videoProject.TranslatedSubtitlePath) || !File.Exists(videoProject.TranslatedSubtitlePath))
            {
                _logger.Warning("翻译字幕文件不存在: {TranslatedSubtitlePath}", videoProject.TranslatedSubtitlePath);
                MessageBox.Show($"翻译字幕文件不存在: {videoProject.TranslatedSubtitlePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var escapedChinesePath = videoProject.TranslatedSubtitlePath.EscapeForFFmpeg();
            var escapedVideoPath = inputVideoPath.EscapeForFFmpeg();
            var escapedAudioPath = videoProject.OutputAudioPath.EscapeForFFmpeg();

            var videoEncoder = videoProject.GetFFmpegVideoEncoder();
            var hwaccel = videoProject.GetFFmpegHWAccel();
            var hwaccelArgs = string.IsNullOrEmpty(hwaccel) ? "" : $"-hwaccel {hwaccel} ";

            var preset = videoProject.UseGPU ? "p2" : "slow";
            var crf = 20;
            var fastSubtitle = false;

            var writeSourceSubtitle = videoProject.WriteSourceSubtitle;
            var writeTargetSubtitle = videoProject.WriteTargetSubtitle;
            var sourceSubtitleType = videoProject.SourceSubtitleType;
            var targetSubtitleType = videoProject.TargetSubtitleType;

            var ffmpegArgs = "";
            var inputArgs = $"-i \"{inputVideoPath}\" -i \"{videoProject.OutputAudioPath}\" ";
            var filterComplexArgs = "";
            var mapArgs = "";
            var subtitleMapArgs = "";
            var hasHardSubtitle = false;
            var subtitleIndex = 0;

            if (writeTargetSubtitle && !string.IsNullOrEmpty(videoProject.TranslatedSubtitlePath) && File.Exists(videoProject.TranslatedSubtitlePath))
            {
                if (targetSubtitleType == SubtitleType.HardBurn)
                {
                    hasHardSubtitle = true;
                    var fontSize = 14;
                    filterComplexArgs = $"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=50,Alignment=2'[v]";
                }
                else
                {
                    inputArgs += $"-i \"{videoProject.TranslatedSubtitlePath}\" ";
                    subtitleMapArgs += $"-map 2:s:{subtitleIndex} -c:s:{subtitleIndex} mov_text ";
                    subtitleIndex++;
                }
            }

            if (writeSourceSubtitle && !string.IsNullOrEmpty(videoProject.SourceSubtitlePath) && File.Exists(videoProject.SourceSubtitlePath))
            {
                var escapedEnglishPath = videoProject.SourceSubtitlePath.EscapeForFFmpeg();

                if (sourceSubtitleType == SubtitleType.HardBurn)
                {
                    hasHardSubtitle = true;
                    if (hasHardSubtitle)
                    {
                        var fontSize = 14;
                        filterComplexArgs += $";[v]subtitles='{escapedEnglishPath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFF00,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=5,Alignment=2'[v]";
                    }
                    else
                    {
                        var fontSize = 14;
                        filterComplexArgs = $"[0:v]subtitles='{escapedEnglishPath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=50,Alignment=2'[v]";
                    }
                }
                else
                {
                    inputArgs += $"-i \"{videoProject.SourceSubtitlePath}\" ";
                    subtitleMapArgs += $"-map 2:s:{subtitleIndex} -c:s:{subtitleIndex} mov_text ";
                    subtitleIndex++;
                }
            }

            if (hasHardSubtitle)
            {
                filterComplexArgs = $"-filter_complex \"{filterComplexArgs}\" ";
                mapArgs = "-map \"[v]\" -map 1:a ";
            }
            else
            {
                mapArgs = "-map 0:v -map 1:a ";
            }

            ffmpegArgs = $"{inputArgs}{filterComplexArgs}{mapArgs}{subtitleMapArgs}-c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -y \"{outputPath}\"";

            _logger.Information("开始执行FFmpeg命令高质量生成最终视频");
            UpdateStatus("正在高质量生成最终视频...");
            UpdateProgressBar(true);
                        
            var ffmpegService = ServiceHelper.GetService<IFFmpegService>();
            await ffmpegService.ExecuteCommandAsync(ffmpegArgs);

            videoProject.OutputVideoPath = outputPath;
            _objectSpace?.CommitChanges();

            #region 生成B站发布信息
            _logger.Information("开始生成B站发布信息");
            UpdateStatus("正在生成B站发布信息...");

            try
            {
                var publishHelper = new Services.BilibiliPublishHelper();
                var publishInfo = await publishHelper.GeneratePublishInfoAsync(videoProject, forceRegenerate: true);

                videoProject.BilibiliPublishTitle = videoProject.ProjectName ?? "未命名视频";
                videoProject.BilibiliPublishTitleChinese = publishInfo.Title;
                videoProject.BilibiliPublishDescription = publishInfo.Description;
                videoProject.BilibiliPublishTags = string.Join(", ", publishInfo.Tags);
                videoProject.BilibiliPublishType = publishInfo.Type;
                videoProject.BilibiliPublishIsRepost = publishInfo.IsRepost;
                videoProject.BilibiliPublishSourceAddress = publishInfo.SourceAddress;
                videoProject.BilibiliPublishEnableOriginalWatermark = publishInfo.EnableOriginalWatermark;
                videoProject.BilibiliPublishEnableNoRepost = publishInfo.EnableNoRepost;

                _objectSpace?.CommitChanges();
                _logger.Information("B站发布信息生成完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "生成B站发布信息时发生错误，但不影响视频生成");
            }
            #endregion

            UpdateProgressBar(false);
            UpdateStatus("就绪");

            _logger.Information("高质量最终视频生成成功: {OutputPath}", outputPath);
            MessageBox.Show($"高质量最终视频生成成功: {outputPath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateVideoPlayerPlayModes(videoProject);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "高质量生成最终视频时发生错误");
            UpdateStatus("就绪");
            UpdateProgressBar(false);
            MessageBox.Show($"高质量生成最终视频时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void PublishToBilibili_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("发布到B站菜单项被点击");

            if (_currentProject == null)
            {
                _logger.Warning("当前没有打开的项目");
                MessageBox.Show("请先打开一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var videoProject = _currentProject;

            _logger.Information("准备发布项目: {ProjectName}", videoProject.ProjectName);

            if (string.IsNullOrEmpty(videoProject.OutputVideoPath) || !File.Exists(videoProject.OutputVideoPath))
            {
                _logger.Warning("最终视频文件不存在: {OutputVideoPath}", videoProject.OutputVideoPath);
                MessageBox.Show($"最终视频文件不存在，请先生成最终视频: {videoProject.OutputVideoPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            #region Show publish dialog
            var publishWindow = new Windows.BilibiliPublishWindow(videoProject)
            {
                Owner = this
            };

            _logger.Information("显示B站发布对话框");
            var result = publishWindow.ShowDialog();
            _logger.Information("B站发布对话框关闭，结果: {Result}", result);

            if (result != true)
            {
                _logger.Information("用户取消了发布");
                return;
            }

            var publishInfo = publishWindow.GetPublishInfo();
            #endregion

            #region Publish to Bilibili
            var publishService = ServiceHelper.GetService<VT.Module.Services.IBilibiliPublishService>();
            if (publishService == null)
            {
                _logger.Warning("B站发布服务不可用");
                MessageBox.Show("B站发布服务不可用，请检查系统配置。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!publishService.IsServiceAvailable())
            {
                _logger.Warning("B站发布服务未正确配置");
                MessageBox.Show("B站发布服务未正确配置，请检查BCUT路径等配置。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _logger.Information("开始发布视频到B站: {VideoPath}", publishInfo.VideoFilePath);
            UpdateStatus("正在发布到B站...");
            UpdateProgressBar(true);

            var publishResult = await publishService.PublishVideoAsync(publishInfo);

            UpdateProgressBar(false);
            UpdateStatus("就绪");

            if (publishResult)
            {
                _logger.Information("视频发布到B站成功");
                MessageBox.Show("视频已成功发布到B站！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _logger.Warning("视频发布到B站失败");
                MessageBox.Show("发布到B站失败，请检查日志信息。", "失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            #endregion
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "发布到B站时发生错误");
            UpdateStatus("就绪");
            UpdateProgressBar(false);
            MessageBox.Show($"发布到B站时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 更新视频播放器的播放模式
    /// </summary>
    private void UpdateVideoPlayerPlayModes(VideoProject videoProject)
    {
        try
        {
            _logger.Information("开始更新播放模式");

            var availableModes = new List<VideoPlayer.PlayMode>();
            var unavailableReasons = new Dictionary<VideoPlayer.PlayMode, string>();

            // 设置视频路径
            videoPlayerControl.OriginalVideoPath = videoProject.SourceVideoPath ?? string.Empty;
            videoPlayerControl.MutedVideoPath = videoProject.SourceMutedVideoPath ?? string.Empty;
            videoPlayerControl.MergedAudioPath = videoProject.OutputAudioPath ?? string.Empty;
            videoPlayerControl.FinalVideoPath = videoProject.OutputVideoPath ?? string.Empty;

            // 检查原始视频模式
            if (!string.IsNullOrEmpty(videoProject.SourceVideoPath) && File.Exists(videoProject.SourceVideoPath))
            {
                availableModes.Add(VideoPlayer.PlayMode.OriginalVideo);
                _logger.Information("添加播放模式: 原始视频");
            }
            else
            {
                unavailableReasons[VideoPlayer.PlayMode.OriginalVideo] =
                    string.IsNullOrEmpty(videoProject.SourceVideoPath) ? "未设置原始视频路径" : "原始视频文件不存在";
            }

            // 检查静音视频+音频模式
            var mutedVideoExists = !string.IsNullOrEmpty(videoProject.SourceMutedVideoPath) && File.Exists(videoProject.SourceMutedVideoPath);
            var mergedAudioExists = !string.IsNullOrEmpty(videoProject.OutputAudioPath) && File.Exists(videoProject.OutputAudioPath);

            if (mutedVideoExists && mergedAudioExists)
            {
                availableModes.Add(VideoPlayer.PlayMode.MutedVideoWithAudio);
                _logger.Information("添加播放模式: 静音视频+音频");
            }
            else
            {
                var reasons = new List<string>();
                if (!mutedVideoExists)
                {
                    reasons.Add(string.IsNullOrEmpty(videoProject.SourceMutedVideoPath) ? "未设置静音视频路径" : "静音视频文件不存在");
                }
                if (!mergedAudioExists)
                {
                    reasons.Add(string.IsNullOrEmpty(videoProject.OutputAudioPath) ? "未设置合并音频路径" : "合并音频文件不存在");
                }
                unavailableReasons[VideoPlayer.PlayMode.MutedVideoWithAudio] = string.Join("; ", reasons);
            }

            // 检查最终视频模式
            if (!string.IsNullOrEmpty(videoProject.OutputVideoPath) && File.Exists(videoProject.OutputVideoPath))
            {
                availableModes.Add(VideoPlayer.PlayMode.FinalVideo);
                _logger.Information("添加播放模式: 最终视频");
            }
            else
            {
                unavailableReasons[VideoPlayer.PlayMode.FinalVideo] =
                    string.IsNullOrEmpty(videoProject.OutputVideoPath) ? "未设置最终视频路径" : "最终视频文件不存在";
            }

            // 设置可用的播放模式
            videoPlayerControl.PlayModesAvailable = availableModes.ToArray();

            // 设置不可用原因
            videoPlayerControl.PlayModeUnavailableReasons = unavailableReasons;

            // 默认选择第一个可用的播放模式
            if (availableModes.Count > 0)
            {
                videoPlayerControl.CurrentPlayMode = availableModes[0];
                _logger.Information("设置默认播放模式: {Mode}", availableModes[0]);
            }

            _logger.Information("播放模式更新完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "更新播放模式失败");
        }
    }

    /// <summary>
    /// VideoProject 对象变更事件处理
    /// </summary>
    private void OnObjectChanged(object? sender, DevExpress.Xpo.ObjectChangeEventArgs e)
    {
        try
        {
            if (_currentProject == null)
            {
                return;
            }

            var propertyName = e.PropertyName;
            _logger.Information("VideoProject 对象变更: {PropertyName}", propertyName);

            #region 监听关键路径属性变更，自动更新播放模式

            if (propertyName == nameof(VideoProject.OutputAudioPath) ||
                propertyName == nameof(VideoProject.SourceMutedVideoPath) ||
                propertyName == nameof(VideoProject.OutputVideoPath) ||
                propertyName == nameof(VideoProject.SourceVideoPath))
            {
                _logger.Information("检测到关键路径属性变更，更新播放模式");
                UpdateVideoPlayerPlayModes(_currentProject);
            }

            #endregion
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理 VideoProject 对象变更事件失败");
        }
    }



    private async void OnSegmentPlayRequested(object? sender, VT.Module.BusinessObjects.Clip clip)
    {
        if (clip is VT.Module.BusinessObjects.AudioClip audioClip && !string.IsNullOrEmpty(audioClip.FilePath))
        {
            try
            {
                _logger.Information("开始播放音频片段: ClipIndex={ClipIndex}, FilePath={FilePath}", audioClip.Index, audioClip.FilePath);
                await _audioPlayerService?.PlayAsync(audioClip.FilePath)!;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "播放音频片段失败: ClipIndex={ClipIndex}", audioClip.Index);
                MessageBox.Show($"播放音频失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// 加载视频文件
    /// </summary>
    public void LoadVideo(string filePath)
    {
        videoPlayerControl.LoadVideo(filePath);
    }

    /// <summary>
    /// 加载字幕文件
    /// </summary>
    public void LoadSubtitle(string filePath)
    {
        videoPlayerControl.LoadSubtitle(filePath);
    }



    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        if (sanitized != fileName)
        {
            _logger.Warning("文件名包含非法字符，已清理: 原文件名='{OriginalFileName}', 清理后='{SanitizedFileName}'", fileName, sanitized);
        }

        return sanitized;
    }

    #endregion

}
