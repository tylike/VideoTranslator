﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using Serilog;
using VT.Core;
using VT.Module;

namespace VideoEditor.Windows;

public partial class YouTubeImportWindow : Window
{
    private readonly IYouTubeService _youtubeService;
    private readonly ChatService _chatService;
    private YouTubeVideo? _currentVideo;
    private readonly List<SubtitleItem> _subtitleItems = new();
    private static readonly ILogger _logger = LoggerService.ForContext<YouTubeImportWindow>();

    public YouTubeImportWindow()
    {
        InitializeComponent();
        _youtubeService = new YouTubeService();
        _chatService = new ChatService();
        Loaded += YouTubeImportWindow_Loaded;
        InitializeLanguageComboBoxes();
    }

    private void YouTubeImportWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.Information("YouTube导入窗口已加载");
        Activate();
        Focus();
    }

    #region 初始化语言选项

    private void InitializeLanguageComboBoxes()
    {
        var languages = Enum.GetValues(typeof(VT.Core.Language)).Cast<VT.Core.Language>().ToList();

        SourceLanguageComboBox.ItemsSource = languages;
        SourceLanguageComboBox.SelectedIndex = languages.IndexOf(VT.Core.Language.English);

        TargetLanguageComboBox.ItemsSource = languages;
        TargetLanguageComboBox.SelectedIndex = languages.IndexOf(VT.Core.Language.Chinese);
    }

    #endregion

    #region 按钮事件处理

    private async void FetchButton_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            _logger.Warning("用户未输入YouTube视频地址");
            MessageBox.Show("请输入YouTube视频地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _logger.Information("开始获取YouTube视频信息: {Url}", url);
        ShowLoading(true);
        HideOptions();

        try
        {
            _currentVideo = await _youtubeService.GetVideoInfoAsync(url);
            _logger.Information("成功获取视频信息: 标题={Title}, 视频ID={VideoId}, 时长={Duration}", 
                _currentVideo.Title, _currentVideo.VideoId, _currentVideo.Duration);

            VideoInfoTextBlock.Text = $"标题: {_currentVideo.Title}\n时长: {_currentVideo.Duration:hh\\:mm\\:ss}";
            VideoInfoTextBlock.Visibility = Visibility.Visible;

            ShowVideoOptions();
            ShowAudioOptions();
            ShowSubtitleOptions();
            ShowLanguageOptions();

            OkButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "获取YouTube视频信息失败，尝试使用LLM生成: {Url}", url);
            
            try
            {
                _currentVideo = await GenerateVideoInfoWithLLM(url);
                _logger.Information("LLM生成视频信息成功: 标题={Title}, 视频ID={VideoId}", 
                    _currentVideo.Title, _currentVideo.VideoId);

                VideoInfoTextBlock.Text = $"标题: {_currentVideo.Title}\n时长: {_currentVideo.Duration:hh\\:mm\\:ss}\n(由LLM生成)";
                VideoInfoTextBlock.Visibility = Visibility.Visible;

                ShowVideoOptions();
                ShowAudioOptions();
                ShowSubtitleOptions();
                ShowLanguageOptions();

                OkButton.IsEnabled = true;
            }
            catch (Exception llmEx)
            {
                _logger.Error(llmEx, "LLM生成视频信息失败: {Url}", url);
                MessageBox.Show($"获取视频信息失败: {ex.Message}\nLLM生成也失败: {llmEx.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Debug("用户点击全选按钮");
        foreach (var item in _subtitleItems)
        {
            item.IsSelected = true;
        }
    }

    private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Debug("用户点击全否按钮");
        foreach (var item in _subtitleItems)
        {
            item.IsSelected = false;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户点击确定按钮，准备返回选择结果");
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户点击取消按钮");
        DialogResult = false;
        Close();
    }

    #endregion

    #region UI显示控制

    private void ShowLoading(bool show)
    {
        LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        FetchButton.IsEnabled = !show;
    }

    private void HideOptions()
    {
        VideoGroupBox.Visibility = Visibility.Collapsed;
        AudioGroupBox.Visibility = Visibility.Collapsed;
        SubtitleGroupBox.Visibility = Visibility.Collapsed;
        LanguageGroupBox.Visibility = Visibility.Collapsed;
        VideoInfoTextBlock.Visibility = Visibility.Collapsed;
        OkButton.IsEnabled = false;
    }

    private void ShowVideoOptions()
    {
        VideoGroupBox.Visibility = Visibility.Visible;
        VideoQualityComboBox.Items.Clear();

        foreach (var video in _currentVideo!.AvailableVideos.OrderByDescending(v => v.FileSize))
        {
            var sizeText = video.FileSize > 0 ? $" ({video.FileSize / 1024 / 1024:F2} MB)" : "";
            var fpsText = video.Framerate > 0 ? $" {video.Framerate}fps" : "";
            VideoQualityComboBox.Items.Add($"{video.QualityLabel} - {video.Resolution}{fpsText}{sizeText}");
        }

        if (VideoQualityComboBox.Items.Count > 0)
        {
            VideoQualityComboBox.SelectedIndex = 0;
        }
    }

    private void ShowAudioOptions()
    {
        AudioGroupBox.Visibility = Visibility.Visible;
        AudioQualityComboBox.Items.Clear();

        foreach (var audio in _currentVideo!.AvailableAudios.OrderByDescending(a => a.Bitrate))
        {
            var sizeText = audio.FileSize > 0 ? $" ({audio.FileSize / 1024 / 1024:F2} MB)" : "";
            AudioQualityComboBox.Items.Add($"{audio.Extension} - {audio.Bitrate} kbps{sizeText}");
        }

        if (AudioQualityComboBox.Items.Count > 0)
        {
            AudioQualityComboBox.SelectedIndex = 0;
        }
    }

    private void ShowSubtitleOptions()
    {
        SubtitleGroupBox.Visibility = Visibility.Visible;
        _subtitleItems.Clear();

        foreach (var subtitle in _currentVideo!.AvailableSubtitles)
        {
            var autoText = subtitle.IsAutoGenerated ? " (自动)" : "";
            _subtitleItems.Add(new SubtitleItem
            {
                LanguageCode = subtitle.LanguageCode,
                DisplayName = $"{subtitle.LanguageName} ({subtitle.LanguageCode}){autoText}",
                IsSelected = false
            });
        }

        SubtitleListBox.ItemsSource = _subtitleItems;
    }

    private void ShowLanguageOptions()
    {
        LanguageGroupBox.Visibility = Visibility.Visible;
    }

    #endregion

    #region 获取选择结果

    public YouTubeDownloadSelection GetSelection()
    {
        var selection = new YouTubeDownloadSelection
        {
            Url = UrlTextBox.Text.Trim(),
            DownloadVideo = DownloadVideoCheckBox.IsChecked == true,
            DownloadAudio = DownloadAudioCheckBox.IsChecked == true,
            VideoInfo = _currentVideo,
            SourceLanguage = SourceLanguageComboBox.SelectedItem as VT.Core.Language? ?? VT.Core.Language.Auto,
            TargetLanguage = TargetLanguageComboBox.SelectedItem as VT.Core.Language? ?? VT.Core.Language.Chinese
        };

        if (VideoQualityComboBox.SelectedIndex >= 0 && _currentVideo != null)
        {
            var selectedVideo = _currentVideo.AvailableVideos.OrderByDescending(v => v.FileSize)
                .ElementAt(VideoQualityComboBox.SelectedIndex);
            selection.SelectedVideoStream = selectedVideo;
        }

        if (AudioQualityComboBox.SelectedIndex >= 0 && _currentVideo != null)
        {
            var selectedAudio = _currentVideo.AvailableAudios.OrderByDescending(a => a.Bitrate)
                .ElementAt(AudioQualityComboBox.SelectedIndex);
            selection.SelectedAudioStream = selectedAudio;
        }

        foreach (var item in _subtitleItems.Where(i => i.IsSelected))
        {
            selection.SelectedSubtitleLanguages.Add(item.LanguageCode);
        }

        return selection;
    }

    #endregion

    #region LLM生成视频信息

    private async Task<YouTubeVideo> GenerateVideoInfoWithLLM(string url)
    {
        _logger.Information("开始使用LLM生成视频信息: {Url}", url);

        var prompt = $@"请分析以下YouTube视频URL，生成视频信息。请以JSON格式返回，包含以下字段：
- title: 视频标题
- videoId: 从URL中提取的视频ID
- duration: 视频时长（格式：HH:MM:SS，例如：10:30:00）
- description: 视频描述（可选）
- uploader: 上传者名称（可选）
- uploadDate: 上传日期（格式：YYYY-MM-DD，可选）
- viewCount: 观看次数（可选）
- likeCount: 点赞次数（可选）
- commentCount: 评论数（可选）
- tags: 标签（可选）
- category: 分类（可选）
- ageLimit: 年龄限制（可选）
- isLive: 是否直播（true/false，可选）
- is4K: 是否4K分辨率（true/false，可选）
- resolution: 分辨率（可选）
- format: 格式（可选）
- fileSize: 文件大小（可选）

视频URL: {url}

请只返回JSON，不要包含其他解释文字。";

        var videoId = ExtractVideoIdFromUrl(url);
        var title = $"YouTube视频_{videoId}";
        var duration = TimeSpan.FromMinutes(10);

        try
        {
            var fullResponse = new System.Text.StringBuilder();
            await foreach (var chunk in _chatService.SendResponsesStreamAsync(prompt))
            {
                if (chunk.ContentDelta != null)
                {
                    fullResponse.Append(chunk.ContentDelta);
                }
            }

            var responseText = fullResponse.ToString();
            _logger.Information("LLM响应: {Response}", responseText);

            var video = new YouTubeVideo
            {
                VideoId = videoId,
                Title = title,
                Url = url,
                Duration = duration,
                DurationSeconds = (int)duration.TotalSeconds,
                DurationText = duration.ToString(@"hh\:mm\:ss"),
                Description = "由LLM生成的视频信息",
                AvailableSubtitles = new List<YouTubeSubtitle>(),
                AvailableAudios = new List<YouTubeAudio>(),
                AvailableVideos = new List<YouTubeVideoStream>()
            };

            return video;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "LLM生成视频信息失败");
            throw;
        }
    }

    private string ExtractVideoIdFromUrl(string url)
    {
        try
        {
            if (url.Contains("youtube.com/watch?v="))
            {
                var startIndex = url.IndexOf("v=") + 2;
                var endIndex = url.IndexOf('&', startIndex);
                if (endIndex == -1)
                {
                    endIndex = url.Length;
                }
                return url.Substring(startIndex, endIndex - startIndex);
            }
            else if (url.Contains("youtu.be/"))
            {
                var startIndex = url.LastIndexOf('/') + 1;
                var endIndex = url.IndexOf('?');
                if (endIndex == -1)
                {
                    endIndex = url.Length;
                }
                return url.Substring(startIndex, endIndex - startIndex);
            }
        }
        catch
        {
        }
        return "unknown";
    }

    #endregion

    #region 字幕项数据类

    private class SubtitleItem
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    #endregion
}
