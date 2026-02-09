﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Serilog;
using IOPath = System.IO.Path;

namespace TimeLine.Controls;

public class RulerScrollBar : ScrollBar
{
    #region 字段

    private RulerCanvas? _rulerCanvas;
    private double _totalDuration = 100;
    private double _zoomFactor = 1.0;
    public static bool WriteRulerLogToFile = false;
    private static readonly ILogger? _logger = CreateRulerLogger();

    #endregion

    #region 构造函数

    static RulerScrollBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RulerScrollBar),new FrameworkPropertyMetadata(typeof(RulerScrollBar)));
    }

    public RulerScrollBar()
    {
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    #endregion

    #region 日志初始化

    private static ILogger? CreateRulerLogger()
    {
        if (!WriteRulerLogToFile)
            return null;
        var logDirectory = @"d:\VideoTranslator\logs";
        if (!System.IO.Directory.Exists(logDirectory))
        {
            System.IO.Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = IOPath.Combine(logDirectory, $"ruler_{timestamp}.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[Ruler] {Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        logger.Information("Ruler 独立日志系统初始化完成，日志文件: {LogFilePath}", logFilePath);
        return logger;
    }

    #endregion

    #region 公共属性

    public static readonly DependencyProperty TotalDurationProperty =
        DependencyProperty.Register(nameof(TotalDuration), typeof(double), typeof(RulerScrollBar),
            new PropertyMetadata(100.0, OnTotalDurationChanged));

    public double TotalDuration
    {
        get => (double)GetValue(TotalDurationProperty);
        set => SetValue(TotalDurationProperty, value);
    }

    private static void OnTotalDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RulerScrollBar ruler)
        {
            _logger?.Debug("TotalDuration 属性变更: Old={Old}, New={New}", e.OldValue, e.NewValue);
            ruler._totalDuration = (double)e.NewValue;
            ruler.UpdateRuler();
        }
    }

    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(RulerScrollBar),
            new PropertyMetadata(1.0, OnZoomFactorChanged));

    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RulerScrollBar ruler)
        {
            _logger?.Debug("ZoomFactor 属性变更: Old={Old}, New={New}", e.OldValue, e.NewValue);
            ruler._zoomFactor = (double)e.NewValue;
            ruler.UpdateRuler();
        }
    }

    #endregion

    #region 事件处理

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _rulerCanvas = GetTemplateChild("RulerCanvas") as RulerCanvas;
        ValueChanged += OnScrollBarValueChanged;
        UpdateRuler();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateRuler();
    }

    private void OnScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateRuler();
    }

    #endregion

    #region 标尺绘制

    private void UpdateRuler()
    {
        if (_rulerCanvas == null)
        {
            return;
        }

        _rulerCanvas.UpdateRuler(_totalDuration, _zoomFactor, Value, ActualWidth, ActualHeight);
    }

    #endregion
}

#region 自定义标尺Canvas

public class RulerCanvas : Canvas
{
    private double _totalDuration = 100;
    private double _zoomFactor = 1.0;
    private double _scrollOffset = 0;
    private double _viewportWidth = 0;
    private double _viewportHeight = 0;

    private static readonly Typeface _textTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Brush _majorTickBrush = CreateFrozenBrush(Color.FromRgb(74, 144, 226));
    private static readonly Brush _minorTickBrush = CreateFrozenBrush(Color.FromRgb(149, 165, 180));
    private static readonly Brush _textBrush = CreateFrozenBrush(Color.FromRgb(51, 51, 51));
    private static readonly Brush _textOutlineBrush = CreateFrozenBrush(Color.FromRgb(255, 255, 255));
    private static readonly Pen _majorTickPen = new Pen(_majorTickBrush, 1.5);
    private static readonly Pen _minorTickPen = new Pen(_minorTickBrush, 1.0);

    static RulerCanvas()
    {
        _majorTickPen.Freeze();
        _minorTickPen.Freeze();
    }

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public void UpdateRuler(double totalDuration, double zoomFactor, double scrollOffset, double viewportWidth, double viewportHeight)
    {
        _totalDuration = totalDuration;
        _zoomFactor = zoomFactor;
        _scrollOffset = scrollOffset;
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (_viewportWidth <= 0 || _viewportHeight <= 0)
        {
            return;
        }

        var rulerHeight = _viewportHeight;
        var minorTickHeight = rulerHeight * 0.3;
        var majorTickHeight = rulerHeight * 0.6;
        var textTopMargin = 2.0;

        var interval = CalculateRulerInterval(_totalDuration);
        var pixelsPerSecond = _zoomFactor * 100.0;
        var startTime = _scrollOffset / pixelsPerSecond;
        var endTime = (_scrollOffset + _viewportWidth) / pixelsPerSecond;

        var startTick = (int)(startTime / interval);
        var endTick = (int)(endTime / interval) + 1;

        for (int i = startTick; i <= endTick; i++)
        {
            var time = i * interval;
            if (time < 0 || time > _totalDuration)
            {
                continue;
            }

            var x = time * _zoomFactor * 100.0 - _scrollOffset;

            var isMajor = i % 5 == 0;
            var tickHeight = isMajor ? majorTickHeight : minorTickHeight;

            var tickPen = isMajor ? _majorTickPen : _minorTickPen;

            drawingContext.DrawLine(
                tickPen,
                new Point(x, rulerHeight - tickHeight),
                new Point(x, rulerHeight));

            if (isMajor)
            {
                var timeText = FormatTime(time);
                var textPosition = new Point(x - 20, textTopMargin);
                var formattedText = new FormattedText(
                    timeText,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _textTypeface,
                    11,
                    _textBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                var geometry = formattedText.BuildGeometry(textPosition);

                drawingContext.DrawGeometry(_textOutlineBrush, new Pen(_textOutlineBrush, 3), geometry);
                drawingContext.DrawText(formattedText, textPosition);
            }
        }
    }

    private double CalculateRulerInterval(double totalDuration)
    {
        if (totalDuration <= 60)
        {
            return 1.0;
        }
        else if (totalDuration <= 300)
        {
            return 5.0;
        }
        else if (totalDuration <= 600)
        {
            return 10.0;
        }
        else if (totalDuration <= 1800)
        {
            return 30.0;
        }
        else
        {
            return 60.0;
        }
    }

    private string FormatTime(double seconds)
    {
        var minutes = (int)(seconds / 60);
        var secs = (int)(seconds % 60);
        return $"{minutes}:{secs:D2}";
    }
}

#endregion
