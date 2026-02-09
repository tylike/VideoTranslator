using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Serilog;
using VT.Module.BusinessObjects;
using TimeLine.Services;
using VT.Core;
using VT.Module;

namespace TimeLine.Controls;

public partial class SimpleTrackControl : ContentControl
{
    #region 字段

    private TrackInfo? _trackInfo;
    private double _zoomFactor = 1.0;
    private double _totalDuration = 0.0;
    private IAudioPlayerService? _audioPlayerService;
    private VideoEditorContext? _viewModel;

    private Canvas? _itemsCanvas;
    private bool _isUpdating = false;
    private bool _isLayoutUpdatePending = false;
    private readonly ILogger _logger = LoggerService.ForContext<SimpleTrackControl>();

    private readonly Dictionary<Clip, SimpleSegmentControl> _segmentControls = new();

    #endregion

    #region 构造函数

    public SimpleTrackControl()
    {
        InitializeComponent();
        _itemsCanvas = this.FindName("项目画布") as Canvas;
        _logger.Debug("[SimpleTrackControl] 构造函数调用");
    }

    #endregion

    #region 公共属性

    public TrackInfo? TrackInfo
    {
        get => _trackInfo;
        set
        {
            if (_trackInfo == value)
            {
                return;
            }

            _logger.Debug("[SimpleTrackControl] TrackInfo 属性变更: Old={Old}, New={New}", _trackInfo?.Title, value?.Title);

            Action updateAction = () =>
            {
                ClearAllSegments();
                UnsubscribeTrackEvents();

                _trackInfo = value;

                if (_trackInfo != null)
                {
                    SubscribeTrackEvents();
                    InitializeAllSegments();
                }

                UpdateUI();
            };

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }
    }

    public double ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            if (Math.Abs(_zoomFactor - value) <= 0.001)
            {
                return;
            }

            _logger.Debug("[SimpleTrackControl] ZoomFactor 属性变更: Old={Old}, New={New}", _zoomFactor, value);

