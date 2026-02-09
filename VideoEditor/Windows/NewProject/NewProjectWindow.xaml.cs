﻿﻿using System.Windows;
using System.Windows.Controls;
using VT.Module.BusinessObjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Serilog;
using System.IO;
using VideoEditor;
using VT.Core;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Module;
using SystemWindow = System.Windows.Window;

namespace VideoEditor.Windows;

public partial class NewProjectWindow : SystemWindow
{
    #region 字段

    private readonly ILogger _logger = LoggerService.ForContext<NewProjectWindow>();
    private string? _videoFilePath;
    private string? _audioFilePath;
    private string? _subtitleFilePath;
    private string? _translatedSubtitleFilePath;
    private VideoProject? _createdProject;
    private readonly IObjectSpace _objectSpace;
    private IServiceProvider _serviceProvider => ServiceHelper.GetMainServiceProvider();

    #endregion

    #region 属性

    public VideoProject? CreatedProject => _createdProject;

    #endregion

    #region 构造函数

    public NewProjectWindow(IObjectSpace objectSpace)
    {
        InitializeComponent();
        _objectSpace = objectSpace;
        InitializeDefaultValues();
        InitializeLanguageComboBoxes();
    }

    #endregion

    #region 按钮事件

    private async void OkButton_Click(object sender, RoutedEventArgs e)
    {
        await HandleOkButtonClick();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户取消了项目创建");
        DialogResult = false;
        Close();
    }

    #endregion

    #region 初始化

    private void InitializeDefaultValues()
    {
        try
        {
            _logger.Information("初始化新建项目窗口默认值");

            _projectNameTextBox.Text = $"新建视频项目_{DateTime.Now:yyyyMMdd_HHmmss}";

            var defaultProjectPath = GetDefaultProjectPath();
            _projectPathTextBox.Text = defaultProjectPath;

            _logger.Information("默认值初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化默认值失败");
        }
    }

    private void InitializeLanguageComboBoxes()
    {
        try
        {
            _logger.Information("初始化语言选择选项");

            var languages = Enum.GetValues(typeof(Language)).Cast<VT.Core.Language>().ToList();

            _sourceLanguageComboBox.ItemsSource = languages;
            _sourceLanguageComboBox.SelectedIndex = languages.IndexOf(VT.Core.Language.Auto);

            _targetLanguageComboBox.ItemsSource = languages;
            _targetLanguageComboBox.SelectedIndex = languages.IndexOf(VT.Core.Language.Chinese);

            _logger.Information("语言选项初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化语言选项失败");
        }
    }

