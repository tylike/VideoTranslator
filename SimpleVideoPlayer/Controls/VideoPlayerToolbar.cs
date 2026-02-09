using LibVLCSharp.Shared;
using SimpleVideoPlayer.Extensions;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleVideoPlayer.Controls
{
    public class VideoPlayerToolbar : Panel
    {
        #region 基础字段

        private MediaPlayer _mediaPlayer;
        private VideoPlayer _videoPlayer;
        private SubtitleSelector _subtitleSelector;
        private VideoPlayControl _playbackButtonsControl;
        private VideoPlayProgress _progressControl;
        private VolumeControl _volumeControl;
        private TableLayoutPanel _layoutPanel;

        #endregion

        #region 构造函数

        public VideoPlayerToolbar(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
            InitializeControl();
            InitializeSubControls();
            InitializeSubtitleControls();
        }

        #endregion

        #region 控件初始化

        private void InitializeControl()
        {
            Dock = DockStyle.Top;
            Height = 60;
            BackColor = Color.FromArgb(245, 245, 245);
            Padding = new Padding(0);

            _layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));

            _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Add(_layoutPanel);
        }

        private void InitializeSubControls()
        {
            _playbackButtonsControl = new VideoPlayControl(_mediaPlayer,this.VideoPlayer);
            _playbackButtonsControl.Dock = DockStyle.Fill;
            _layoutPanel.Controls.Add(_playbackButtonsControl, 0, 0);

            _progressControl = new VideoPlayProgress(_mediaPlayer);
            _progressControl.Dock = DockStyle.Fill;
            _layoutPanel.Controls.Add(_progressControl, 1, 0);

            _volumeControl = new VolumeControl(_mediaPlayer);
            _volumeControl.Dock = DockStyle.Fill;
            _layoutPanel.Controls.Add(_volumeControl, 2, 0);
        }

        private void InitializeSubtitleControls()
        {
            _subtitleSelector = new SubtitleSelector(this._mediaPlayer,this.VideoPlayer);            
            _subtitleSelector.Dock = DockStyle.Fill;
            _layoutPanel.Controls.Add(_subtitleSelector, 3, 0);
        }

        #endregion

        #region 属性

        public VideoPlayer VideoPlayer
        {
            get => _videoPlayer;
            set
            {
                _videoPlayer = value;
                if (_playbackButtonsControl != null)
                {
                    _playbackButtonsControl.VideoPlayer = value;
                }
            }
        }
        
        public SubtitleSelector SubtitleController => _subtitleSelector;
        public VideoPlayControl PlayController => _playbackButtonsControl;
        public VolumeControl VolumeController => _volumeControl;
        public VideoPlayProgress PlayProgress => _progressControl;
        #endregion

        #region 方法

        public void SetVolume(int volume)
        {
            _volumeControl?.SetVolume(volume);
        }

        public void UpdateVolumeControls()
        {
            _volumeControl?.UpdateVolumeControls();
        }

        public void UpdateVolumeLabel()
        {
            _volumeControl?.UpdateVolumeLabel();
        }

        #endregion

        #region 资源清理

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _playbackButtonsControl?.Dispose();
                _progressControl?.Dispose();
                _volumeControl?.Dispose();
                _subtitleSelector?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
