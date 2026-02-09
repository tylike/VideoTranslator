using System;
using System.Windows;
using Microsoft.Win32;
using Common.Logging;

namespace VideoPlayer
{
    public partial class MainWindow : Window
    {
        #region 字段

        private VideoDownloadWindow _videoDownloadWindow;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<MainWindow>();

        #endregion

        #region 构造函数

        public MainWindow()
        {
            InitializeComponent();
            InitializeEventHandlers();
        }

        #endregion

        #region 初始化方法

        private void InitializeEventHandlers()
        {
            this.Closing += MainWindow_Closing;
        }

        #endregion

        #region 菜单事件处理

        private void OpenVideo_Click(object sender, RoutedEventArgs e)
        {
            Logger.Information("点击打开视频菜单");
            
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择视频文件",
                Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|所有文件|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Logger.Information("选择视频文件: {FileName}", openFileDialog.FileName);
                videoPlayerControl.LoadVideo(openFileDialog.FileName);
                statusText.Text = $"已加载: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
            }
        }

        private void DownloadVideo_Click(object sender, RoutedEventArgs e)
        {
            Logger.Information("点击下载视频菜单");
            
            if (_videoDownloadWindow == null || !_videoDownloadWindow.IsLoaded)
            {
                _videoDownloadWindow = new VideoDownloadWindow();
                _videoDownloadWindow.Show();
            }
            else
            {
                _videoDownloadWindow.Activate();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Logger.Information("点击退出菜单");
            this.Close();
        }

        #endregion

        #region 窗体事件

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Information("窗体关闭开始");
            try
            {
                Logger.Information("停止视频播放");
                if (videoPlayerControl != null)
                {
                    videoPlayerControl.StopPlayback();
                    
                    System.Threading.Thread.Sleep(500);
                    
                    Logger.Information("释放 VideoPlayerControl");
                    videoPlayerControl.Dispose();
                    videoPlayerControl = null;
                    Logger.Information("VideoPlayerControl 释放完成");
                }
                
                if (_videoDownloadWindow != null && _videoDownloadWindow.IsLoaded)
                {
                    Logger.Information("关闭 VideoDownloadWindow");
                    _videoDownloadWindow.Close();
                    _videoDownloadWindow = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "窗体关闭异常");
            }
            Logger.Information("窗体关闭完成");
        }

        #endregion
    }
}