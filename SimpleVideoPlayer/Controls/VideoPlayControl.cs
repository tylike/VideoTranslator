using LibVLCSharp.Shared;
using SimpleVideoPlayer.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using Common.Logging;

namespace SimpleVideoPlayer.Controls
{
    public class VideoPlayControl : FlowLayoutPanel
    {
        #region 基础字段

        private MediaPlayer _mediaPlayer;
        private VideoPlayer _videoPlayer;
        private bool _isSeeking = false;
        private Timer _seekTimer;
        private float _targetPosition;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<VideoPlayControl>();

        #endregion

        #region 构造函数
        VideoPlayer rootUI;
        public VideoPlayControl(MediaPlayer mediaPlayer, VideoPlayer ui)
        {
            this.rootUI = ui;
            _mediaPlayer = mediaPlayer;
            InitializeControl();
            InitializeButtons();
            SubscribeToMediaPlayerEvents();
            InitializeButtonEvents();
        }

        #endregion

        #region 控件初始化

        private void InitializeControl()
        {
            FlowDirection = FlowDirection.LeftToRight;
            WrapContents = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Height = 60;
            BackColor = Color.FromArgb(245, 245, 245);
            Padding = new Padding(5, 0, 5, 0);
        }

        private void InitializeButtons()
        {
            LoadVideoButton = CreateButton("加载视频", 80, Color.FromArgb(76, 175, 80));
            PlayPauseButton = CreateButton("播放", 80, Color.FromArgb(33, 150, 243));
            StopButton = CreateButton("停止", 60, Color.FromArgb(220, 53, 69));

            Controls.Add(LoadVideoButton);
            Controls.Add(PlayPauseButton);
            Controls.Add(StopButton);

        }

        private Button CreateButton(string text, int width, Color backColor)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 15, 2, 15),
                Font = new Font("Microsoft YaHei", 9),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        #endregion

        #region 属性

        public Button LoadVideoButton { get; private set; }
        public Button PlayPauseButton { get; private set; }
        public Button StopButton { get; private set; }       

        public VideoPlayer VideoPlayer
        {
            get => _videoPlayer;
            set => _videoPlayer = value;
        }

        #endregion

        #region 事件订阅

        private void SubscribeToMediaPlayerEvents()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Playing += OnMediaPlayerPlaying;
                _mediaPlayer.Paused += OnMediaPlayerPaused;
                _mediaPlayer.Stopped += OnMediaPlayerStopped;
                _mediaPlayer.EndReached += OnMediaPlayerEndReached;
            }
        }

        private void UnsubscribeFromMediaPlayerEvents()
        {
            if (_mediaPlayer != null)
            {
                Logger.Debug("取消订阅 Playing 事件");
                try
                {
                    _mediaPlayer.Playing -= OnMediaPlayerPlaying;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "取消订阅 Playing 事件失败");
                }

                Logger.Debug("取消订阅 Paused 事件");
                try
                {
                    _mediaPlayer.Paused -= OnMediaPlayerPaused;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "取消订阅 Paused 事件失败");
                }

                Logger.Debug("取消订阅 Stopped 事件");
                try
                {
                    _mediaPlayer.Stopped -= OnMediaPlayerStopped;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "取消订阅 Stopped 事件失败");
                }

                Logger.Debug("取消订阅 EndReached 事件");
                try
                {
                    _mediaPlayer.EndReached -= OnMediaPlayerEndReached;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "取消订阅 EndReached 事件失败");
                }

                Logger.Debug("所有事件取消订阅完成");
            }
        }

        #endregion

        #region 事件处理

        private void OnMediaPlayerPlaying(object sender, EventArgs e)
        {
            Logger.Debug("MediaPlayer Playing 事件");
            SetPlayingState(true);
        }

        private void OnMediaPlayerPaused(object sender, EventArgs e)
        {
            Logger.Debug("MediaPlayer Paused 事件");
            SetPlayingState(false);
        }

        private void OnMediaPlayerStopped(object sender, EventArgs e)
        {
            Logger.Debug("MediaPlayer Stopped 事件");
            SetPlayingState(false);
        }

        private void OnMediaPlayerEndReached(object sender, EventArgs e)
        {
            Logger.Debug("MediaPlayer EndReached 事件");
            SetPlayingState(false);
        }

        #endregion

        #region 按钮事件

        private void InitializeButtonEvents()
        {
            LoadVideoButton.Click += this.OnLoadVideoClicked;
            PlayPauseButton.Click += OnPlayPauseClicked;
            StopButton.Click += OnStopClicked;
        }

        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            Logger.Debug("播放/暂停按钮点击");
            if (_mediaPlayer != null)
            {
                Logger.Debug("当前状态: {State}", _mediaPlayer.State);
                if (_mediaPlayer.State == VLCState.Playing)
                {
                    Logger.Debug("执行暂停");
                    _mediaPlayer.Pause();
                }
                else
                {
                    Logger.Debug("执行播放");
                    _mediaPlayer.Play();
                }
            }
            else
            {
                Logger.Warning("MediaPlayer 为空");
            }
        }

        private void OnStopClicked(object sender, EventArgs e)
        {
            Logger.Debug("停止按钮点击");
            _mediaPlayer?.Stop();
        }

        #endregion

        #region 方法

        private void SetPlayingState(bool isPlaying)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() =>
                    {
                        if (!IsDisposed && !Disposing)
                        {
                            PlayPauseButton.Text = isPlaying ? "暂停" : "播放";
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                }
            }
            else
            {
                if (!IsDisposed && !Disposing)
                {
                    PlayPauseButton.Text = isPlaying ? "暂停" : "播放";
                }
            }
        }

        #endregion

        #region 资源清理

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnsubscribeFromMediaPlayerEvents();
                _seekTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        private void OnLoadVideoClicked(object sender, EventArgs e)
        {
            if (sender == this) return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv|所有文件|*.*",
                Title = "选择视频文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadVideo(openFileDialog.FileName);
            }
        }

        public void LoadVideo(string path)
        {
            Logger.Debug("LoadVideo 开始: {Path}", path);
            VideoPlayer.CurrentVideoPath = path;
            var _media = new Media(VideoPlayer.LibVLC, path, FromType.FromPath);
            if (IsHandleCreated)
            {
                try
                {
                    _mediaPlayer.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "LoadVideo Stop 异常");
                }
            }

            _mediaPlayer.Media = _media;
            Logger.Debug("LoadVideo 完成");
        }
    }
}
