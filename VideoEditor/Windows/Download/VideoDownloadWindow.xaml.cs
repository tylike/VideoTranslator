﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using VideoTranslator.Services;
using YoutubeDLSharp.Metadata;
using Serilog;
using WpfWindow = System.Windows.Window;
using System.IO;
using Microsoft.Win32;
using VideoTranslator.Config;
using VideoEditor.Helpers;
using VT.Module;
using VT.Module.BusinessObjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using Microsoft.Extensions.DependencyInjection;
using VT.Core;
using VideoEditor.Models;
using VideoTranslator.Interfaces;

namespace VideoEditor.Windows;

public partial class VideoDownloadWindow : WpfWindow
{
    #region 字段
    private static readonly ILogger _logger = Log.ForContext<VideoDownloadWindow>();
    private VideoData? _videoInfo;
    private const string BaseDownloadDirectory = @"d:\video.download";
    private readonly ObservableCollection<SubtitleItem> _subtitleItems = new();
    private string? _downloadedVideoPath;
    private string? _downloadedAudioPath;
    #endregion

    #region 属性

    public string? DownloadedVideoPath { get; private set; }
    public string? DownloadedAudioPath { get; private set; }
    public VideoData? VideoInfo => _videoInfo;
    public VideoProject? CreatedProject { get; private set; }
    IObjectSpace ObjectSpace;
    #endregion

    #region 构造函数

    public VideoDownloadWindow(IObjectSpace objectSpace)
    {
        this.ObjectSpace = objectSpace;
        InitializeComponent();
        Loaded += VideoDownloadWindow_Loaded;
    }

