using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VT.Module;

namespace VT.Win.Services;

internal class YouTubeUrlInputDialog : Form
{
    private TextBox urlTextBox;
    private Button fetchButton;
    private Button okButton;
    private Button cancelButton;
    private Button selectAllButton;
    private Button deselectAllButton;
    private Label promptLabel;
    private Label videoInfoLabel;
    private GroupBox videoGroupBox;
    private GroupBox audioGroupBox;
    private GroupBox subtitleGroupBox;
    private CheckedListBox subtitleListBox;
    private ComboBox audioQualityComboBox;
    private ComboBox videoQualityComboBox;
    private CheckBox downloadVideoCheckBox;
    private CheckBox downloadAudioCheckBox;
    private Panel loadingPanel;
    private Label loadingLabel;
    private ProgressBar loadingProgressBar;

    private YouTubeVideo? _currentVideo;
    private readonly IYouTubeService _youtubeService;

    public YouTubeUrlInputDialog()
    {
        _youtubeService = new YouTubeService();
        InitializeComponents();
        this.Load += YouTubeUrlInputDialog_Load;
    }

    private void YouTubeUrlInputDialog_Load(object? sender, EventArgs e)
    {
        this.BringToFront();
        this.Activate();
        this.Focus();
    }

    private void InitializeComponents()
    {
        Text = "导入YouTube视频";
        Width = 700;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        this.BringToFront();

        promptLabel = new Label
        {
            Text = "请输入YouTube视频地址:",
            Location = new System.Drawing.Point(20, 20),
            Width = 450
        };

        urlTextBox = new TextBox
        {
            Location = new System.Drawing.Point(20, 50),
            Width = 480
        };

        fetchButton = new Button
        {
            Text = "获取资源",
            Location = new System.Drawing.Point(510, 50),
            Width = 80
        };
        fetchButton.Click += FetchButton_Click;

        videoInfoLabel = new Label
        {
            Text = "",
            Location = new System.Drawing.Point(20, 85),
            Width = 650,
            Height = 40,
            Visible = false
        };

        videoGroupBox = new GroupBox
        {
            Text = "视频",
            Location = new System.Drawing.Point(20, 135),
            Width = 650,
            Height = 50,
            Visible = false
        };

        downloadVideoCheckBox = new CheckBox
        {
            Text = "下载视频",
            Location = new System.Drawing.Point(10, 20),
            Width = 150,
            Checked = true
        };

        videoQualityComboBox = new ComboBox
        {
            Location = new System.Drawing.Point(170, 18),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        videoGroupBox.Controls.AddRange(new Control[] { downloadVideoCheckBox, videoQualityComboBox });

        audioGroupBox = new GroupBox
        {
            Text = "音频",
            Location = new System.Drawing.Point(20, 195),
            Width = 650,
            Height = 50,
            Visible = false
        };

        downloadAudioCheckBox = new CheckBox
        {
            Text = "下载音频",
            Location = new System.Drawing.Point(10, 20),
            Width = 150,
            Checked = true
        };

        audioQualityComboBox = new ComboBox
        {
            Location = new System.Drawing.Point(170, 18),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        audioGroupBox.Controls.AddRange(new Control[] { downloadAudioCheckBox, audioQualityComboBox });

        subtitleGroupBox = new GroupBox
        {
            Text = "字幕",
            Location = new System.Drawing.Point(20, 255),
            Width = 650,
            Height = 220,
            Visible = false
        };

        subtitleListBox = new CheckedListBox
        {
            Location = new System.Drawing.Point(10, 25),
            Width = 630,
            Height = 150,
            CheckOnClick = true
        };

        selectAllButton = new Button
        {
            Text = "全选",
            Location = new System.Drawing.Point(10, 180),
            Width = 80
        };
        selectAllButton.Click += SelectAllButton_Click;

        deselectAllButton = new Button
        {
            Text = "全否",
            Location = new System.Drawing.Point(100, 180),
            Width = 80
        };
        deselectAllButton.Click += DeselectAllButton_Click;

        subtitleGroupBox.Controls.AddRange(new Control[] { subtitleListBox, selectAllButton, deselectAllButton });

        okButton = new Button
        {
            Text = "确定",
            Location = new System.Drawing.Point(470, 500),
            Width = 80,
            DialogResult = DialogResult.OK,
            Enabled = false
        };

        cancelButton = new Button
        {
            Text = "取消",
            Location = new System.Drawing.Point(560, 500),
            Width = 80,
            DialogResult = DialogResult.Cancel
        };

        loadingPanel = new Panel
        {
            Location = new System.Drawing.Point(20, 85),
            Width = 650,
            Height = 400,
            Visible = false,
            BackColor = System.Drawing.Color.White
        };

        loadingLabel = new Label
        {
            Text = "正在获取视频信息...",
            Location = new System.Drawing.Point(250, 180),
            Width = 200,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        };

        loadingProgressBar = new ProgressBar
        {
            Location = new System.Drawing.Point(200, 210),
            Width = 250,
            Style = ProgressBarStyle.Marquee
        };

        loadingPanel.Controls.AddRange(new Control[] { loadingLabel, loadingProgressBar });

        Controls.AddRange(new Control[]
        {
            promptLabel,
            urlTextBox,
            fetchButton,
            videoInfoLabel,
            videoGroupBox,
            audioGroupBox,
            subtitleGroupBox,
            okButton,
            cancelButton,
            loadingPanel
        });

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private async void FetchButton_Click(object? sender, EventArgs e)
    {
        var url = urlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("请输入YouTube视频地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        loadingPanel.Visible = true;
        videoGroupBox.Visible = false;
        audioGroupBox.Visible = false;
        subtitleGroupBox.Visible = false;
        videoInfoLabel.Visible = false;
        okButton.Enabled = false;
        fetchButton.Enabled = false;

        try
        {
            _currentVideo = await _youtubeService.GetVideoInfoAsync(url);

            videoInfoLabel.Text = $"标题: {_currentVideo.Title}\n时长: {_currentVideo.Duration:hh\\:mm\\:ss}";
            videoInfoLabel.Visible = true;

            videoGroupBox.Visible = true;
            videoQualityComboBox.Items.Clear();
            foreach (var video in _currentVideo.AvailableVideos.OrderByDescending(v => v.FileSize))
            {
                var sizeText = video.FileSize > 0 ? $" ({video.FileSize / 1024 / 1024:F2} MB)" : "";
                var fpsText = video.Framerate > 0 ? $" {video.Framerate}fps" : "";
                videoQualityComboBox.Items.Add($"{video.QualityLabel} - {video.Resolution}{fpsText}{sizeText}");
            }
            if (videoQualityComboBox.Items.Count > 0)
            {
                videoQualityComboBox.SelectedIndex = 0;
            }

            audioGroupBox.Visible = true;
            audioQualityComboBox.Items.Clear();
            foreach (var audio in _currentVideo.AvailableAudios.OrderByDescending(a => a.Bitrate))
            {
                var sizeText = audio.FileSize > 0 ? $" ({audio.FileSize / 1024 / 1024:F2} MB)" : "";
                audioQualityComboBox.Items.Add($"{audio.Extension} - {audio.Bitrate} kbps{sizeText}");
            }
            if (audioQualityComboBox.Items.Count > 0)
            {
                audioQualityComboBox.SelectedIndex = 0;
            }

            subtitleGroupBox.Visible = true;
            subtitleListBox.Items.Clear();
            foreach (var subtitle in _currentVideo.AvailableSubtitles)
            {
                var autoText = subtitle.IsAutoGenerated ? " (自动)" : "";
                subtitleListBox.Items.Add($"{subtitle.LanguageName} ({subtitle.LanguageCode}){autoText}");
            }

            okButton.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"获取视频信息失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadingPanel.Visible = false;
            fetchButton.Enabled = true;
        }
    }

    private void SelectAllButton_Click(object? sender, EventArgs e)
    {
        for (int i = 0; i < subtitleListBox.Items.Count; i++)
        {
            subtitleListBox.SetItemChecked(i, true);
        }
    }

    private void DeselectAllButton_Click(object? sender, EventArgs e)
    {
        for (int i = 0; i < subtitleListBox.Items.Count; i++)
        {
            subtitleListBox.SetItemChecked(i, false);
        }
    }

    public YouTubeDownloadSelection GetSelection()
    {
        var selection = new YouTubeDownloadSelection
        {
            Url = urlTextBox.Text.Trim(),
            DownloadVideo = downloadVideoCheckBox.Checked,
            DownloadAudio = downloadAudioCheckBox.Checked,
            VideoInfo = _currentVideo
        };

        if (videoQualityComboBox.SelectedIndex >= 0 && _currentVideo != null)
        {
            var selectedVideo = _currentVideo.AvailableVideos.OrderByDescending(v => v.FileSize)
                .ElementAt(videoQualityComboBox.SelectedIndex);
            selection.SelectedVideoStream = selectedVideo;
        }

        if (audioQualityComboBox.SelectedIndex >= 0 && _currentVideo != null)
        {
            var selectedAudio = _currentVideo.AvailableAudios.OrderByDescending(a => a.Bitrate)
                .ElementAt(audioQualityComboBox.SelectedIndex);
            selection.SelectedAudioStream = selectedAudio;
        }

        for (int i = 0; i < subtitleListBox.CheckedItems.Count; i++)
        {
            var index = subtitleListBox.CheckedIndices[i];
            if (_currentVideo != null && index < _currentVideo.AvailableSubtitles.Count)
            {
                selection.SelectedSubtitleLanguages.Add(_currentVideo.AvailableSubtitles[index].LanguageCode);
            }
        }

        return selection;
    }
}
