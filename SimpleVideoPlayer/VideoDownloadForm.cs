using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleVideoPlayer
{
    public partial class VideoDownloadForm : Form
    {
        #region 字段

        private VideoDownloader _downloader;
        private string _currentVideoUrl;
        private string _currentOutputPath;

        #endregion

        #region 构造函数

        public VideoDownloadForm()
        {
            InitializeComponent();
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

            Debug.WriteLine($"[VideoDownloadForm] 准备下载 YouTube 视频: {videoUrl}");

            bool success = await _downloader.DownloadVideoAsync(videoUrl, outputPath);

            return success;
        }

        #endregion

        #region 事件处理

        private void OnDownloadProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDownloadProgressChanged(sender, e)));
                return;
            }

            Debug.WriteLine($"[VideoDownloadForm] 下载进度: {e.Percentage}%");
            
            progressBarDownload.Value = e.Percentage;
            labelProgress.Text = $"{e.Percentage}%";
        }

        private void OnDownloadCompleted(object sender, DownloadCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDownloadCompleted(sender, e)));
                return;
            }

            Debug.WriteLine($"[VideoDownloadForm] 下载完成: {e.OutputPath}");

            MessageBox.Show(
                $"视频下载成功！\n保存位置: {e.OutputPath}",
                "下载完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            buttonDownload.Enabled = true;
            buttonCancel.Enabled = false;
        }

        private void OnDownloadError(object sender, DownloadErrorEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDownloadError(sender, e)));
                return;
            }

            Debug.WriteLine($"[VideoDownloadForm] 下载错误: {e.ErrorMessage}");

            MessageBox.Show(
                $"下载失败: {e.ErrorMessage}",
                "下载错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            buttonDownload.Enabled = true;
            buttonCancel.Enabled = false;
        }

        #endregion

        #region 按钮事件

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "MP4 文件 (*.mp4)|*.mp4|MKV 文件 (*.mkv)|*.mkv|AVI 文件 (*.avi)|*.avi|所有文件 (*.*)|*.*",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxOutputPath.Text = saveFileDialog.FileName;
            }
        }

        private async void buttonDownload_Click(object sender, EventArgs e)
        {
            string videoUrl = textBoxUrl.Text.Trim();
            string outputPath = textBoxOutputPath.Text.Trim();

            if (string.IsNullOrEmpty(videoUrl))
            {
                MessageBox.Show("请输入视频 URL", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                MessageBox.Show("请选择保存路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            buttonDownload.Enabled = false;
            buttonCancel.Enabled = true;
            progressBarDownload.Value = 0;
            labelProgress.Text = "0%";

            try
            {
                await DownloadYouTubeVideoAsync(videoUrl, outputPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoDownloadForm] 下载异常: {ex.Message}");
                Debug.WriteLine($"[VideoDownloadForm] 异常堆栈: {ex.StackTrace}");
                MessageBox.Show($"下载异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                buttonDownload.Enabled = true;
                buttonCancel.Enabled = false;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("[VideoDownloadForm] 取消下载");
            _downloader.CancelDownload();
        }

        #endregion

        #region 资源释放



        #endregion

        #region 窗体设计器生成的代码

        private System.ComponentModel.IContainer components = null;
        private Label labelUrl;
        private TextBox textBoxUrl;
        private Label labelOutputPath;
        private TextBox textBoxOutputPath;
        private Button buttonBrowse;
        private ProgressBar progressBarDownload;
        private Label labelProgress;
        private Button buttonDownload;
        private Button buttonCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing)
            {
                _downloader?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelUrl = new System.Windows.Forms.Label();
            this.textBoxUrl = new System.Windows.Forms.TextBox();
            this.labelOutputPath = new System.Windows.Forms.Label();
            this.textBoxOutputPath = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.progressBarDownload = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.buttonDownload = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            
            this.labelUrl.AutoSize = true;
            this.labelUrl.Location = new System.Drawing.Point(12, 15);
            this.labelUrl.Name = "labelUrl";
            this.labelUrl.Size = new System.Drawing.Size(68, 12);
            this.labelUrl.TabIndex = 0;
            this.labelUrl.Text = "视频 URL:";
            
            this.textBoxUrl.Location = new System.Drawing.Point(86, 12);
            this.textBoxUrl.Name = "textBoxUrl";
            this.textBoxUrl.Size = new System.Drawing.Size(400, 21);
            this.textBoxUrl.TabIndex = 1;
            this.textBoxUrl.Text = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            
            this.labelOutputPath.AutoSize = true;
            this.labelOutputPath.Location = new System.Drawing.Point(12, 48);
            this.labelOutputPath.Name = "labelOutputPath";
            this.labelOutputPath.Size = new System.Drawing.Size(68, 12);
            this.labelOutputPath.TabIndex = 2;
            this.labelOutputPath.Text = "保存路径:";
            
            this.textBoxOutputPath.Location = new System.Drawing.Point(86, 45);
            this.textBoxOutputPath.Name = "textBoxOutputPath";
            this.textBoxOutputPath.Size = new System.Drawing.Size(400, 21);
            this.textBoxOutputPath.TabIndex = 3;
            this.textBoxOutputPath.Text = @"C:\Videos\downloaded_video.mp4";
            
            this.buttonBrowse.Location = new System.Drawing.Point(492, 43);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.TabIndex = 4;
            this.buttonBrowse.Text = "浏览...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            
            this.progressBarDownload.Location = new System.Drawing.Point(86, 80);
            this.progressBarDownload.Name = "progressBarDownload";
            this.progressBarDownload.Size = new System.Drawing.Size(400, 23);
            this.progressBarDownload.TabIndex = 5;
            
            this.labelProgress.AutoSize = true;
            this.labelProgress.Location = new System.Drawing.Point(492, 85);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(23, 12);
            this.labelProgress.TabIndex = 6;
            this.labelProgress.Text = "0%";
            
            this.buttonDownload.Location = new System.Drawing.Point(86, 120);
            this.buttonDownload.Name = "buttonDownload";
            this.buttonDownload.Size = new System.Drawing.Size(100, 30);
            this.buttonDownload.TabIndex = 7;
            this.buttonDownload.Text = "开始下载";
            this.buttonDownload.UseVisualStyleBackColor = true;
            this.buttonDownload.Click += new System.EventHandler(this.buttonDownload_Click);
            
            this.buttonCancel.Location = new System.Drawing.Point(200, 120);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 30);
            this.buttonCancel.TabIndex = 8;
            this.buttonCancel.Text = "取消下载";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Enabled = false;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 170);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonDownload);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.progressBarDownload);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.textBoxOutputPath);
            this.Controls.Add(this.labelOutputPath);
            this.Controls.Add(this.textBoxUrl);
            this.Controls.Add(this.labelUrl);
            this.Name = "VideoDownloadForm";
            this.Text = "视频下载器";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
