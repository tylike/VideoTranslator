using System;
using System.Drawing;
using System.Windows.Forms;
using Common.Logging;
using Serilog;

namespace SimpleVideoPlayer
{
    public class MainForm : Form
    {
        #region 字段

        private VideoPlayer _videoPlayer;
        private ToolStrip _toolStrip;
        private ToolStripButton _buttonDownload;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<MainForm>();

        #endregion

        #region 构造函数

        public MainForm()
        {
            InitializeForm();
            InitializeControls();
        }

        #endregion

        #region 初始化方法

        private void InitializeForm()
        {
            Text = "Simple Video Player";
            Size = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(240, 240, 240);
            MinimumSize = new Size(800, 600);
        }

        private void InitializeControls()
        {
            InitializeToolStrip();
            InitializeVideoPlayer();
        }

        private void InitializeToolStrip()
        {
            _toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = new ToolStripProfessionalRenderer()
            };

            _buttonDownload = new ToolStripButton
            {
                Text = "下载视频",
                Image = null,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            _buttonDownload.Click += ButtonDownload_Click;

            _toolStrip.Items.Add(_buttonDownload);

            Controls.Add(_toolStrip);
        }

        private void InitializeVideoPlayer()
        {
            _videoPlayer = new VideoPlayer
            {
                Dock = DockStyle.Fill
            };
            _videoPlayer.BringToFront();
            Controls.Add(_videoPlayer);
        }

        #endregion

        #region 按钮事件

        private void ButtonDownload_Click(object sender, EventArgs e)
        {
            Logger.Information("点击下载视频按钮");
            
            var downloadForm = new VideoDownloadForm();
            downloadForm.Show();
        }

        #endregion

        

        #region 窗体事件

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Logger.Information("窗体关闭开始");
            try
            {
                Logger.Debug("释放 VideoPlayer");
                _videoPlayer?.Dispose();
                _videoPlayer = null;
                Logger.Debug("VideoPlayer 释放完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "窗体关闭异常");
            }
            Logger.Debug("调用基类 OnFormClosing");
            base.OnFormClosing(e);
            Logger.Information("窗体关闭完成");
        }

        #endregion
    }
}