    private string GetDefaultProjectPath()
    {
        try
        {
            var defaultPath = "D:\\VideoTranslator\\videoProjects";
            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
            }
            return defaultPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取默认项目路径失败");
            return "D:\\VideoTranslator\\videoProjects";
        }
    }

    #endregion

    #region 按钮事件

    private void InputMode_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("用户切换输入模式");

            if (_videoPlusAudioMode?.IsChecked == true)
            {
                _audioFileGrid.Visibility = Visibility.Visible;
                _audioInfoTextBlock.Visibility = Visibility.Visible;
                _logger.Information("切换到无声视频+音频模式");
            }
            else if (_videoOnlyMode?.IsChecked == true)
            {
                _audioFileGrid?.Visibility = Visibility.Collapsed;
                _audioInfoTextBlock?.Visibility = Visibility.Collapsed;
                _logger.Information("切换到仅视频模式");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "切换输入模式失败");
        }
    }
    
    string _muteVideoPath;

    private async void BrowseVideoButton_Click(object sender, RoutedEventArgs e)
    {
        var result = await BrowseFileAsync("视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|所有文件|*.*", "选择视频文件");
        if (result != null)
        {
            _videoFilePath = result;
            _videoPathTextBox.Text = _videoFilePath;
            UpdateFileInfo(_videoInfoTextBlock, _videoFilePath);
            
            _logger.Information("用户选择视频文件: {FilePath}", _videoFilePath);

            if (this._videoOnlyMode.IsChecked == true)
            {
                var rst = await ServiceHelper.FFmpeg.GetVideoStreamInfo(_videoFilePath);
                if (!rst.HasAudio)
                {
                    MessageBox.Show($"此视频[{_videoFilePath}]没有音频!自动切换到 静音视频+音频模式!");
                    _videoOnlyMode.IsChecked = false;
                    _videoPlusAudioMode.IsChecked = true;
                    _muteVideoPath = _videoFilePath;                  
                }
                else
                {
                    this._audioFilePath = Path.Combine(Path.GetDirectoryName(_videoFilePath), "audio_source.wav");
                    this._muteVideoPath = Path.Combine(Path.GetDirectoryName(_videoFilePath), $"mute_video_source{Path.GetExtension(_videoFilePath)}");
                    await ServiceHelper.FFmpeg.SeparateMainVideoAndAudio(_videoFilePath, _muteVideoPath, this._audioFilePath);
                    this._audioPathTextBox.Text = _audioFilePath;
                    var sourceLang = await ServiceHelper.Whisper.DetectLanguageAsync(this._audioFilePath);
                    SetLanguageBasedOnDetection(sourceLang);
                }
            }            
        }
    }

    private async void BrowseAudioButton_Click(object sender, RoutedEventArgs e)
    {
        var result = await BrowseFileAsync("音频文件|*.mp3;*.wav;*.flac;*.aac;*.m4a;*.ogg;*.wma|所有文件|*.*", "选择音频文件");
        if (result != null)
        {
            _audioFilePath = result;
            _audioPathTextBox.Text = _audioFilePath;
            UpdateFileInfo(_audioInfoTextBlock, _audioFilePath);
            _logger.Information("用户选择音频文件: {FilePath}", _audioFilePath);
            var sl = await ServiceHelper.Whisper.DetectLanguageAsync(_audioFilePath);
            SetLanguageBasedOnDetection(sl);
        }
    }

    private async void BrowseSubtitleButton_Click(object sender, RoutedEventArgs e)
    {
        var result = await BrowseFileAsync("字幕文件|*.srt;*.ass;*.ssa;*.vtt;*.sub|所有文件|*.*", "选择字幕文件");
        if (result != null)
        {
            _subtitleFilePath = result;
            _subtitlePathTextBox.Text = _subtitleFilePath;
            UpdateFileInfo(_subtitleInfoTextBlock, _subtitleFilePath);
            _logger.Information("用户选择字幕文件: {FilePath}", _subtitleFilePath);
        }
    }

    private async void BrowseTranslatedSubtitleButton_Click(object sender, RoutedEventArgs e)
    {
        var result = await BrowseFileAsync("字幕文件|*.srt;*.ass;*.ssa;*.vtt;*.sub|所有文件|*.*", "选择目标字幕文件");
        if (result != null)
        {
            _translatedSubtitleFilePath = result;
            _translatedSubtitlePathTextBox.Text = _translatedSubtitleFilePath;
            UpdateFileInfo(_translatedSubtitleInfoTextBlock, _translatedSubtitleFilePath);
            _logger.Information("用户选择目标字幕文件: {FilePath}", _translatedSubtitleFilePath);
        }
    }

    private async Task<string?> BrowseFileAsync(string filter, string title)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                RestoreDirectory = true
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "浏览文件失败");
            MessageBox.Show($"浏览文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private async Task HandleOkButtonClick()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_projectNameTextBox.Text))
            {
                MessageBox.Show("请输入项目名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                _projectNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_videoFilePath))
            {
                MessageBox.Show("请选择视频文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!File.Exists(_videoFilePath))
            {
                MessageBox.Show("选择的视频文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_videoPlusAudioMode.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(_audioFilePath))
                {
                    MessageBox.Show("请选择音频文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!File.Exists(_audioFilePath))
                {
                    MessageBox.Show("选择的音频文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var project = await CreateProject();
            if (project != null)
            {
                _createdProject = project;
                _logger.Information("项目创建成功: {ProjectName} (Oid: {Oid})", project.ProjectName, project.Oid);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("创建项目失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "创建项目失败");
            MessageBox.Show($"创建项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 项目创建

    private async Task<VideoProject?> CreateProject()
    {
        _logger.Information("开始创建项目: {ProjectName}", _projectNameTextBox.Text);

        #region 创建项目对象

        var project = await VideoProject.CreateProject(
            _objectSpace,
            _projectNameTextBox.Text,
            _videoFilePath,
            (VT.Core.Language)_sourceLanguageComboBox.SelectedItem,
            (VT.Core.Language)_targetLanguageComboBox.SelectedItem,
            _audioFilePath
        );

        if (project == null)
        {
            _logger.Error("创建项目对象失败");
            return null;
        }

        #endregion

        #region 保存项目

        _objectSpace.CommitChanges();
        _logger.Information("项目创建成功: {ProjectName} (Oid: {Oid})", project.ProjectName, project.Oid);

        #endregion

        return project;
    }

    #endregion

    #region 辅助方法

    private void SetLanguageBasedOnDetection(Language detectedLanguage)
    {
        _sourceLanguageComboBox.SelectedItem = detectedLanguage;
        if (detectedLanguage == VT.Core.Language.English)
        {
            _targetLanguageComboBox.SelectedItem = VT.Core.Language.Chinese;
        }
        else if (detectedLanguage == VT.Core.Language.Chinese)
        {
            _targetLanguageComboBox.SelectedItem = VT.Core.Language.English;
        }
        _logger.Information("语言检测完成，源语言: {SourceLanguage}，目标语言: {TargetLanguage}", 
            _sourceLanguageComboBox.SelectedItem, _targetLanguageComboBox.SelectedItem);
    }

    private void UpdateFileInfo(TextBlock textBlock, string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                textBlock.Text = "文件不存在";
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var sizeMB = fileInfo.Length / (1024.0 * 1024.0);
            var sizeKB = fileInfo.Length / 1024.0;
            var extension = fileInfo.Extension.ToUpper();

            string sizeText;
            if (sizeMB >= 1)
            {
                sizeText = $"文件大小: {sizeMB:F2} MB";
            }
            else
            {
                sizeText = $"文件大小: {sizeKB:F2} KB";
            }

            var infoText = $"{sizeText} | 格式: {extension} | 修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            textBlock.Text = infoText;
        }
        catch (Exception)
        {
            textBlock.Text = "无法获取文件信息";
        }
    }

    #endregion
}
