using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Common.Logging;

namespace VideoPlayer
{
    public partial class VideoDownloadWindow : Window
    {
        #region 字段

        private VideoDownloader _downloader;
        private string _currentVideoUrl;
        private string _currentOutputPath;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<VideoDownloadWindow>();

        #endregion

        #region 构造函数

        public VideoDownloadWindow()
        {
            InitializeComponent();
            InitializeDownloader();
        }

        #endregion

        #region 初始化方法

        private void InitializeDownloader()
        {
            _downloader = new VideoDownloader();
            _downloader.ProgressChanged += OnDownloadProgressChanged;
            _downloader.DownloadCompleted += OnDownloadCompleted;
            _downloader.DownloadError += OnDownloadError;
        }

        #endregion

        #region 下载方法

        public async Task<bool> DownloadYouTubeVideoAsync(string videoUrl, string outputPath)
        {
            _currentVideoUrl = videoUrl;
            _currentOutputPath = outputPath;

            Logger.Information("准备下载 YouTube 视频: {VideoUrl}", videoUrl);

            bool success = await _downloader.DownloadVideoAsync(videoUrl, outputPath);

            return success;
        }

        #endregion

        #region 事件处理

        private void OnDownloadProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnDownloadProgressChanged(sender, e));
                return;
            }

            Logger.Debug("下载进度: {Percentage}%", e.Percentage);
            
            progressBar.Value = e.Percentage;
            progressLabel.Content = $"{e.Percentage}%";
        }

        private void OnDownloadCompleted(object sender, DownloadCompletedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnDownloadCompleted(sender, e));
                return;
            }

            Logger.Information("下载完成: {OutputPath}", e.OutputPath);

            MessageBox.Show(
                $"视频下载成功！\n保存位置: {e.OutputPath}",
                "下载完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            downloadButton.IsEnabled = true;
            cancelButton.IsEnabled = false;
        }

        private void OnDownloadError(object sender, DownloadErrorEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnDownloadError(sender, e));
                return;
            }

            Logger.Error("下载错误: {ErrorMessage}", e.ErrorMessage);

            MessageBox.Show(
                $"下载失败: {e.ErrorMessage}",
                "下载错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            downloadButton.IsEnabled = true;
            cancelButton.IsEnabled = false;
        }

        #endregion

        #region 按钮事件

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "MP4 文件 (*.mp4)|*.mp4|MKV 文件 (*.mkv)|*.mkv|AVI 文件 (*.avi)|*.avi|所有文件 (*.*)|*.*",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                outputPathTextBox.Text = saveFileDialog.FileName;
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = urlTextBox.Text.Trim();
            string outputPath = outputPathTextBox.Text.Trim();

            if (string.IsNullOrEmpty(videoUrl))
            {
                MessageBox.Show("请输入视频 URL", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                MessageBox.Show("请选择保存路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            downloadButton.IsEnabled = false;
            cancelButton.IsEnabled = true;
            progressBar.Value = 0;
            progressLabel.Content = "0%";

            try
            {
                await DownloadYouTubeVideoAsync(videoUrl, outputPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "下载异常");
                MessageBox.Show($"下载异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                downloadButton.IsEnabled = true;
                cancelButton.IsEnabled = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Information("取消下载");
            _downloader.CancelDownload();
        }

        #endregion

        #region 资源释放

        protected override void OnClosed(EventArgs e)
        {
            _downloader?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}