    private void VideoDownloadWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.Information("视频下载窗口已加载");
        Activate();
        Focus();
    }

    #endregion

    #region 按钮事件处理

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户点击下载按钮，准备下载");

        try
        {
            var url = UrlTextBox.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入视频地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            #region 创建下载文件夹

            var downloadFolder = CreateDownloadFolder();

            #endregion

            ShowLoading(true);
            LoadingTextBlock.Text = "正在下载...";

            try
            {
                #region 保存视频信息

                SaveVideoInfo(downloadFolder);

                #endregion

                #region 下载视频

                if (DownloadVideoCheckBox.IsChecked == true && VideoFormatsDataGrid.SelectedItems.Count > 0)
                {
                    var selectedFormats = VideoFormatsDataGrid.SelectedItems.Cast<FormatDisplayModel>().ToList();
                    foreach (var selectedFormat in selectedFormats)
                    {
                        var videoId = _videoInfo?.ID ?? "video";
                        var videoPath = Path.Combine(downloadFolder, $"{videoId}_video.{selectedFormat.Ext}");
                        _logger.Information("开始下载视频: 格式ID={FormatId}", selectedFormat.FormatId);

                        try
                        {
                            _downloadedVideoPath = await YtDlpService.DownloadVideoAsync(
                                url,
                                videoPath,
                                selectedFormat.FormatId,
                                new Progress<string>(UpdateProgress));

                            _logger.Information("视频下载完成: {Path}", _downloadedVideoPath);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("Requested format is not available"))
                        {
                            _logger.Warning(ex, "格式ID={FormatId}不可用，尝试自动选择格式", selectedFormat.FormatId);

                            var autoVideoPath = Path.Combine(downloadFolder, $"{videoId}_video_auto.{selectedFormat.Ext}");
                            _downloadedVideoPath = await YtDlpService.DownloadVideoAsync(
                                url,
                                autoVideoPath,
                                null,
                                new Progress<string>(UpdateProgress));

                            _logger.Information("视频下载完成（自动格式）: {Path}", _downloadedVideoPath);
                        }
                    }
                }
                #endregion

                #region 下载音频

                if (DownloadAudioCheckBox.IsChecked == true && AudioFormatsDataGrid.SelectedItems.Count > 0)
                {
                    var selectedFormats = AudioFormatsDataGrid.SelectedItems.Cast<FormatDisplayModel>().ToList();
                    foreach (var selectedFormat in selectedFormats)
                    {
                        var videoId = _videoInfo?.ID ?? "video";
                        var audioPath = Path.Combine(downloadFolder, $"{videoId}_audio.{selectedFormat.Ext}");
                        _logger.Information("开始下载音频: 格式ID={FormatId}", selectedFormat.FormatId);

                        try
                        {
                            _downloadedAudioPath = await YtDlpService.DownloadAudioAsync(
                                url,
                                audioPath,
                                selectedFormat.FormatId,
                                new Progress<string>(UpdateProgress));

                            _logger.Information("音频下载完成: {Path}", _downloadedAudioPath);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("Requested format is not available"))
                        {
                            _logger.Warning(ex, "格式ID={FormatId}不可用，尝试自动选择格式", selectedFormat.FormatId);

                            var autoAudioPath = Path.Combine(downloadFolder, $"{videoId}_audio_auto.{selectedFormat.Ext}");
                            _downloadedAudioPath = await YtDlpService.DownloadAudioAsync(
                                url,
                                autoAudioPath,
                                null,
                                new Progress<string>(UpdateProgress));

                            _logger.Information("音频下载完成（自动格式）: {Path}", _downloadedAudioPath);
                        }
                    }
                }
                #endregion

                #region 下载字幕

                if (DownloadSubtitleCheckBox.IsChecked == true)
                {
                    var selectedSubtitles = _subtitleItems.Where(s => s.IsSelected).ToList();
                    foreach (var subtitle in selectedSubtitles)
                    {
                        var videoId = _videoInfo?.ID ?? "video";
                        var subtitlePath = Path.Combine(downloadFolder, $"{videoId}_subtitle_{subtitle.LanguageCode}.srt");
                        _logger.Information("开始下载字幕: 语言代码={LanguageCode}", subtitle.LanguageCode);
                        await YtDlpService.DownloadSubtitleAsync(
                            url,
                            subtitlePath,
                            subtitle.LanguageCode,
                            new Progress<string>(UpdateProgress));
                    }
                }

                #endregion

                _logger.Information("下载完成: {Folder}", downloadFolder);
                DownloadedVideoPath = _downloadedVideoPath;
                DownloadedAudioPath = _downloadedAudioPath;

                #region 检测音频时长并自动触发VAD检测

                var mediaPath = !string.IsNullOrEmpty(_downloadedVideoPath) ? _downloadedVideoPath : _downloadedAudioPath;

                if (!string.IsNullOrEmpty(mediaPath))
                {
                    bool flowControl = await ExecuteVad(mediaPath);
                    if (!flowControl)
                    {
                        return;
                    }
                }

                #endregion

                ShowLoading(false);
                NewProjectButton.IsEnabled = true;
                MessageBox.Show("下载完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载失败");
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "下载按钮点击失败");
            MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户点击取消按钮");
        DialogResult = false;
        Close();
    }
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户点击确定按钮");
        DialogResult = true;
        Close();
    }

    private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户点击新建项目按钮");

        try
        {
            if (string.IsNullOrEmpty(_downloadedVideoPath) && string.IsNullOrEmpty(_downloadedAudioPath))
            {
                MessageBox.Show("请先下载视频或音频", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await CreateProjectFromDownload();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "新建项目失败");
            MessageBox.Show($"新建项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region --
    private void ShowLoading(bool show)
    {
        LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        FetchButton.IsEnabled = !show;
        DownloadButton.IsEnabled = !show && _videoInfo != null;
        NewProjectButton.IsEnabled = !show && (!string.IsNullOrEmpty(_downloadedVideoPath) || !string.IsNullOrEmpty(_downloadedAudioPath));
    }

    private void HideOptions()
    {
        VideoGroupBox.Visibility = Visibility.Collapsed;
        AudioGroupBox.Visibility = Visibility.Collapsed;
        SubtitleGroupBox.Visibility = Visibility.Collapsed;
        VideoInfoTextBlock.Visibility = Visibility.Collapsed;
        OkButton.IsEnabled = false;
    }

    private void ShowVideoOptions()
    {
        if (_videoInfo == null)
        {
            return;
        }

        var videoFormats = _videoInfo.GetVideoFormats();
        if (videoFormats.Count == 0)
        {
            return;
        }

        VideoGroupBox.Visibility = Visibility.Visible;
        var displayModels = videoFormats.Select(f => new FormatDisplayModel(f)).ToList();
        VideoFormatsDataGrid.ItemsSource = displayModels;

        if (VideoFormatsDataGrid.Items.Count > 0)
        {
            VideoFormatsDataGrid.SelectedIndex = 0;
        }
    }

    private void ShowAudioOptions()
    {
        if (_videoInfo == null)
        {
            return;
        }

        var audioFormats = _videoInfo.GetAudioFormats();
        if (audioFormats.Count == 0)
        {
            return;
        }

        AudioGroupBox.Visibility = Visibility.Visible;
        var displayModels = audioFormats.Select(f => new FormatDisplayModel(f)).ToList();
        AudioFormatsDataGrid.ItemsSource = displayModels;

        if (AudioFormatsDataGrid.Items.Count > 0)
        {
            AudioFormatsDataGrid.SelectedIndex = 0;
        }
    }

    private void ShowSubtitleOptions()
    {
        if (_videoInfo == null)
        {
            return;
        }

        var subtitleInfos = _videoInfo.GetSubtitleInfos();
        if (subtitleInfos.Count == 0)
        {
            return;
        }

        SubtitleGroupBox.Visibility = Visibility.Visible;
        _subtitleItems.Clear();

        foreach (var subtitle in subtitleInfos)
        {
            var typeText = subtitle.IsAutoGenerated ? "自动" : "人工";
            _subtitleItems.Add(new SubtitleItem
            {
                LanguageCode = subtitle.LanguageCode,
                LanguageName = subtitle.LanguageName,
                SubtitleType = typeText,
                IsSelected = true
            });
        }

        SubtitlesDataGrid.ItemsSource = _subtitleItems;
    }

    private void SelectAllSubtitlesButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var subtitle in _subtitleItems)
        {
            subtitle.IsSelected = true;
        }
        SubtitlesDataGrid.Items.Refresh();
    }

    private void DeselectAllSubtitlesButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var subtitle in _subtitleItems)
        {
            subtitle.IsSelected = false;
        }
        SubtitlesDataGrid.Items.Refresh();
    }

    private void UpdateProgress(string message)
    {
        _logger.Debug("进度更新: {Message}", message);
    }

    /// <summary>
    /// 获取下一个可用的下载文件夹序号
    /// </summary>
    private int GetNextFolderNumber()
    {
        if (!Directory.Exists(BaseDownloadDirectory))
        {
            Directory.CreateDirectory(BaseDownloadDirectory);
            return 1;
        }

        var existingFolders = Directory.GetDirectories(BaseDownloadDirectory)
            .Select(d => Path.GetFileName(d))
            .Where(d => int.TryParse(d, out _))
            .Select(d => int.Parse(d))
            .OrderByDescending(d => d)
            .ToList();

        if (existingFolders.Count == 0)
        {
            return 1;
        }

        return existingFolders[0] + 1;
    }

    /// <summary>
    /// 创建下载文件夹
    /// </summary>
    private string CreateDownloadFolder()
    {
        var folderNumber = GetNextFolderNumber();
        var folderPath = Path.Combine(BaseDownloadDirectory, folderNumber.ToString());
        Directory.CreateDirectory(folderPath);
        _logger.Information("创建下载文件夹: {FolderPath}", folderPath);
        return folderPath;
    }

    /// <summary>
    /// 保存视频信息到 JSON 文件
    /// </summary>
    private void SaveVideoInfo(string folderPath)
    {
        if (_videoInfo == null)
        {
            return;
        }

        var infoPath = Path.Combine(folderPath, "video_info.json");
        var info = new
        {
            Id = _videoInfo.ID,
            Title = _videoInfo.Title,
            Description = _videoInfo.Description,
            Uploader = _videoInfo.Uploader,
            UploaderId = _videoInfo.UploaderID,
            UploadDate = _videoInfo.GetFormattedUploadDate(),
            Duration = _videoInfo.GetFormattedDuration(),
            ViewCount = _videoInfo.GetFormattedViewCount(),
            LikeCount = _videoInfo.GetFormattedLikeCount(),
            Thumbnail = _videoInfo.Thumbnail,
            VideoFormats = _videoInfo.GetVideoFormats().Select(f => new
            {
                f.FormatId,
                Ext = f.Extension,
                f.Resolution,
                Fps = f.GetFormattedFps(),
                VCodec = f.VideoCodec,
                ACodec = f.AudioCodec,
                FileSize = f.GetFormattedFileSize(),
                FileSizeBytes = f.GetFileSizeBytes(),
                Tbr = f.GetFormattedTbr(),
                Vbr = f.GetFormattedVbr(),
                Abr = f.GetFormattedAbr(),
                f.FormatNote
            }).ToList(),
            AudioFormats = _videoInfo.GetAudioFormats().Select(f => new
            {
                f.FormatId,
                Ext = f.Extension,
                ACodec = f.AudioCodec,
                FileSize = f.GetFormattedFileSize(),
                FileSizeBytes = f.GetFileSizeBytes(),
                Tbr = f.GetFormattedTbr(),
                Abr = f.GetFormattedAbr(),
                f.FormatNote
            }).ToList(),
            Subtitles = _videoInfo.GetSubtitleInfos().Select(f => new
            {
                f.LanguageCode,
                f.LanguageName,
                f.IsAutoGenerated
            }).ToList(),
            FullTitle = _videoInfo.AltTitle,
            DurationString = _videoInfo.GetFormattedDuration(),
            WebpageUrl = _videoInfo.WebpageUrl,
            OriginalUrl = _videoInfo.WebpageUrl,
            ChannelId = _videoInfo.ChannelID,
            ChannelUrl = _videoInfo.ChannelUrl,
            ChannelFollowerCount = _videoInfo.ChannelFollowerCount ?? 0,
            ChannelIsVerified = false,
            UploaderUrl = _videoInfo.UploaderUrl,
            AverageRating = _videoInfo.GetFormattedAverageRating(),
            CommentCount = _videoInfo.CommentCount ?? 0,
            Categories = _videoInfo.Categories,
            Tags = _videoInfo.Tags,
            LiveStatus = _videoInfo.LiveStatus,
            IsLive = _videoInfo.IsLive,
            WasLive = _videoInfo.WasLive,
            PlayableInEmbed = _videoInfo.PlayableInEmbed,
            AgeLimit = _videoInfo.AgeLimit,
            Thumbnails = _videoInfo.Thumbnails?.Select(t => new
            {
                Url = t.Url,
                Id = t.ID,
                t.Width,
                t.Height,
                t.Resolution
            }).ToList(),
            Chapters = _videoInfo.Chapters?.Select(c => new
            {
                c.Title,
                c.StartTime,
                c.EndTime
            }).ToList(),
            Extractor = _videoInfo.Extractor
        };

        var json = System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(infoPath, json, System.Text.Encoding.UTF8);
        _logger.Information("视频信息已保存: {InfoPath}", infoPath);
    }

    #endregion

    #region 项目创建

    private async Task CreateProjectFromDownload()
    {
        try
        {
            _logger.Information("开始从下载内容创建项目");

            var objectSpace = this.ObjectSpace;

            #region 验证下载文件

            if (string.IsNullOrEmpty(_downloadedVideoPath) && string.IsNullOrEmpty(_downloadedAudioPath))
            {
                var message = "未找到下载的视频或音频文件，无法创建项目。";
                _logger.Error(message);
                MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_downloadedVideoPath) && !File.Exists(_downloadedVideoPath))
            {
                var message = $"视频文件不存在: {_downloadedVideoPath}";
                _logger.Error(message);
                MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_downloadedAudioPath) && !File.Exists(_downloadedAudioPath))
            {
                var message = $"音频文件不存在: {_downloadedAudioPath}";
                _logger.Error(message);
                MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            #endregion

            #region 创建项目

            var projectName = $"视频项目_{_videoInfo?.Title ?? DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            var project = await VideoProject.CreateProject(
                objectSpace,
                projectName,
                _downloadedVideoPath,
                VT.Core.Language.Auto,
                VT.Core.Language.Chinese,
                _downloadedAudioPath
            );

            if (project == null)
            {
                _logger.Error("创建项目失败");
                MessageBox.Show("创建项目失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            #endregion

            #region 检测语言

            var serviceProvider = ServiceHelper.GetMainServiceProvider();
            var progressService = serviceProvider.GetRequiredService<IProgressService>();

            if (!string.IsNullOrEmpty(project.SourceAudioPath) && File.Exists(project.SourceAudioPath))
            {
                progressService?.SetStatusMessage("正在检测音频语言...");

                var whisperService = serviceProvider.GetRequiredService<WhisperRecognitionService>();
                var detectedLanguage = await whisperService.DetectLanguageAsync(project.SourceAudioPath);
                _logger.Information("检测到的语言: {DetectedLanguage}", detectedLanguage);

                #region 根据检测结果设置语言

                if (detectedLanguage != VT.Core.Language.Chinese)
                {
                    project.SourceLanguage = detectedLanguage;
                    project.TargetLanguage = VT.Core.Language.Chinese;
                    _logger.Information("检测到非中文语言，设置源语言为: {SourceLanguage}，目标语言为: {TargetLanguage}", project.SourceLanguage, project.TargetLanguage);
                }
                else
                {
                    project.SourceLanguage = VT.Core.Language.Chinese;
                    project.TargetLanguage = VT.Core.Language.English;
                    _logger.Information("检测到中文语言，设置源语言为: {SourceLanguage}，目标语言为: {TargetLanguage}", project.SourceLanguage, project.TargetLanguage);
                }

                objectSpace.CommitChanges();

                #endregion
            }
            else
            {
                _logger.Warning("音频文件不存在，跳过语言检测: {SourceAudioPath}", project.SourceAudioPath);
            }

            #endregion

            progressService?.ResetProgress();
            _logger.Information("项目创建完成");

            CreatedProject = project;
            DialogResult = true;
            Close();

            MessageBox.Show("项目创建成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "创建项目时发生错误");
            MessageBox.Show($"创建项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 内部类

    private class SubtitleItem
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string SubtitleType { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    #endregion
}
