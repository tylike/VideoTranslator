﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Serilog;
using VT.Module.BusinessObjects;
using TimeLine.Services;
using System.IO;
using VideoTranslator.Interfaces;
using VT.Core;
using VT.Module;

namespace TimeLine.Controls;

public partial class SimpleSegmentControl : Button
{
    #region 字段

    private Clip? _clip;
    private double _zoomFactor = 1.0;
    private double _totalDuration = 0.0;
    private bool _canPlay = false;
    private IAudioPlayerService? _audioPlayerService;
    private VideoEditorContext? _viewModel;
    private readonly ClipContextMenuFactory _contextMenuFactory;
    private const double SamplesPerSecond = 100.0;

    private readonly ILogger _logger = LoggerService.ForContext<SimpleSegmentControl>();

    #region UI控件缓存

    private TextBlock? _textBlock;
    private TextBlock? _speedTextBlock;
    private Rectangle? _overDurationBackground;
    private Border? _pendingBorder;
    private Border? _negativeWidthIndicator;

    #endregion

    #region 节流机制

    private DispatcherTimer? _throttleTimer;
    private const int ThrottleDelayMs = 16;

    #endregion

    #endregion

    #region 构造函数

    public SimpleSegmentControl()
    {
        InitializeComponent();
        
        Click += OnButtonClick;
        MouseEnter += OnButtonMouseEnter;
        MouseLeave += OnButtonMouseLeave;
        Loaded += OnControlLoaded;
        MouseRightButtonUp += OnRightButtonUp;
        InitializeWaveformEvents();
        InitializeToolTip();
        CacheUIControls();
        InitializeThrottleTimer();
        _contextMenuFactory = new ClipContextMenuFactory();
        
        _logger.Debug(" 构造函数调用");
    }
    

    #endregion

    #region ToolTip

    private void InitializeToolTip()
    {
        var toolTip = new ToolTip();
        toolTip.Opened += OnToolTipOpened;
        this.ToolTip = toolTip;
        _logger.Debug(" 初始化ToolTip");
    }

    private void OnToolTipOpened(object sender, RoutedEventArgs e)
    {
        var toolTip = sender as ToolTip;
        if (toolTip != null && _clip != null)
        {
            toolTip.Content = _clip.ToolTipInfo;
            _logger.Debug(" ToolTip打开时更新内容: ClipIndex={ClipIndex}", _clip?.Index);
        }
    }

    #endregion

    #region UI控件缓存

    private void CacheUIControls()
    {
        _textBlock = this.FindName("文本块") as TextBlock;
        _speedTextBlock = this.FindName("速度文本块") as TextBlock;
        _overDurationBackground = this.FindName("超时背景矩形") as Rectangle;
        _pendingBorder = this.FindName("待生成标识边框") as Border;
        _negativeWidthIndicator = this.FindName("负数宽度标识边框") as Border;
    }

    #endregion

    #region 节流机制

