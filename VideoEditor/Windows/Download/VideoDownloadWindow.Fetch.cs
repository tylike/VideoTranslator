using System.Windows;
using VideoTranslator.Services;
using VideoEditor.Models;
using VideoTranslator.Utils;
using System.IO;

namespace VideoEditor.Windows;

public partial class VideoDownloadWindow
{
    private async void FetchButton_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            _logger.Warning("用户未输入视频地址");
            MessageBox.Show("请输入视频地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _logger.Information("开始获取视频信息: {Url}", url);
        ShowLoading(true);
        HideOptions();

        try
        {
            _videoInfo = await YtDlpService.GetVideoInfoAsync(url, new Progress<string>(UpdateProgress));
            _logger.Information("成功获取视频信息: 标题={Title}, ID={Id}, 时长={Duration}",
                _videoInfo.Title, _videoInfo.ID, _videoInfo.Duration);

            var durationDisplay = _videoInfo.GetFormattedDuration();
            
            VideoInfoTextBlock.Text = $"标题: {_videoInfo.Title}\n" +
                                  $"上传者: {_videoInfo.Uploader}\n" +
                                  $"时长: {durationDisplay}\n" +
                                  $"观看次数: {_videoInfo.ViewCount}\n" +
                                  $"点赞数: {_videoInfo.LikeCount}";
            
            if (_videoInfo.CommentCount > 0)
            {
                VideoInfoTextBlock.Text += $"\n评论数: {_videoInfo.CommentCount}";
            }
            
            if (_videoInfo.AverageRating.HasValue)
            {
                VideoInfoTextBlock.Text += $"\n评分: {_videoInfo.AverageRating}";
            }
            
            if (_videoInfo.ChannelFollowerCount > 0)
            {
                VideoInfoTextBlock.Text += $"\n频道关注: {_videoInfo.ChannelFollowerCount:N0}";
            }
            
            VideoInfoTextBlock.Visibility = Visibility.Visible;

            ShowVideoOptions();
            ShowAudioOptions();
            ShowSubtitleOptions();

            DownloadButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取视频信息失败: {Url}", url);
            MessageBox.Show($"获取视频信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private async void QuickDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户点击一键下载按钮");

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
                #region 获取视频信息

                _videoInfo = await YtDlpService.GetVideoInfoAsync(url, new Progress<string>(UpdateProgress));
                _logger.Information("成功获取视频信息: 标题={Title}, ID={Id}", _videoInfo.Title, _videoInfo.ID);

                #endregion

                #region 保存视频信息

                SaveVideoInfo(downloadFolder);

                #endregion

                #region 一键下载高质量视频和音频

                var videoId = _videoInfo?.ID ?? "video";
                var videoPath = Path.Combine(downloadFolder, $"{videoId}_video.mkv");
                var audioPath = Path.Combine(downloadFolder, $"{videoId}_audio.mp3");

                _logger.Information("开始一键下载高质量视频和音频");

                #region 下载视频

                _logger.Information("开始下载视频: 使用最佳质量格式");
                try
                {
                    _downloadedVideoPath = await YtDlpService.DownloadVideoAsync(
                        url,
                        videoPath,
                        null,
                        new Progress<string>(UpdateProgress));

                    _logger.Information("视频下载完成: {Path}", _downloadedVideoPath);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "视频下载失败，继续下载音频");
                    _downloadedVideoPath = null;
                }

                #endregion

                #region 下载音频

                _logger.Information("开始下载音频: 使用最佳质量格式");
                try
                {
                    _downloadedAudioPath = await YtDlpService.DownloadAudioAsync(
                        url,
                        audioPath,
                        null,
                        new Progress<string>(UpdateProgress));

                    _logger.Information("音频下载完成: {Path}", _downloadedAudioPath);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "音频下载失败");
                    _downloadedAudioPath = null;
                }

                #endregion

                #region 验证下载结果

                if (string.IsNullOrEmpty(_downloadedVideoPath) && string.IsNullOrEmpty(_downloadedAudioPath))
                {
                    throw new InvalidOperationException("视频和音频都下载失败，请检查网络连接或视频地址");
                }

                #endregion

                #endregion

                _logger.Information("一键下载完成: {Folder}", downloadFolder);
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
                MessageBox.Show("一键下载完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "一键下载失败");
                MessageBox.Show($"一键下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "一键下载按钮点击失败");
            MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}
