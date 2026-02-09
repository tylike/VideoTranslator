using System;
using System.Drawing;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace SimpleVideoPlayer.Controls
{
    public class VideoDisplayPanel : Panel
    {
        #region 字段

        private VideoView _videoView;
        private Label _subtitleLabel;
        private VideoPlayer _videoPlayer;

        #endregion

        #region 属性

        public VideoView VideoView => _videoView;

        #endregion

        #region 构造函数

        public VideoDisplayPanel()
        {
            InitializeControl();
            InitializeVideoView();
            InitializeSubtitleLabel();
        }

        #endregion

        #region 初始化方法

        private void InitializeControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.Black;
            Padding = new Padding(0, 0, 0, 20);
        }

        private void InitializeVideoView()
        {
            _videoView = new VideoView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            Controls.Add(_videoView);
            _videoView.SendToBack();
        }

        private void InitializeSubtitleLabel()
        {
            _subtitleLabel = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = false,
                Height = 120,
                BackColor = Color.FromArgb(100, 0, 0, 0),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 20, FontStyle.Bold),
                Padding = new Padding(20, 15, 20, 15),
                Visible = false
            };

            Controls.Add(_subtitleLabel);
            _subtitleLabel.BringToFront();
        }

        #endregion

    }
}
