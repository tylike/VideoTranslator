using System.Windows;
using VideoTranslator.Services;
using VideoEditor.Models;
using VideoTranslator.Utils;

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

            var durationSeconds = (decimal)_videoInfo.Duration; //ParseDurationToSeconds(_videoInfo.Duration);
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

}
