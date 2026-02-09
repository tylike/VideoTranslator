using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DevExpress.Xpo;
using Serilog;
using VT.Module.BusinessObjects;
using TimeLine.Services;
using VT.Module;

namespace TimeLine.Controls;

public partial class SimpleTimeLinePanel : ContentControl
{
    #region 字段

    private double _zoomFactor = 1.0;
    private double _totalDuration = 100;
    private double _durationBufferRatio = 0.2;
    private IAudioPlayerService? _audioPlayerService;
    private TimeLine.Controls.RulerScrollBar? _rulerScrollBar;

    private ScrollViewer? _scrollViewer=>this.ScrollViewer;
    private ScrollViewer? _rightScrollViewer=>this.RightScrollViewer;
    private StackPanel? _leftPanelStack=>this.LeftPanelStack;
    private StackPanel? _rightPanelStack=>this.RightPanelStack;
    private readonly ILogger _logger = LoggerService.ForContext<SimpleTimeLinePanel>();

    private static readonly Dictionary<string, string> _trackColors = new()
    {
        { "时间轴", "#808080" },
        { "VAD", "#FF6B6B" },
        { "说话音频", "#9B59B6" },
        { "源SRT", "#4ECDC4" },
        { "源字幕", "#4ECDC4" },
        { "源字幕Vad", "#4ECDC4" },
        { "目标翻译SRT", "#45B7D1" },
        { "目标字幕", "#45B7D1" },
        { "源音频片段", "#96CEB4" },
        { "源音频", "#96CEB4" },
        { "目标翻译音频片段", "#FFD93D" },
        { "目标音频", "#FFD93D" },
        { "调整音频片段", "#FF8C42" },
        { "调整音频", "#FF8C42" },
        { "源人声音频", "#9B59B6" },
        { "背景音频", "#3498DB" }
    };

    private static readonly Dictionary<MediaType, string> _mediaTypeColors = new()
    {
        { MediaType.Video, "#808080" },
        { MediaType.Audio, "#96CEB4" },
        { MediaType.Image, "#FFD93D" },
        { MediaType.Subtitles, "#4ECDC4" },
        { MediaType.源视频, "#808080" },
        { MediaType.说话音频, "#9B59B6" },
        { MediaType.背景音频, "#3498DB" },
        { MediaType.源音频, "#96CEB4" },
        { MediaType.静音视频, "#808080" },
        { MediaType.源字幕, "#4ECDC4" },
        { MediaType.目标字幕, "#45B7D1" },
        { MediaType.源字幕Vad, "#4ECDC4" },
        { MediaType.源字幕下载, "#4ECDC4" },
        { MediaType.TTS分段, "#FFD93D" },
        { MediaType.调整音频段, "#FF8C42" }
    };

    #endregion

    #region 构造函数

    public SimpleTimeLinePanel()
    {
        InitializeComponent();
        _logger.Debug("[SimpleTimeLinePanel] 构造函数调用");

        SetupDataBindings();
        SetupScrollSync();
        InitializeHoverIndicator();
        InitializeRulerScrollBar();

        this.tracks = new Tracks(_leftPanelStack, _rightPanelStack);
    }

    private void SetupDataBindings()
    {
        // 绑定缩放滑块
        var sliderBinding = new Binding("ZoomFactor") { Source = this, Mode = BindingMode.TwoWay };
        ZoomSlider.SetBinding(System.Windows.Controls.Primitives.RangeBase.ValueProperty, sliderBinding);

        // 绑定缩放值显示
        var valueBinding = new Binding("ZoomFactor") { Source = this, StringFormat = "{0:F2}x" };
        ZoomValueTextBlock.SetBinding(TextBlock.TextProperty, valueBinding);

        // 绑定轨道可见性下拉框（在Tracks属性设置时也会更新）
        if (Tracks != null)
        {
            TracksComboBox.ItemsSource = Tracks;
        }
    }

    private void InitializeRulerScrollBar()
    {
        Loaded += (s, e) =>
        {
            if (_rightScrollViewer == null)
            {
                return;
            }

            var template = _rightScrollViewer.Template;
            if (template == null)
            {
                return;
            }

            var horizontalScrollBar = template.FindName("PART_HorizontalScrollBar", _rightScrollViewer) as TimeLine.Controls.RulerScrollBar;
            if (horizontalScrollBar != null)
            {
                _rulerScrollBar = horizontalScrollBar;
                _rulerScrollBar.TotalDuration = _totalDuration;
                _rulerScrollBar.ZoomFactor = _zoomFactor;
                _logger.Debug("[SimpleTimeLinePanel] RulerScrollBar 已找到并初始化: TotalDuration={TotalDuration}, ZoomFactor={ZoomFactor}", 
                    _totalDuration, _zoomFactor);
            }
        };
    }

