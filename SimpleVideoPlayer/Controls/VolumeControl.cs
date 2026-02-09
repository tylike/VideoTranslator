using LibVLCSharp.Shared;
using SimpleVideoPlayer.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Common.Logging;

namespace SimpleVideoPlayer.Controls
{
    public class VolumeControl : Panel
    {
        #region 基础字段

        private MediaPlayer _mediaPlayer;
        private bool _isUpdatingVolume = false;
        private int _lastVolume;
        private TableLayoutPanel _layoutPanel;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<VolumeControl>();

        #endregion

        #region 构造函数

        public VolumeControl(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
            InitializeControl();
            InitializeVolumeControls();
            SubscribeToMediaPlayerEvents();
            InitializeButtonEvents();
            UpdateVolumeControls();
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
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Add(_layoutPanel);
        }

        private void InitializeVolumeControls()
        {
            MuteButton = CreateButton("静音", 60, Color.FromArgb(156, 39, 176));
            _layoutPanel.Controls.Add(MuteButton, 0, 0);

            VolumeLabel = new Label
            {
                Text = "音量",
                Height = 30,
                Margin = new Padding(2, 15, 5, 15),
                Font = new Font("Microsoft YaHei", 9),
                BackColor = Color.FromArgb(245, 245, 245),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _layoutPanel.Controls.Add(VolumeLabel, 1, 0);

            VolumeBar = new TrackBar
            {
                Dock = DockStyle.Fill,
                Height = 30,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                TickFrequency = 10,
                Margin = new Padding(5, 15, 5, 15)
            };
            VolumeBar.Scroll += OnVolumeBarScroll;
            _layoutPanel.Controls.Add(VolumeBar, 2, 0);
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

        public Button MuteButton { get; private set; }
        public TrackBar VolumeBar { get; private set; }
        public Label VolumeLabel { get; private set; }

        #endregion

        #region 事件订阅

        private void SubscribeToMediaPlayerEvents()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;
            }
        }

        private void UnsubscribeFromMediaPlayerEvents()
        {
            if (_mediaPlayer != null)
            {
                Logger.Debug("取消订阅 VolumeChanged 事件");
                try
                {
                    _mediaPlayer.VolumeChanged -= OnMediaPlayerVolumeChanged;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "取消订阅 VolumeChanged 事件失败");
                }

                Logger.Debug("所有事件取消订阅完成");
            }
        }

        #endregion

        #region 事件处理

        private void OnMediaPlayerVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            try
            {
                if (IsHandleCreated)
                {
                    this.Invoke(() =>
                    {
                        if (!IsDisposed && !Disposing)
                        {
                            var v = e.Volume.ConvertToControlVolumeValue();
                            if (v != VolumeBar.Value)
                            {
                                Debug.WriteLine($"OnMediaPlayerVolumeChanged:{v},{VolumeBar.Value}");
                                VolumeBar.Value = v;
                            }
                            if (!Equals(VolumeLabel.Tag, v))
                            {
                                VolumeLabel.Tag = v;
                                VolumeLabel.Text = $"音量: {v}";
                            }

                            if (v == 0)
                            {
                                MuteButton.Text = "取消静音";
                            }
                            else
                            {
                                MuteButton.Text = "静音";
                            }
                        }
                    });
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void OnVolumeBarScroll(object sender, EventArgs e)
        {
            if (_isUpdatingVolume)
            {
                return;
            }

            Logger.Debug("音量调节: {Volume}", VolumeBar.Value);

            if (_mediaPlayer.Volume != VolumeBar.Value)
            {
                _mediaPlayer.Volume = VolumeBar.Value;
                Logger.Debug("音量已设置: {Volume}", VolumeBar.Value);
            }
        }

        private void InitializeButtonEvents()
        {
            MuteButton.Click += OnMuteClicked;
        }

        private void OnMuteClicked(object sender, EventArgs e)
        {
            Logger.Debug("静音按钮点击");
            if (_mediaPlayer.Volume == 0)
            {
                Logger.Debug("取消静音");
                SetVolume(_lastVolume);
            }
            else
            {
                Logger.Debug("静音");
                _lastVolume = _mediaPlayer.Volume;
                SetVolume(0);
            }
        }

        #endregion

        #region 方法

        public void SetVolume(int volume)
        {
            _mediaPlayer.Volume = volume;
        }

        public void UpdateVolumeControls()
        {
            if (VolumeLabel != null && VolumeBar != null && _mediaPlayer != null)
            {
                var volume = _mediaPlayer.Volume;

                VolumeLabel.Text = volume.ToString();
                VolumeBar.Value = volume;

                if (volume == 0)
                {
                    MuteButton.Text = "取消";
                }
                else
                {
                    MuteButton.Text = "静音";
                }
            }
        }

        public void UpdateVolumeLabel()
        {
            UpdateVolumeControls();
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
    }
}
