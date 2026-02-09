using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using SimpleVideoPlayer.Controls;
using Common.Logging;

namespace SimpleVideoPlayer
{
    public class VideoPlayer : Control
    {
        #region 字段
        public LibVLC LibVLC => _libVLC;
        private VideoPlayerToolbar toolbar;
        private VideoDisplayPanel _videoDisplayPanel;
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _media;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<VideoPlayer>();

        public string CurrentVideoPath { get; set; }
        #endregion

        #region 构造函数和初始化

        public VideoPlayer()
        {
            BackColor = Color.FromArgb(240, 240, 240);
            Logger.Debug("构造函数开始");
            InitializeVLC();
            InitializeControls();
            Logger.Debug("构造函数完成");
        }

        private void InitializeVLC()
        {
            Logger.Debug("初始化 VLC 开始");
            _libVLC = new LibVLC(
                "--no-xlib",
                "--no-video-title-show",
                "--no-snapshot-preview",
                "--no-audio-time-stretch",
                "--sub-file=auto",
                "--subsdec-encoding=auto",
                "--sub-autodetect-file",
                "--sub-autodetect-fuzzy=2"
            );
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.TimeChanged += OnTimeChanged;
            _mediaPlayer.EnableHardwareDecoding = false;
            Logger.Debug("VLC 和 MediaPlayer 创建完成");
        }

        protected virtual void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {

        }
        public void SeekTo(double seconds)
        {
            toolbar.PlayProgress.SeekTo(seconds);
        }
        #endregion

        #region 控件初始化

        private void InitializeControls()
        {
            _videoDisplayPanel = new VideoDisplayPanel() { Dock = DockStyle.Fill };
            SetVideoView(_videoDisplayPanel.VideoView);

            toolbar = new VideoPlayerToolbar(_mediaPlayer) { Dock = DockStyle.Bottom };
            toolbar.VideoPlayer = this;

            this.Controls.Add(_videoDisplayPanel);
            this.Controls.Add(toolbar);
        }

        #endregion

        #region 视频视图和加载

        public void SetVideoView(VideoView videoView)
        {
            if (videoView != null)
            {
                videoView.MediaPlayer = _mediaPlayer;
            }
        }


        #endregion


        #region 资源释放

        private bool _isDisposing = false;

        public new void Dispose()
        {
            if (_isDisposing)
            {
                return;
            }
            _isDisposing = true;

            Logger.Debug("Dispose 开始");
            try
            {
                if (toolbar != null)
                {
                    Logger.Debug("释放 PlaybackControlPanel");
                    toolbar.Dispose();
                    toolbar = null;
                }

                if (_videoDisplayPanel != null)
                {
                    Logger.Debug("释放 VideoDisplayPanel");
                    _videoDisplayPanel.Dispose();
                    _videoDisplayPanel = null;
                }

                if (_mediaPlayer != null)
                {
                    Logger.Debug("MediaPlayer 状态: {State}, 是否播放中: {IsPlaying}", _mediaPlayer.State, _mediaPlayer.IsPlaying);
                    Logger.Debug("停止播放");
                    try
                    {
                        _mediaPlayer.Stop();
                        Logger.Debug("播放已停止");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "停止播放时异常");
                    }
                }

                if (_media != null)
                {
                    Logger.Debug("释放 Media");
                    _media.Dispose();
                    _media = null;
                }

                if (_mediaPlayer != null)
                {
                    Logger.Debug("释放 MediaPlayer");
                    _mediaPlayer.Dispose();
                    _mediaPlayer = null;
                }

                if (_libVLC != null)
                {
                    Logger.Debug("释放 LibVLC");
                    _libVLC.Dispose();
                    _libVLC = null;
                }
                Logger.Debug("Dispose 完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Dispose 异常");
            }
        }

        public void LoadVideo(string filename) => toolbar.PlayController.LoadVideo(filename);
        public void LoadSubtitle(string fileName) => toolbar.SubtitleController.LoadSubtitleFile(fileName);


        #endregion
    }
}