    #endregion

    #region 公共属性

 
    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(SimpleTimeLinePanel),
            new PropertyMetadata(1.0, OnZoomFactorChanged));

    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    public static readonly DependencyProperty TotalDurationProperty =
        DependencyProperty.Register(nameof(TotalDuration), typeof(double), typeof(SimpleTimeLinePanel),
            new PropertyMetadata(100.0, OnTotalDurationChanged));

    public double TotalDuration
    {
        get => (double)GetValue(TotalDurationProperty);
        set
        {
            SetValue(TotalDurationProperty, value);
        }
    }

    public static readonly DependencyProperty DurationBufferRatioProperty =
        DependencyProperty.Register(nameof(DurationBufferRatio), typeof(double), typeof(SimpleTimeLinePanel),
            new PropertyMetadata(0.2, OnDurationBufferRatioChanged));

    public double DurationBufferRatio
    {
        get => (double)GetValue(DurationBufferRatioProperty);
        set
        {
            SetValue(DurationBufferRatioProperty, value);
        }
    }

    private static void OnDurationBufferRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SimpleTimeLinePanel panel)
        {
            var newValue = (double)e.NewValue;
            if (newValue < 0 || newValue > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(DurationBufferRatio), "缓冲系数必须在 0 到 1 之间");
            }
            panel._durationBufferRatio = newValue;
            panel._logger.Debug("[SimpleTimeLinePanel] DurationBufferRatio 属性变更: New={New}", newValue);
            panel.UpdateTotalDuration();
        }
    }

    private static void OnTotalDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SimpleTimeLinePanel panel)
        {
            var newValue = (double)e.NewValue;
            if (Math.Abs(panel._totalDuration - newValue) > 0.001)
            {
                panel._logger.Debug("[SimpleTimeLinePanel] TotalDuration 属性变更: Old={Old}, New={New}", panel._totalDuration, newValue);
                panel._totalDuration = newValue;
                panel.tracks.UpdateAllTracksDuration(newValue);
                
                if (panel._rulerScrollBar != null)
                {
                    panel._rulerScrollBar.TotalDuration = newValue;
                }
            }
        }
    }

    private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SimpleTimeLinePanel panel)
        {
            var newValue = (double)e.NewValue;
            panel._zoomFactor = newValue;
            panel._logger.Debug("[SimpleTimeLinePanel] ZoomFactor 属性变更: Old={Old}, New={New}", e.OldValue, newValue);
            panel.tracks.Zoom(newValue);
            
            if (panel._rulerScrollBar != null)
            {
                panel._rulerScrollBar.ZoomFactor = newValue;
            }
        }
    }

    public IAudioPlayerService? AudioPlayerService
    {
        get => _audioPlayerService;
        set
        {
            _audioPlayerService = value;
            UpdateAllTracksServices();
        }
    }

    #endregion

    public VT.Module.VideoEditorContext Context { get; set; }

    #region 事件

    public event EventHandler<TrackInfo>? TrackSelected;
    public event EventHandler<Clip>? SegmentPlayRequested;

    #endregion

    #region 滚动同步

    private void SetupScrollSync()
    {
        if (_scrollViewer != null && _rightScrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            _rightScrollViewer.PreviewMouseWheel += OnRightPanelMouseWheel;
            _rightScrollViewer.ScrollChanged += OnRightPanelScrollChanged;
        }
    }

    private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_rightScrollViewer != null && e.VerticalChange != 0)
        {
            _rightScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        }
    }

    private void OnRightPanelScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer != null && e.HorizontalChange != 0)
        {
            _scrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }
    }

    private void OnRightPanelMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_rightScrollViewer == null)
        {
            return;
        }

        double horizontalOffset = _rightScrollViewer.HorizontalOffset;
        double scrollAmount = e.Delta * -0.1;

        horizontalOffset += scrollAmount;
        horizontalOffset = Math.Max(0, Math.Min(horizontalOffset, _rightScrollViewer.ScrollableWidth));

        _rightScrollViewer.ScrollToHorizontalOffset(horizontalOffset);

        e.Handled = true;
    }

    #endregion


    public XPCollection<TrackInfo> Tracks
    {
        get => field;
        set
        {
            if (field != value)
            {
                field?.CollectionChanged -= Tracks_CollectionChanged;
                ClearAllTracks();
                field = value;
                
                // 更新下拉框数据源
                TracksComboBox.ItemsSource = value;
                
                if (value != null)
                {
                    var maxEnd = value.Any() ? value.Max(t => t.Segments.Count > 0 ? t.Segments.Max(s => s.End.TotalSeconds) : 100) : 100;
                    var bufferedDuration = maxEnd > 0 ? maxEnd * (1 + _durationBufferRatio) : 100;
                    this.TotalDuration = Math.Max(bufferedDuration, 100);
                }
                CreateAllTracks();
                field?.CollectionChanged += Tracks_CollectionChanged;

            }
            void CreateAllTracks()
            {
                if(Tracks!=null)
                foreach (var item in Tracks)
                {
                    CreateTrackControl(item);
                }
            }
            void Tracks_CollectionChanged(object sender, XPCollectionChangedEventArgs e)
            {
                if (e.CollectionChangedType == XPCollectionChangedType.AfterAdd)
                {
                    var track = (TrackInfo)e.ChangedObject;
                    CreateTrackControl(track);
                    UpdateTotalDuration();
                }
                else if (e.CollectionChangedType == XPCollectionChangedType.AfterRemove)
                {
                    var track = (TrackInfo)e.ChangedObject;
                    OnTrackRemoved(track);
                    UpdateTotalDuration();
                }
            }
            void ClearAllTracks()
            {
                _logger.Debug("[SimpleTimeLinePanel] 清理所有轨道: 当前控件数={CurrentCount}", tracks.Count);

                foreach (var track in tracks.TrackControls)
                {
                    UnsubscribeTrackEvents(track.Info);
                }
                tracks.TrackControls.Clear();

                _leftPanelStack?.Children.Clear();
                _rightPanelStack?.Children.Clear();
            }
        }
    }

    #region 事件订阅
    private void SubscribeTrackEvents(TrackInfo track)
    {
        if (track == null)
        {
            return;
        }

        track.Changed += OnTrackPropertyChanged;

        _logger.Debug("[SimpleTimeLinePanel] 已订阅 Track 事件: Title={Title}", track.Title);
    }

    private void UnsubscribeTrackEvents(TrackInfo track)
    {
        if (track == null)
        {
            return;
        }

        track.Changed -= OnTrackPropertyChanged;

        _logger.Debug("[SimpleTimeLinePanel] 已取消订阅 Track 事件: Title={Title}", track.Title);
    }

    private void UpdateAllTracksServices()
    {
        foreach (var control in tracks.TrackControls)
        {
            control.Control.AudioPlayerService = _audioPlayerService;
        }
    }

    #endregion

    #region 创建控件
    Tracks tracks;
    private string GetTrackColor(TrackInfo track)
    {
        if (!string.IsNullOrEmpty(track.Color) && track.Color != "#00FF00")
        {
            return track.Color;
        }

        if (_trackColors.TryGetValue(track.Title, out var color))
        {
            return color;
        }

        if (track.Media != null && _mediaTypeColors.TryGetValue(track.Media.MediaType, out var mediaColor))
        {
            return mediaColor;
        }

        var colors = _mediaTypeColors.Values.ToList();
        var index = track.Index % colors.Count;
        return colors[index];
    }

    #endregion

    #region 菜单处理

    private void OnTrackHeaderMenuButtonClick(object? sender, TrackInfo track)
    {
        _logger.Debug("[SimpleTimeLinePanel] 菜单按钮点击: Title={Title}", track.Title);

        if (sender is TrackHeaderControl headerControl)
        {
            var contextMenu = TrackContextMenu.Create(track, Context);
            contextMenu.PlacementTarget = headerControl;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;

            _logger.Debug("[SimpleTimeLinePanel] 轨道菜单已打开: Title={Title}, MediaType={MediaType}",
                track.Title, track.Media?.MediaType);
        }
    }

    #endregion

    #region 片段播放

    private void OnSegmentPlayRequested(object? sender, Clip clip)
    {
        SegmentPlayRequested?.Invoke(this, clip);
    }

    #endregion

    private void OnTracksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _logger.Debug("[SimpleTimeLinePanel] Tracks 集合变化: Action={Action}", e.Action);

        if (e.NewItems != null)
        {
            foreach (TrackInfo track in e.NewItems)
            {
                CreateTrackControl(track);
            }
        }

        if (e.OldItems != null)
        {
            foreach (TrackInfo track in e.OldItems)
            {
                OnTrackRemoved(track);
            }
        }
    }

    private void OnTrackPropertyChanged(object? sender, ObjectChangeEventArgs e)
    {
        if (sender is not TrackInfo track)
        {
            return;
        }

        if (e.PropertyName == nameof(TrackInfo.Segments))
        {
            UpdateTotalDuration();
        }

        _logger.Debug("[SimpleTimeLinePanel] Track 属性变化: PropertyName={PropertyName}, Title={Title}", e.PropertyName, track.Title);
        var inf = tracks.Find(track);
        if (inf == null)
        {
            return;
        }
        switch (e.PropertyName)
        {
            case nameof(TrackInfo.Title):
                inf.Header.Title = track.Title;
                break;
            case nameof(TrackInfo.Height):
                inf.Header.Height = track.Height;
                inf.Control.Height = track.Height;
                break;
            case nameof(TrackInfo.Color):
                track.Color = track.Color;
                break;
            case nameof(TrackInfo.Visible):
                UpdateTrackVisibility(track);
                break;
        }
    }

    private void UpdateTotalDuration()
    {
        if (Tracks != null && Tracks.Any())
        {
            var maxEnd = Tracks.Max(t => t.Segments.Count > 0 ? t.Segments.Max(s => s.End.TotalSeconds) : 0);
            var bufferedDuration = maxEnd > 0 ? maxEnd * (1 + _durationBufferRatio) : 100;
            TotalDuration = Math.Max(bufferedDuration, 100);
        }
        else
        {
            TotalDuration = 100;
        }
    }

    private void UpdateTrackVisibility(TrackInfo track)
    {
        var inf = tracks.Find(track);
        if (inf == null)
        {
            return;
        }

        _logger.Debug("[SimpleTimeLinePanel] 轨道可见性变化: Title={Title}, Visible={Visible}", track.Title, track.Visible);

        inf.Header.Visibility = track.Visible ? Visibility.Visible : Visibility.Collapsed;
        inf.Control.Visibility = track.Visible ? Visibility.Visible : Visibility.Collapsed;
    }

    

    #region 轨道管理

    private void CreateTrackControl(TrackInfo track)
    {
        if (_leftPanelStack == null || _rightPanelStack == null)
        {
            return;
        }

        _logger.Debug("[SimpleTimeLinePanel] 轨道新增: Title={Title}, Visible={Visible}", track.Title, track.Visible);

        var headerControl = CreateTrackHeader(track);
        var trackControl = CreateTrack(track);
        SubscribeTrackEvents(track);
        tracks.Add(new TrackControl(track, trackControl, headerControl));

        // 根据Visible属性设置初始可见性
        UpdateTrackVisibility(track);

        TrackHeaderControl CreateTrackHeader(TrackInfo track)
        {
            var headerControl = new TrackHeaderControl
            {
                Title = track.Title,
                Height = track.Height > 0 ? track.Height : 50,
                TrackInfo = track
            };

            headerControl.MouseLeftButtonDown += OnTrackHeaderMouseLeftButtonDown;
            headerControl.MouseMove += OnTrackHeaderMouseMove;
            headerControl.MouseLeftButtonUp += OnTrackHeaderMouseLeftButtonUp;
            headerControl.MouseLeave += OnTrackHeaderMouseLeave;
            headerControl.MenuButtonClick += OnTrackHeaderMenuButtonClick;

            _logger.Debug("[SimpleTimeLinePanel] 创建轨道标题控件: Title={Title}, Height={Height}", track.Title, track.Height);

            return headerControl;
        }

        SimpleTrackControl CreateTrack(TrackInfo track)
        {
            var trackColor = GetTrackColor(track);

            if (string.IsNullOrEmpty(track.Color) || track.Color == "#00FF00")
            {
                track.Color = trackColor;
                _logger.Debug("[SimpleTimeLinePanel] 设置轨道颜色: Title={Title}, Color={Color}", track.Title, trackColor);
            }

            var trackControl = new SimpleTrackControl
            {
                TrackInfo = track,
                ZoomFactor = _zoomFactor,
                TotalDuration = _totalDuration,
                AudioPlayerService = _audioPlayerService
            };

            trackControl.Height = track.Height > 0 ? track.Height : 50;

            trackControl.SegmentPlayRequested += OnSegmentPlayRequested;

            _logger.Debug("[SimpleTimeLinePanel] 创建轨道控件: Title={Title}, Height={Height}, Color={Color}", track.Title, track.Height, trackColor);

            return trackControl;
        }
    }

    private void OnTrackRemoved(TrackInfo track)
    {
        var inf = tracks.Find(track)!;

        _logger.Debug("[SimpleTimeLinePanel] 轨道删除: Title={Title}", track.Title);

        _leftPanelStack?.Children.Remove(inf.Header);
        _rightPanelStack?.Children.Remove(inf.Control);

        UnsubscribeTrackEvents(track);
        tracks.Remove(inf);
    }
    #endregion
}
