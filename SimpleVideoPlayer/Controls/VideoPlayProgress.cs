using LibVLCSharp.Shared;
using SimpleVideoPlayer.Extensions;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Common.Logging;

namespace SimpleVideoPlayer.Controls
{
    public class VideoPlayProgress : Panel
    {
        #region 基础字段

        private MediaPlayer _mediaPlayer;
        private double _currentTime = 0;
        private bool _isDragging = false;
        private TableLayoutPanel _layoutPanel;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<VideoPlayProgress>();

        #endregion

        #region 构造函数
        SynchronizationContext _syncContext;
        public VideoPlayProgress(MediaPlayer mediaPlayer)
        {
            _syncContext = SynchronizationContext.Current;
            _mediaPlayer = mediaPlayer;
            InitializeControl();
            InitializeProgressBar();
            InitializeLabels();
            SubscribeToMediaPlayerEvents();
        }

        #endregion

        #region 控件初始化

        private void InitializeControl()
        {
            Dock = DockStyle.Fill;
            Height = 60;
            BackColor = Color.FromArgb(245, 245, 245);
            Padding = new Padding(0);

            _layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Add(_layoutPanel);
        }

        private void InitializeProgressBar()
        {
            ProgressBar = new TrackBar
            {
                Dock = DockStyle.Fill,
                Height = 30,
                Minimum = 0,
                Maximum = 1000,
                Value = 0,
                TickFrequency = 100,
                Margin = new Padding(5, 15, 5, 15)
            };

            ProgressBar.Scroll += OnProgressBarScroll;
            ProgressBar.MouseDown += (s, e) => { _isDragging = true; };
            ProgressBar.MouseUp += (s, e) => { _isDragging = false; };

            _layoutPanel.Controls.Add(ProgressBar, 0, 0);
        }

        private void InitializeLabels()
        {
            TimeLabel = new Label
            {
                Text = "未知",
                Height = 30,
                Margin = new Padding(2, 15, 5, 15),
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _layoutPanel.Controls.Add(TimeLabel, 1, 0);
        }

        #endregion

        #region 属性

        public Label TimeLabel { get; private set; }
        public TrackBar ProgressBar { get; private set; }

        #endregion

        #region 事件订阅

        private void SubscribeToMediaPlayerEvents()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.TimeChanged += OnMediaPlayerTimeChanged;
                _mediaPlayer.LengthChanged += OnMediaPlayerLengthChanged;
            }
        }

        private void UnsubscribeFromMediaPlayerEvents()
        {
            if (_mediaPlayer != null)
            {
                Logger.Debug("取消订阅 TimeChanged 事件");
                try
                {
                    _mediaPlayer.TimeChanged -= OnMediaPlayerTimeChanged;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "取消订阅 TimeChanged 事件失败");
                }

                Logger.Debug("取消订阅 LengthChanged 事件");
                try
                {
                    _mediaPlayer.LengthChanged -= OnMediaPlayerLengthChanged;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "取消订阅 LengthChanged 事件失败");
                }

                Logger.Debug("所有事件取消订阅完成");
            }
        }

        #endregion

        #region 事件处理

        private void OnMediaPlayerTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            _currentTime = e.Time / 1000.0;
            UpdateTimeDisplay();
        }

        private void OnMediaPlayerLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Logger.Debug("MediaPlayer LengthChanged 事件: {Length}ms", e.Length);
            UpdateTimeDisplay();
        }

        private void OnProgressBarScroll(object sender, EventArgs e)
        {
            Logger.Debug("进度条拖动: {Value}", ProgressBar.Value);
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                var position = ProgressBar.Value / 1000.0;
                _mediaPlayer.Position = (float)position;
            }
        }

        #endregion

        #region 方法

        private void UpdateTimeDisplay()
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            var currentTimeText = FormatTime(_currentTime);
            var totalDuration = _mediaPlayer != null ? _mediaPlayer.Length / 1000.0 : 0;
            var durationText = FormatTime(totalDuration);
            var displayText = $"{currentTimeText} / {durationText}";

            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() =>
                    {
                        if (!IsDisposed && !Disposing)
                        {
                            TimeLabel.Text = displayText;
                            UpdateProgressBar();
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
                    TimeLabel.Text = displayText;
                    UpdateProgressBar();
                }
            }
        }

        private void UpdateProgressBar()
        {
            if (ProgressBar != null && !_isDragging)
            {
                var totalDuration = _mediaPlayer != null ? _mediaPlayer.Length / 1000.0 : 0;
                if (totalDuration > 0)
                {
                    var progress = (int)((_currentTime / totalDuration) * 1000);
                    if (progress >= 0 && progress <= ProgressBar.Maximum)
                    {
                        ProgressBar.Value = progress;
                    }
                }
            }
        }

        private string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.TotalHours >= 1
                ? timeSpan.ToString(@"hh\:mm\:ss")
                : timeSpan.ToString(@"mm\:ss");
        }

        #endregion

        #region 资源清理

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnsubscribeFromMediaPlayerEvents();
            }
            base.Dispose(disposing);
        }

        #endregion

        public void SeekTo(double seconds)
        {
            SeekTo(seconds, false);
        }

        public void SeekTo(double seconds, bool autoPlay)
        {
            if (_mediaPlayer != null)
            {
                var state = _mediaPlayer.State;

                Logger.Debug("SeekTo: {Seconds}s, 自动播放: {AutoPlay}, 当前状态: {State}", seconds, autoPlay, state);

                if (state == VLCState.Stopped || state == VLCState.NothingSpecial || state == VLCState.Opening)
                {
                    if (autoPlay)
                    {
                        Logger.Debug("SeekTo: 状态为 {State}，先播放再定位", state);
                        _syncContext?.Post(_ =>
                        {
                            _mediaPlayer.Play();
                            var time = (long)(seconds * 1000);
                            _mediaPlayer.Time = time;
                            Logger.Debug("SeekTo: 已定位到 {Time}ms", time);
                        }, null);
                    }
                    else
                    {
                        Logger.Debug("SeekTo: 状态为 {State}，直接设置时间并停止", state);
                        _syncContext?.Post(_ =>
                        {
                            _mediaPlayer.Play();
                            var time = (long)(seconds * 1000);
                            _mediaPlayer.Time = time;
                            Logger.Debug("SeekTo: 已定位到 {Time}ms，停止播放", time);
                            _mediaPlayer.Stop();
                        }, null);
                    }
                }
                else
                {
                    var time = (long)(seconds * 1000);
                    _mediaPlayer.Time = time;
                    Logger.Debug("SeekTo: 已定位到 {Time}ms", time);
                }
            }
        }

        
    }
}