    private void InitializeThrottleTimer()
    {
        _throttleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ThrottleDelayMs)
        };
        _throttleTimer.Tick += OnThrottleTimerTick;
    }

    private void OnThrottleTimerTick(object? sender, EventArgs e)
    {
        _throttleTimer?.Stop();
        
        if (_updatePositionPending)
        {
            UpdatePositionAndSizeInternal();
            _updatePositionPending = false;
        }
    }

    private void ScheduleThrottledUpdate()
    {
        if (_throttleTimer == null)
        {
            return;
        }

        _updatePositionPending = true;

        if (_throttleTimer.IsEnabled)
        {
            return;
        }

        _throttleTimer.Start();
    }

    #endregion

    #region 公共属性

    public Clip? Clip
    {
        get => _clip;
        set
        {
            if (_clip != value)
            {
                _logger.Debug(" Clip 属性变更: Old={OldClipIndex}, New={NewClipIndex}", _clip?.Index, value?.Index);
                UnsubscribeClipEvents();
                _clip = value;
                this.Name = $"Segment_{_clip?.Index}";
                if (value is AudioClip ac && !string.IsNullOrEmpty(ac.FilePath))
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(ac.FilePath);
                    var sanitizedPath = System.Text.RegularExpressions.Regex.Replace(fileName, @"[^a-zA-Z0-9_]", "_");
                    this.Name += $"_{sanitizedPath}";
                }
                
                SubscribeClipEvents();
                UpdateUI();
            }
        }
    }

    public double ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            if (Math.Abs(_zoomFactor - value) > 0.001)
            {
                _logger.Debug(" ZoomFactor 属性变更: Old={Old}, New={New}", _zoomFactor, value);
                _zoomFactor = value;
                UpdatePositionAndSize();
            }
        }
    }

    public double TotalDuration
    {
        get => _totalDuration;
        set
        {
            if (Math.Abs(_totalDuration - value) > 0.001)
            {
                _logger.Debug(" TotalDuration 属性变更: Old={Old}, New={New}", _totalDuration, value);
                _totalDuration = value;
                UpdatePositionAndSize();
            }
        }
    }

    public bool CanPlay
    {
        get => _canPlay;
        set
        {
            if (_canPlay != value)
            {
                _logger.Debug(" CanPlay 属性变更: Old={Old}, New={New}", _canPlay, value);
                _canPlay = value;
                UpdateCursor();
            }
        }
    }

    public IAudioPlayerService? AudioPlayerService
    {
        get => _audioPlayerService;
        set
        {
            _audioPlayerService = value;
        }
    }

    public VideoEditorContext? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
        }
    }

    public string RowColor
    {
        get => BorderBrush?.ToString() ?? "#CCCCCC";
        set
        {
            if (ColorConverter.ConvertFromString(value) is Color color)
            {
                BorderBrush = new SolidColorBrush(color);
            }
        }
    }

    #endregion

    #region 事件

    public event EventHandler<Clip>? PlayRequested;

    #endregion

    #region 事件订阅

    private void SubscribeClipEvents()
    {
        if (_clip == null)
        {
            return;
        }

        _clip.Changed += OnClipPropertyChanged;
        _logger.Debug(" 已订阅 Clip 属性变化事件: Clip={ClipIndex}", _clip.Index);
    }

    private void UnsubscribeClipEvents()
    {
        if (_clip == null)
        {
            return;
        }

        _clip.Changed -= OnClipPropertyChanged;
        _logger.Debug(" 已取消订阅 Clip 属性变化事件: Clip={ClipIndex}", _clip.Index);
    }

    private void OnClipPropertyChanged(object? sender, DevExpress.Xpo.ObjectChangeEventArgs e)
    {
        _logger.Debug(" Clip 属性变化: PropertyName={PropertyName}, ClipIndex={ClipIndex}", e.PropertyName, _clip?.Index);

        switch (e.PropertyName)
        {
            case nameof(Clip.Start):
            case nameof(Clip.End):
                ScheduleThrottledUpdate();
                break;
            case nameof(Clip.DisplayText):
                UpdateText();
                break;
            case nameof(Clip.BackgroundColor):
                UpdateOverDurationBackground();
                break;
            case nameof(AudioClip.SpeedMultiplier):
                UpdateSpeedText();
                break;
            case nameof(AudioClip.FilePath):
                UpdatePendingGenerationIndicator();
                UpdateCanPlay();
                UpdateWaveformImage();
                break;
            case "WaveformData":
                _logger.Information("[SimpleSegmentControl] WaveformData 属性变化: ClipIndex={ClipIndex}", _clip?.Index);
                UpdateWaveformImage();
                break;
            case "ShowWaveform":
                _logger.Information("[SimpleSegmentControl] ShowWaveform 属性变化: ClipIndex={ClipIndex}, ShowWaveform={ShowWaveform}", 
                    _clip?.Index, (_clip as IWaveform)?.ShowWaveform);
                UpdateWaveformImage();
                break;
        }
    }

    #endregion

    #region UI更新

    private bool _updatePending = false;
    private bool _updatePositionPending = false;
    private bool _updateTextPending = false;
    private bool _updateBackgroundPending = false;
    private bool _updateSpeedPending = false;
    private bool _updatePendingIndicatorPending = false;
    private bool _updateWaveformPending = false;

    private void UpdateUI()
    {
        ScheduleBatchUpdate();
    }

    private void ScheduleBatchUpdate()
    {
        if (_updatePending)
        {
            return;
        }

        _updatePending = true;
        _updatePositionPending = true;
        _updateTextPending = true;
        _updateBackgroundPending = true;
        _updateSpeedPending = true;
        _updatePendingIndicatorPending = true;
        _updateWaveformPending = true;

        Dispatcher.BeginInvoke(new Action(ExecuteBatchUpdate), DispatcherPriority.Render);
    }

    private void SchedulePositionUpdate()
    {
        if (_updatePositionPending)
        {
            return;
        }

        _updatePositionPending = true;
        Dispatcher.BeginInvoke(new Action(UpdatePositionAndSize), DispatcherPriority.Render);
    }

    private void ExecuteBatchUpdate()
    {
        _updatePending = false;

        if (_updatePositionPending)
        {
            UpdatePositionAndSizeInternal();
            _updatePositionPending = false;
        }

        if (_updateTextPending)
        {
            UpdateTextInternal();
            _updateTextPending = false;
        }

        if (_updateBackgroundPending)
        {
            UpdateOverDurationBackgroundInternal();
            _updateBackgroundPending = false;
        }

        if (_updateSpeedPending)
        {
            UpdateSpeedTextInternal();
            _updateSpeedPending = false;
        }

        if (_updatePendingIndicatorPending)
        {
            UpdatePendingGenerationIndicatorInternal();
            _updatePendingIndicatorPending = false;
        }

        if (_updateWaveformPending)
        {
            UpdateWaveformImageInternal();
            _updateWaveformPending = false;
        }
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        _logger.Debug(" 控件已加载: ClipIndex={ClipIndex}", _clip?.Index);
    }

    private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _logger.Debug(" 控件尺寸变化: ClipIndex={ClipIndex}, NewWidth={Width:F2}, NewHeight={Height:F2}",
            _clip?.Index, e.NewSize.Width, e.NewSize.Height);
    }

    private void UpdatePositionAndSize()
    {
        if (!Dispatcher.CheckAccess())
        {
            SchedulePositionUpdate();
            return;
        }

        UpdatePositionAndSizeInternal();
    }

    private void UpdatePositionAndSizeInternal()
    {
        if (_clip == null)
        {
            return;
        }
        var seg = _clip as ISpeechSegment;
        var left = seg.StartSeconds * _zoomFactor * 100.0;
        var width = seg.DurationSeconds * _zoomFactor * 100.0;

        Canvas.SetLeft(this, left);
        Width = Math.Max(0, width);
        
        if (double.IsNaN(Height) || Height <= 0)
        {
            Height = 50;
        }

        UpdateNegativeWidthIndicator(width);

        _logger.Debug(" 更新位置和大小: ClipIndex={ClipIndex}, Left={Left}, Width={Width}, Height={Height}", _clip.Index, left, width, Height);
    }

    private void UpdateNegativeWidthIndicator(double calculatedWidth)
    {
        if (_negativeWidthIndicator == null || _clip == null)
        {
            return;
        }

        if (calculatedWidth < 0)
        {
            _negativeWidthIndicator.Visibility = Visibility.Visible;
            _logger.Warning("[SimpleSegmentControl] 检测到负数宽度: ClipIndex={ClipIndex}, ClipType={ClipType}, Start={Start}, End={End}, CalculatedWidth={Width:F3}, DisplayText={Text}",
                _clip.Index, _clip.GetType().Name, _clip.Start, _clip.End, calculatedWidth, _clip.DisplayText);
        }
        else
        {
            _negativeWidthIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateText()
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!_updatePending)
            {
                Dispatcher.BeginInvoke(new Action(UpdateText), DispatcherPriority.Render);
            }
            return;
        }

        UpdateTextInternal();
    }

    private void UpdateTextInternal()
    {
        if (_textBlock == null || _clip == null)
        {
            return;
        }

        var displayText = _clip.DisplayText;

        if (string.IsNullOrEmpty(displayText))
        {
            _textBlock.Visibility = Visibility.Collapsed;
        }
        else
        {
            _textBlock.Text = displayText;
            _textBlock.Visibility = Visibility.Visible;
        }

        _logger.Debug(" 更新文本显示: ClipIndex={ClipIndex}, ClipType={ClipType}, DisplayText={DisplayText}, Visibility={Visibility}",
            _clip.Index, _clip.GetType().Name, displayText, _textBlock.Visibility);
    }

    private void UpdateSpeedText()
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!_updatePending)
            {
                Dispatcher.BeginInvoke(new Action(UpdateSpeedText), DispatcherPriority.Render);
            }
            return;
        }

        UpdateSpeedTextInternal();
    }

    private void UpdateSpeedTextInternal()
    {
        if (_speedTextBlock == null || _clip == null)
        {
            return;
        }

        var audioClip = _clip as AudioClip;
        var speedMultiplier = audioClip?.SpeedMultiplier ?? 1.0;
        if (speedMultiplier <= 1)
        {
            _speedTextBlock.Visibility = Visibility.Collapsed;
        }
        else
        {
            _speedTextBlock.Text = $"x{speedMultiplier:F2}";
            _speedTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void UpdateOverDurationBackground()
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!_updatePending)
            {
                Dispatcher.BeginInvoke(new Action(UpdateOverDurationBackground), DispatcherPriority.Render);
            }
            return;
        }

        UpdateOverDurationBackgroundInternal();
    }

    private void UpdateOverDurationBackgroundInternal()
    {
        if (_overDurationBackground == null || _clip == null)
        {
            return;
        }

        var backgroundColor = _clip.BackgroundColor;
        if (backgroundColor != "Transparent" && ColorConverter.ConvertFromString(backgroundColor) is Color color)
        {
            _overDurationBackground.Fill = new SolidColorBrush(color);
            _overDurationBackground.Visibility = Visibility.Visible;
        }
        else
        {
            _overDurationBackground.Visibility = Visibility.Collapsed;
        }

        var audioClip = _clip as AudioClip;
        _logger.Debug(" 更新超时背景: ClipIndex={ClipIndex}, BackgroundColor={BackgroundColor}, OverDurationRatio={OverDurationRatio}",
            _clip?.Index, backgroundColor, audioClip?.OverDurationRatio ?? 0);
    }

    private void UpdateCursor()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(UpdateCursor));
            return;
        }

        Cursor = _canPlay ? Cursors.Hand : Cursors.Arrow;
    }

    private void UpdatePendingGenerationIndicator()
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!_updatePending)
            {
                Dispatcher.BeginInvoke(new Action(UpdatePendingGenerationIndicator), DispatcherPriority.Render);
            }
            return;
        }

        UpdatePendingGenerationIndicatorInternal();
    }

    private void UpdatePendingGenerationIndicatorInternal()
    {
        if (_pendingBorder == null || _clip == null)
        {
            return;
        }

        var audioClip = _clip as AudioClip;
        if (audioClip != null && string.IsNullOrEmpty(audioClip.FilePath))
        {
            _pendingBorder.Visibility = Visibility.Visible;
            _logger.Debug(" 显示待生成标识: ClipIndex={ClipIndex}, FilePath={FilePath}", _clip.Index, audioClip.FilePath);
        }
        else
        {
            _pendingBorder.Visibility = Visibility.Collapsed;
            _logger.Debug(" 隐藏待生成标识: ClipIndex={ClipIndex}, IsAudioClip={IsAudioClip}, HasFilePath={HasFilePath}", 
                _clip.Index, audioClip != null, !string.IsNullOrEmpty(audioClip?.FilePath));
        }
    }

    private void UpdateCanPlay()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(UpdateCanPlay));
            return;
        }

        var audioClip = _clip as AudioClip;
        var newCanPlay = audioClip != null && !string.IsNullOrEmpty(audioClip.FilePath);
        
        if (_canPlay != newCanPlay)
        {
            _canPlay = newCanPlay;
            UpdateCursor();
            _logger.Debug(" 更新 CanPlay: ClipIndex={ClipIndex}, CanPlay={CanPlay}, HasFilePath={HasFilePath}", 
                _clip?.Index, _canPlay, !string.IsNullOrEmpty(audioClip?.FilePath));
        }
    }

    #endregion

    #region 鼠标事件

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (_canPlay && _clip is AudioClip audioClip && !string.IsNullOrEmpty(audioClip.FilePath))
        {
            PlayRequested?.Invoke(this, _clip);
        }
    }

    private void OnRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_clip == null)
        {
            _logger.Warning("[SimpleSegmentControl] 右键点击时 Clip 为空，跳过菜单显示");
            return;
        }

        e.Handled = true;

        _logger.Information("[SimpleSegmentControl] 开始创建右键菜单: ClipType={ClipType}, ClipIndex={ClipIndex}, HasViewModel={HasViewModel}, HasPlayRequested={HasPlayRequested}",
            _clip.GetType().Name, _clip.Index, _viewModel != null, PlayRequested != null);

        var contextMenu = new ContextMenu();

        #region 添加基于特性的菜单项

        Action<Clip>? playAction = PlayRequested != null ? clip => PlayRequested?.Invoke(this, clip) : null;
        var menuItems = _contextMenuFactory.CreateMenuItems(_clip, _viewModel, playAction);
        
        _logger.Information("[SimpleSegmentControl] 菜单项创建完成: MenuItemCount={Count}", menuItems.Count);
        
        foreach (var item in menuItems)
        {
            if (item is MenuItem menuItem)
            {
                _logger.Debug("[SimpleSegmentControl] 添加菜单项: Header={Header}, IsEnabled={IsEnabled}", menuItem.Header, menuItem.IsEnabled);
            }
            contextMenu.Items.Add(item);
        }

        #endregion

        contextMenu.PlacementTarget = this;
        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        contextMenu.IsOpen = true;

        _logger.Information("[SimpleSegmentControl] 右键菜单已显示: ClipType={ClipType}, ClipIndex={ClipIndex}, TotalMenuItems={Count}",
            _clip.GetType().Name, _clip.Index, contextMenu.Items.Count);
    }

    private void OnButtonMouseEnter(object sender, MouseEventArgs e)
    {
        Panel.SetZIndex(this, 100);
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
        BorderThickness = new Thickness(2);
    }

    private void OnButtonMouseLeave(object sender, MouseEventArgs e)
    {
        Panel.SetZIndex(this, 0);
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
        BorderThickness = new Thickness(1);
    }

    #endregion
}
