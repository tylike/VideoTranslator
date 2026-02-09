using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Serilog;
using TimeLine.Services;
using TimeLine.Windows;
using VT.Module.BusinessObjects;

namespace TimeLine.Controls;

public partial class TrackHeaderControl : ContentControl
{
    #region 字段

    private TrackInfo? _trackInfo;
    private string _title = string.Empty;
    private double _height = 50;

    private readonly ILogger _logger = LoggerService.ForContext<TrackHeaderControl>();

    #endregion

    #region 构造函数

    public TrackHeaderControl()
    {
        InitializeComponent();
        _logger.Debug("[TrackHeaderControl] 构造函数调用");
    }

    #endregion

    #region 依赖属性

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TrackHeaderControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(TrackHeaderControl), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty BorderBrushProperty =
        DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(TrackHeaderControl), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(204, 204, 204))));

    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(TrackHeaderControl), new PropertyMetadata(new Thickness(0, 0, 0, 1)));

    #endregion

    #region 公共属性

    public TrackInfo? TrackInfo
    {
        get => _trackInfo;
        set
        {
            if (_trackInfo != value)
            {
                _trackInfo?.Changed -= _trackInfo_Changed;
                _logger.Debug("[TrackHeaderControl] TrackInfo 属性变更: Old={Old}, New={New}", _trackInfo?.Title, value?.Title);
                _trackInfo = value;
                _trackInfo?.Changed += _trackInfo_Changed;
                _trackInfo?.Validate();
            }
        }
    }

    private void _trackInfo_Changed(object sender, DevExpress.Xpo.ObjectChangeEventArgs e)
    {
        if(e.PropertyName == nameof(TrackInfo.ErrorMessage))
        {
            var errorIcon = this.错误图标;
            if (errorIcon != null)
            {
                if (string.IsNullOrEmpty(_trackInfo.ErrorMessage))
                {
                    errorIcon.Visibility = Visibility.Collapsed;
                }
                else
                {
                    errorIcon.Visibility = Visibility.Visible;
                    errorIcon.ToolTip = _trackInfo.ErrorMessage;
                }
            }
        }
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set
        {
            if (_title != value)
            {
                _logger.Debug("[TrackHeaderControl] Title 属性变更: Old={Old}, New={New}", _title, value);
                _title = value;
                SetValue(TitleProperty, value);
                var titleTextBlock = this.标题文本块;
                if (titleTextBlock != null)
                {
                    titleTextBlock.Text = value;
                }
            }
        }
    }

    #endregion

    #region 事件

    public event EventHandler<TrackInfo>? MenuButtonClick;

    #endregion

    #region 事件处理

    private void OnMenuButtonClick(object sender, RoutedEventArgs e)
    {
        _logger.Debug("[TrackHeaderControl] 菜单按钮点击: Title={Title}", _title);
        MenuButtonClick?.Invoke(this, _trackInfo);
        e.Handled = true;
    }

    private void OnEditButtonClick(object sender, RoutedEventArgs e)
    {
        _logger.Debug("[TrackHeaderControl] 编辑按钮点击: Title={Title}", _title);
        
        if (_trackInfo == null)
        {
            _logger.Warning("[TrackHeaderControl] TrackInfo 为空，无法打开编辑窗口");
            return;
        }
        
        try
        {
            var editWindow = new SegmentsEditWindow(_trackInfo);
            var result = editWindow.ShowDialog();
            
            if (result == true)
            {
                _logger.Debug("[TrackHeaderControl] 片段编辑完成");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[TrackHeaderControl] 打开片段编辑窗口失败");
            MessageBox.Show($"打开编辑窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        e.Handled = true;
    }

    #endregion
}
