using System.Windows;
using VT.Module.BusinessObjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WpfWindow = System.Windows.Window;
using VideoTranslator.Services;
using VT.Core;

namespace VideoEditor.Windows;

public partial class EditProjectWindow : WpfWindow
{
    #region 字段

    private readonly ILogger _logger = LoggerService.ForContext<EditProjectWindow>();
    private readonly VideoProject _videoProject;
    private readonly IObjectSpace _objectSpace;

    #endregion

    #region 属性

    public VideoProject VideoProject => _videoProject;

    #endregion

    #region 构造函数

    public EditProjectWindow(VideoProject videoProject, IObjectSpace objectSpace)
    {
        InitializeComponent();
        _videoProject = videoProject;
        _objectSpace = objectSpace;
        InitializeControls();
    }

    #endregion

    #region 初始化

    private void InitializeControls()
    {
        try
        {
            _logger.Information("初始化编辑项目窗口控件");

            InitializeLanguageComboBoxes();
            InitializeTranslationStyleComboBox();
            InitializeEncodingPresetComboBox();
            InitializeSubtitleTypeComboBoxes();
            LoadProjectData();

            _logger.Information("控件初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化控件失败");
            MessageBox.Show($"初始化控件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeLanguageComboBoxes()
    {
        try
        {
            _logger.Information("初始化语言选择选项");

            var languages = Enum.GetValues(typeof(Language)).Cast<Language>().ToList();

            SourceLanguageComboBox.ItemsSource = languages;
            TargetLanguageComboBox.ItemsSource = languages;

            _logger.Information("语言选项初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化语言选项失败");
        }
    }

    private void InitializeTranslationStyleComboBox()
    {
        try
        {
            _logger.Information("初始化翻译风格选项");

            var translationStyles = Enum.GetValues(typeof(TranslationStyle)).Cast<TranslationStyle>().ToList();

            TranslationStyleComboBox.ItemsSource = translationStyles;

            _logger.Information("翻译风格选项初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化翻译风格选项失败");
        }
    }

    private void InitializeEncodingPresetComboBox()
    {
        try
        {
            _logger.Information("初始化编码预设选项");

            var encodingPresets = Enum.GetValues(typeof(EncodingPreset)).Cast<EncodingPreset>().ToList();

            EncodingPresetComboBox.ItemsSource = encodingPresets;

            _logger.Information("编码预设选项初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化编码预设选项失败");
        }
    }

    private void InitializeSubtitleTypeComboBoxes()
    {
        try
        {
            _logger.Information("初始化字幕类型选项");

            var subtitleTypes = Enum.GetValues(typeof(SubtitleType)).Cast<SubtitleType>().ToList();

            SourceSubtitleTypeComboBox.ItemsSource = subtitleTypes;
            TargetSubtitleTypeComboBox.ItemsSource = subtitleTypes;

            _logger.Information("字幕类型选项初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化字幕类型选项失败");
        }
    }

    private void LoadProjectData()
    {
        try
        {
            _logger.Information("加载项目数据");

            ProjectNameTextBox.Text = _videoProject.ProjectName ?? string.Empty;
            SourceLanguageComboBox.SelectedItem = _videoProject.SourceLanguage;
            TargetLanguageComboBox.SelectedItem = _videoProject.TargetLanguage;
            TranslationStyleComboBox.SelectedItem = _videoProject.TranslationStyle;
            CustomSystemPromptTextBox.Text = _videoProject.CustomSystemPrompt ?? string.Empty;
            UseGPUCheckBox.IsChecked = _videoProject.UseGPU;
            EncodingPresetComboBox.SelectedItem = _videoProject.EncodingPreset;
            FastSubtitleRenderingCheckBox.IsChecked = _videoProject.FastSubtitleRendering;

            WriteSourceSubtitleCheckBox.IsChecked = _videoProject.WriteSourceSubtitle;
            SourceSubtitleTypeComboBox.SelectedItem = _videoProject.SourceSubtitleType;
            WriteTargetSubtitleCheckBox.IsChecked = _videoProject.WriteTargetSubtitle;
            TargetSubtitleTypeComboBox.SelectedItem = _videoProject.TargetSubtitleType;

            ProjectPathTextBox.Text = _videoProject.ProjectPath ?? string.Empty;
            SourceVideoPathTextBox.Text = _videoProject.SourceVideoPath ?? string.Empty;
            SourceAudioPathTextBox.Text = _videoProject.SourceAudioPath ?? string.Empty;
            SourceMutedVideoPathTextBox.Text = _videoProject.SourceMutedVideoPath ?? string.Empty;
            SourceVocalAudioPathTextBox.Text = _videoProject.SourceVocalAudioPath ?? string.Empty;
            SourceBackgroundAudioPathTextBox.Text = _videoProject.SourceBackgroundAudioPath ?? string.Empty;
            SourceSubtitlePathTextBox.Text = _videoProject.SourceSubtitlePath ?? string.Empty;
            TranslatedSubtitlePathTextBox.Text = _videoProject.TranslatedSubtitlePath ?? string.Empty;
            OutputVideoPathTextBox.Text = _videoProject.OutputVideoPath ?? string.Empty;

            _logger.Information("项目数据加载完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载项目数据失败");
        }
    }

    #endregion

    #region 按钮事件

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("用户点击保存按钮");

            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                MessageBox.Show("请输入项目名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ProjectNameTextBox.Focus();
                return;
            }

            SaveProjectData();
            _objectSpace.CommitChanges();

            _logger.Information("项目信息保存成功");
            DialogResult = true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存项目信息失败");
            MessageBox.Show($"保存项目信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("用户取消了编辑");
        DialogResult = false;
    }

    #endregion

    #region 项目数据保存

    private void SaveProjectData()
    {
        try
        {
            _logger.Information("保存项目数据");

            _videoProject.ProjectName = ProjectNameTextBox.Text.Trim();
            _videoProject.SourceLanguage = SourceLanguageComboBox.SelectedItem as Language? ?? (global::VT.Core.Language.Auto);
            _videoProject.TargetLanguage = TargetLanguageComboBox.SelectedItem as Language? ?? global::VT.Core.Language.Chinese;
            _videoProject.TranslationStyle = TranslationStyleComboBox.SelectedItem as TranslationStyle? ?? TranslationStyle.Professional;
            _videoProject.CustomSystemPrompt = CustomSystemPromptTextBox.Text.Trim();
            _videoProject.UseGPU = UseGPUCheckBox.IsChecked ?? false;
            _videoProject.EncodingPreset = EncodingPresetComboBox.SelectedItem as EncodingPreset? ?? EncodingPreset.Fast;
            _videoProject.FastSubtitleRendering = FastSubtitleRenderingCheckBox.IsChecked ?? false;

            _videoProject.WriteSourceSubtitle = WriteSourceSubtitleCheckBox.IsChecked ?? false;
            _videoProject.SourceSubtitleType = SourceSubtitleTypeComboBox.SelectedItem as SubtitleType? ?? SubtitleType.SoftSubtitle;
            _videoProject.WriteTargetSubtitle = WriteTargetSubtitleCheckBox.IsChecked ?? false;
            _videoProject.TargetSubtitleType = TargetSubtitleTypeComboBox.SelectedItem as SubtitleType? ?? SubtitleType.SoftSubtitle;

            _videoProject.ProjectPath = ProjectPathTextBox.Text.Trim();
            _videoProject.SourceVideoPath = SourceVideoPathTextBox.Text.Trim();
            _videoProject.SourceAudioPath = SourceAudioPathTextBox.Text.Trim();
            _videoProject.SourceMutedVideoPath = SourceMutedVideoPathTextBox.Text.Trim();
            _videoProject.SourceVocalAudioPath = SourceVocalAudioPathTextBox.Text.Trim();
            _videoProject.SourceBackgroundAudioPath = SourceBackgroundAudioPathTextBox.Text.Trim();
            _videoProject.SourceSubtitlePath = SourceSubtitlePathTextBox.Text.Trim();
            _videoProject.TranslatedSubtitlePath = TranslatedSubtitlePathTextBox.Text.Trim();
            _videoProject.OutputVideoPath = OutputVideoPathTextBox.Text.Trim();

            _logger.Information("项目数据保存完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存项目数据时发生错误");
            throw;
        }
    }

    #endregion
}
