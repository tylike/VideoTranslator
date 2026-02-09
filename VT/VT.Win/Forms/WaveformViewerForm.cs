using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;

namespace VT.Win.Forms;

public partial class WaveformViewerForm : Form
{
    private Panel waveformPanel;
    private VScrollBar vScrollBar;
    private Label titleLabel;
    private List<WaveformData> waveformDataList = new();
    private const int WaveformHeight = 360;
    private const int WaveformSpacing = 10;
    private const int SamplesPerPixel = 100;
    private string[]? wavFilePaths;

    public WaveformViewerForm()
    {
        InitializeComponent();
    }

    public WaveformViewerForm(string[] wavFilePaths) : this()
    {
        this.wavFilePaths = wavFilePaths;
        LoadWaveforms();
    }

    private void InitializeComponent()
    {
        Text = "音频波形查看器";
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 600);

        titleLabel = new Label
        {
            Text = "音频片段波形",
            Location = new Point(10, 10),
            Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold),
            AutoSize = true
        };

        waveformPanel = new Panel
        {
            Location = new Point(10, 40),
            Width = ClientSize.Width - 30,
            Height = ClientSize.Height - 60,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoScroll = true,
            BackColor = Color.White
        };

        vScrollBar = new VScrollBar
        {
            Location = new Point(ClientSize.Width - 20, 40),
            Height = ClientSize.Height - 60,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            Minimum = 0,
            Maximum = 0,
            LargeChange = 100,
            SmallChange = 10,
            Visible = false
        };

        Controls.Add(titleLabel);
        Controls.Add(waveformPanel);
        Controls.Add(vScrollBar);
    }

    private void LoadWaveforms()
    {
        try
        {
            if (wavFilePaths == null || wavFilePaths.Length == 0)
            {
                MessageBox.Show("没有提供音频文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var wavFiles = wavFilePaths
                .Where(f => File.Exists(f))
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            waveformDataList.Clear();

            foreach (var wavFile in wavFiles)
            {
                var waveformData = LoadWaveformData(wavFile);
                if (waveformData != null)
                {
                    waveformDataList.Add(waveformData);
                }
            }

            DrawWaveforms();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载波形失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private WaveformData? LoadWaveformData(string filePath)
    {
        try
        {
            using var audioFile = new AudioFileReader(filePath);
            var samples = new List<float>();
            var buffer = new float[audioFile.WaveFormat.SampleRate * 10];
            int samplesRead;

            while ((samplesRead = audioFile.Read(buffer, 0, buffer.Length)) > 0)
            {
                samples.AddRange(buffer.Take(samplesRead));
            }

            return new WaveformData
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Samples = samples.ToArray(),
                Duration = audioFile.TotalTime,
                SampleRate = audioFile.WaveFormat.SampleRate
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载文件失败 {filePath}: {ex.Message}");
            return null;
        }
    }

    private void DrawWaveforms()
    {
        waveformPanel.Controls.Clear();

        int yOffset = 0;

        foreach (var data in waveformDataList)
        {
            var waveformControl = new WaveformControl(data)
            {
                Location = new Point(0, yOffset),
                Width = waveformPanel.ClientSize.Width - 20,
                Height = WaveformHeight
            };

            waveformPanel.Controls.Add(waveformControl);
            yOffset += WaveformHeight + WaveformSpacing;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (waveformPanel != null)
        {
            waveformPanel.Width = ClientSize.Width - 30;
            waveformPanel.Height = ClientSize.Height - 60;
            vScrollBar.Height = ClientSize.Height - 60;
            vScrollBar.Location = new Point(ClientSize.Width - 20, 40);
            DrawWaveforms();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        foreach (Control control in waveformPanel.Controls)
        {
            if (control is WaveformControl waveformControl)
            {
                waveformControl.Dispose();
            }
        }
        base.OnFormClosing(e);
    }
}