            Action updateAction = () =>
            {
                _zoomFactor = value;
                UpdateCanvasWidth();
                UpdateAllSegments();
            };

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }
    }

    public double TotalDuration
    {
        get => _totalDuration;
        set
        {
            if (Math.Abs(_totalDuration - value) <= 0.001)
            {
                return;
            }

            _logger.Debug("[SimpleTrackControl] TotalDuration 属性变更: Old={Old}, New={New}", _totalDuration, value);

            Action updateAction = () =>
            {
                _totalDuration = value;
                UpdateCanvasWidth();
            };

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }
    }

    public IAudioPlayerService? AudioPlayerService
    {
        get => _audioPlayerService;
        set
        {
            _audioPlayerService = value;
            UpdateAllSegmentsServices();
        }
    }

    public VideoEditorContext? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            UpdateAllSegmentsViewModel();
        }
    }

    #endregion

    #region 事件

    public event EventHandler<Clip>? SegmentPlayRequested;

    #endregion

    #region 事件订阅

    private void SubscribeTrackEvents()
    {
        if (_trackInfo == null)
        {
            return;
        }

        _trackInfo.Changed += OnTrackPropertyChanged;
        _trackInfo.Segments.CollectionChanged += OnSegmentsCollectionChanged;

        _logger.Debug("[SimpleTrackControl] 已订阅 TrackInfo 事件: Title={Title}", _trackInfo.Title);
    }

    private void UnsubscribeTrackEvents()
    {
        if (_trackInfo == null)
        {
            return;
        }

        _trackInfo.Changed -= OnTrackPropertyChanged;
        _trackInfo.Segments.CollectionChanged -= OnSegmentsCollectionChanged;

        _logger.Debug("[SimpleTrackControl] 已取消订阅 TrackInfo 事件: Title={Title}", _trackInfo.Title);
    }

    private void OnTrackPropertyChanged(object? sender, DevExpress.Xpo.ObjectChangeEventArgs e)
    {
        _logger.Debug("[SimpleTrackControl] TrackInfo 属性变化: PropertyName={PropertyName}, Title={Title}", e.PropertyName, _trackInfo?.Title);

        Action updateAction = e.PropertyName switch
        {
            nameof(TrackInfo.Height) => UpdateTrackHeight,
            nameof(TrackInfo.Color) => UpdateAllSegmentsColor,
            _ => null
        };

        if (updateAction != null)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }
    }

    private void OnSegmentsCollectionChanged(object? sender, DevExpress.Xpo.XPCollectionChangedEventArgs e)
    {
        _logger.Debug("[SimpleTrackControl] Segments 集合变化: CollectionChangedType={CollectionChangedType}", e.CollectionChangedType);

        if (e.ChangedObject is not Clip clip)
        {
            return;
        }

        if (e.CollectionChangedType == DevExpress.Xpo.XPCollectionChangedType.AfterAdd)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnSegmentAdded(clip));
            }
            else
            {
                OnSegmentAdded(clip);
            }
        }
        else if (e.CollectionChangedType == DevExpress.Xpo.XPCollectionChangedType.AfterRemove)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnSegmentRemoved(clip));
            }
            else
            {
                OnSegmentRemoved(clip);
            }
        }
    }

    #endregion

    #region 片段管理

    private void ClearAllSegments()
    {
        _logger.Debug("[SimpleTrackControl] 清理所有片段: 当前控件数={CurrentCount}", _segmentControls.Count);

        foreach (var segment in _segmentControls.Keys.ToList())
        {
            if (_segmentControls.TryGetValue(segment, out var control))
            {
                _itemsCanvas?.Children.Remove(control);
                _segmentControls.Remove(segment);
            }
        }
    }

    private void InitializeAllSegments()
    {
        if (_trackInfo == null || _itemsCanvas == null)
        {
            return;
        }

        _logger.Debug("[SimpleTrackControl] 初始化所有片段: 目标数={TargetCount}", _trackInfo.Segments.Count);

        foreach (var segment in _trackInfo.Segments)
        {
            var control = CreateSegmentControl(segment);
            _itemsCanvas.Children.Add(control);
            _segmentControls[segment] = control;
        }
    }

    private void OnSegmentAdded(Clip clip)
    {
        if (_itemsCanvas == null || _trackInfo == null)
        {
            return;
        }

        _logger.Debug("[SimpleTrackControl] 片段新增: Index={Index}, Start={Start}", clip.Index, clip.Start);

        var control = CreateSegmentControl(clip);
        _itemsCanvas.Children.Add(control);
        _segmentControls[clip] = control;
    }

    private void OnSegmentRemoved(Clip clip)
    {
        if (!_segmentControls.TryGetValue(clip, out var control))
        {
            return;
        }

        _logger.Debug("[SimpleTrackControl] 片段删除: Index={Index}", clip.Index);

        _itemsCanvas?.Children.Remove(control);
        _segmentControls.Remove(clip);
    }

    #endregion

    #region UI更新

    private void UpdateUI()
    {
        UpdateCanvasWidth();
        UpdateTrackHeight();
    }

    private void UpdateCanvasWidth()
    {
        if (_itemsCanvas == null)
        {
            return;
        }

        if (_isLayoutUpdatePending)
        {
            return;
        }

        _isLayoutUpdatePending = true;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            var canvasWidth = _totalDuration * _zoomFactor * 100.0;
            _itemsCanvas.Width = canvasWidth;
            _isLayoutUpdatePending = false;

            _logger.Debug("[SimpleTrackControl] 更新 Canvas 宽度: Width={Width}", canvasWidth);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateTrackHeight()
    {
        if (_trackInfo == null)
        {
            return;
        }

        if (_trackInfo.Height > 0)
        {
            Height = _trackInfo.Height;
            _logger.Debug("[SimpleTrackControl] 更新轨道高度: Title={Title}, Height={Height}", _trackInfo.Title, _trackInfo.Height);
        }
        else if (Height <= 0)
        {
            Height = 50;
            _logger.Debug("[SimpleTrackControl] 设置默认轨道高度: Title={Title}, Height={Height}", _trackInfo.Title, 50);
        }
    }

    private void UpdateAllSegments()
    {
        foreach (var control in _segmentControls.Values)
        {
            control.ZoomFactor = _zoomFactor;
            control.TotalDuration = _totalDuration;
        }
    }

    private void UpdateAllSegmentsServices()
    {
        foreach (var control in _segmentControls.Values)
        {
            control.AudioPlayerService = _audioPlayerService;
        }
    }

    private void UpdateAllSegmentsViewModel()
    {
        foreach (var control in _segmentControls.Values)
        {
            control.ViewModel = _viewModel;
        }
    }

    private void UpdateAllSegmentsColor()
    {
        if (_trackInfo == null)
        {
            return;
        }

        var trackColor = _trackInfo.Color;
        _logger.Debug("[SimpleTrackControl] 更新所有片段颜色: TrackTitle={TrackTitle}, TrackColor={TrackColor}, SegmentCount={SegmentCount}", 
            _trackInfo.Title, trackColor, _segmentControls.Count);

        foreach (var control in _segmentControls.Values)
        {
            control.RowColor = trackColor;
        }
    }

    #endregion

    #region 创建片段控件

    private SimpleSegmentControl CreateSegmentControl(Clip clip)
    {
        var trackColor = _trackInfo?.Color ?? "#00FF00";
        _logger.Debug("[SimpleTrackControl] 创建片段控件: ClipIndex={ClipIndex}, TrackTitle={TrackTitle}, TrackColor={TrackColor}", 
            clip.Index, _trackInfo?.Title, trackColor);

        var control = new SimpleSegmentControl
        {
            Clip = clip,
            ZoomFactor = _zoomFactor,
            TotalDuration = _totalDuration,
            AudioPlayerService = _audioPlayerService,
            ViewModel = _viewModel,
            CanPlay = clip is AudioClip audioClip && !string.IsNullOrEmpty(audioClip.FilePath),
            Height = _trackInfo?.Height ?? 50,
            RowColor = trackColor
        };

        control.PlayRequested += OnSegmentPlayRequested;

        var left = (clip as ISpeechSegment) .StartSeconds * _zoomFactor * 100.0;
        var width = clip.Duration * _zoomFactor * 100.0;

        Canvas.SetLeft(control, left);
        Canvas.SetTop(control, 0);
        control.Width = Math.Max(0, width);

        _logger.Debug("[SimpleTrackControl] 创建片段控件: Index={Index}, Left={Left}, Width={Width}", clip.Index, left, width);

        return control;
    }

    private void OnSegmentPlayRequested(object? sender, Clip clip)
    {
        SegmentPlayRequested?.Invoke(this, clip);
    }

    #endregion
}
