using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Core;
using MessageType = VideoTranslator.Interfaces.MessageType;
using VT.Module;
namespace VideoEditor.Windows;

public partial class VoskTestWindow : Window
{
    #region Fields
    private readonly IFFmpegService _ffmpegService;
    private readonly IProgressService _progressService;
    private bool _isRecognizing = false;
    #endregion

    #region Constructor
    public VoskTestWindow()
    {
        InitializeComponent();
        _ffmpegService = ServiceHelper.GetService<IFFmpegService>();
        _progressService = ServiceHelper.GetService<IProgressService>();

        var app = ServiceHelper.AppSettings;
        this.ModelPathTextBox.Text = app.VoskModelPath;
        this.SpkModelPathTextBox.Text = app.VoskSpeakerModelPath;
        this.WavFilePathTextBox.Text = @"D:\VideoTranslator\videoProjects\25\source_audio.flac";

    }
    #endregion

    #region Event Handlers

    private void BrowseWav_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "WAV文件 (*.wav)|*.wav|所有文件 (*.*)|*.*",
            Title = "选择WAV音频文件"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            WavFilePathTextBox.Text = openFileDialog.FileName;
        }
    }

    private void BrowseModel_Click(object sender, RoutedEventArgs e)
    {
        var openFolderDialog = new OpenFolderDialog
        {
            Title = "选择语音识别模型目录"
        };

        if (openFolderDialog.ShowDialog() == true)
        {
            ModelPathTextBox.Text = openFolderDialog.FolderName;
        }
    }

    private void BrowseSpkModel_Click(object sender, RoutedEventArgs e)
    {
        var openFolderDialog = new OpenFolderDialog
        {
            Title = "选择说话人识别模型目录"
        };

        if (openFolderDialog.ShowDialog() == true)
        {
            SpkModelPathTextBox.Text = openFolderDialog.FolderName;
        }
    }

    private void ClearResult_Click(object sender, RoutedEventArgs e)
    {
        ResultTextBox.Clear();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }

    private async void StartRecognition_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecognizing)
        {
            return;
        }

        #region 验证输入
        if (string.IsNullOrWhiteSpace(WavFilePathTextBox.Text))
        {
            MessageBox.Show("请选择WAV音频文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelPathTextBox.Text))
        {
            MessageBox.Show("请选择语音识别模型目录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!System.IO.File.Exists(WavFilePathTextBox.Text))
        {
            MessageBox.Show("WAV音频文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!System.IO.Directory.Exists(ModelPathTextBox.Text))
        {
            MessageBox.Show("语音识别模型目录不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(SpkModelPathTextBox.Text) && !System.IO.Directory.Exists(SpkModelPathTextBox.Text))
        {
            MessageBox.Show("说话人识别模型目录不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        #endregion

        _isRecognizing = true;
        StartRecognitionButton.IsEnabled = false;
        StartRecognitionButton.Content = "识别中...";
        ResultTextBox.Clear();
        LogTextBox.Clear();

        try
        {
            #region 创建窗口进度服务
            #endregion

            #region 创建识别服务
            var settings = ServiceHelper.AppSettings;
            var voskService = new VoskRecognitionService();
            #endregion

            #region 执行识别
            var spkModelPath = string.IsNullOrWhiteSpace(SpkModelPathTextBox.Text) ? null : SpkModelPathTextBox.Text;
            var prompt = string.IsNullOrWhiteSpace(PromptTextBox.Text) ? null : PromptTextBox.Text;

            var subtitles = await voskService.RecognizeAsync(
                WavFilePathTextBox.Text,
                "zh",
                spkModelPath,
                prompt
            );
            #endregion

            #region 显示结果
            ResultTextBox.AppendText("\n========== 识别结果 ==========\n");
            foreach (var subtitle in subtitles)
            {
                ResultTextBox.AppendText($"[{subtitle.StartTime:hh\\:mm\\:ss\\.fff} - {subtitle.EndTime:hh\\:mm\\:ss\\.fff}] {subtitle.Text}\n");
            }
            ResultTextBox.AppendText($"\n共识别到 {subtitles.Count} 个片段\n");
            ResultTextBox.AppendText("==============================\n");
            ResultTextBox.ScrollToEnd();
            #endregion
        }
        catch (Exception ex)
        {
            ResultTextBox.AppendText($"\n[错误] 识别失败: {ex.Message}\n");
            ResultTextBox.ScrollToEnd();
            MessageBox.Show($"识别失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isRecognizing = false;

            StartRecognitionButton.IsEnabled = true;
            StartRecognitionButton.Content = "开始识别";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecognizing)
        {
            var result = MessageBox.Show("识别正在进行中，确定要关闭吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }
        }

        DialogResult = false;
        Close();
    }

    #endregion

    #region Inner Classes

    private class WindowProgressService : IProgressService, IDisposable
    {
        private readonly VoskTestWindow _window;

        public WindowProgressService(VoskTestWindow window)
        {
            _window = window;
        }

        public object? Application { get; set; }

        public void SetStatusMessage(string message, VideoTranslator.Interfaces.MessageType type = VideoTranslator.Interfaces.MessageType.Info, bool newline = true, bool log = true)
        {
            if (!log)
            {
                return;
            }

            _window.Dispatcher.Invoke(() =>
            {
                _window.LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{(newline ? "\n" : "")}");
                _window.LogTextBox.ScrollToEnd();
            });
        }

        public void ShowProgress(bool marquee = false)
        {
        }

        public void HideProgress()
        {
        }

        public void ResetProgress()
        {
        }

        public void Report()
        {
            Report("", MessageType.Info);
        }

        public void Report(string message, MessageType messageType = MessageType.Info)
        {
            SetStatusMessage(message, messageType);
        }

        public void Title(string message)
        {
            SetStatusMessage(message, MessageType.Title);
        }

        public void Success(string message)
        {
            SetStatusMessage(message, MessageType.Success);
        }

        public void Error(string message)
        {
            SetStatusMessage(message, MessageType.Error);
        }

        public void Warning(string message)
        {
            SetStatusMessage(message, MessageType.Warning);
        }

        public void Dispose()
        {
        }

        void IProgressService.ReportProgress(double value)
        {
        }

        void IProgressService.SetProgressMaxValue(double value)
        {
        }
    }

    #endregion
}
