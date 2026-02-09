﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TimeLine.Extensions;
using Serilog;
using Serilog.Events;
using System.IO;
using IOPath = System.IO.Path;

namespace TimeLine.Controls;

public partial class SimpleTimeLinePanel
{
    #region 字段

    private Canvas? _hoverIndicatorCanvas;
    private Line? _hoverLine;
    private Border? _hoverTimeBorder;
    private TextBlock? _hoverTimeTextBlock;
    private Grid? _rightContentGrid;
    private const int HoverDebounceMs = 100;
    private bool _isHoverIndicatorInitialized = false;
    private double _lastMouseX = -1;
    public static bool WriteHoverIndicatorLog = false;
    private static readonly ILogger? _hoverLogger = CreateHoverIndicatorLogger();

    #endregion

    #region 日志初始化

    private static ILogger? CreateHoverIndicatorLogger()
    {
        if (!WriteHoverIndicatorLog)
        {
            return null;
        }
        var logDirectory = @"d:\VideoTranslator\logs";
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = IOPath.Combine(logDirectory, $"hoverindicator_{timestamp}.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[HoverIndicator] {Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        logger.Information("HoverIndicator 独立日志系统初始化完成，日志文件: {LogFilePath}", logFilePath);
        return logger;
    }

    #endregion

    #region 初始化

    private void InitializeHoverIndicator()
    {
        Loaded += (s, e) =>
        {
            _hoverLogger?.Debug("Loaded 事件触发");

            if (_isHoverIndicatorInitialized)
            {
                _hoverLogger?.Debug("已初始化，跳过");
                return;
            }

            if (_rightScrollViewer == null)
            {
                _hoverLogger?.Debug("_rightScrollViewer 为空");
                return;
            }

            var parentGrid = _rightScrollViewer.Parent as Grid;
            if (parentGrid == null)
            {
                _hoverLogger?.Debug("parentGrid 为空");
                return;
            }

            _rightContentGrid = parentGrid;
            _hoverIndicatorCanvas = parentGrid.FindName("HoverIndicatorCanvas") as Canvas;
            _hoverLine = _hoverIndicatorCanvas?.FindName("HoverLine") as Line;
            _hoverTimeBorder = _hoverIndicatorCanvas?.FindName("HoverTimeBorder") as Border;
            _hoverTimeTextBlock = _hoverTimeBorder?.FindName("HoverTimeTextBlock") as TextBlock;

            _hoverLogger?.Debug("控件引用获取完成: _rightContentGrid={HasGrid}, _hoverIndicatorCanvas={HasCanvas}, _hoverLine={HasLine}",
                _rightContentGrid != null, _hoverIndicatorCanvas != null, _hoverLine != null);

            if (_rightContentGrid != null)
            {
                _rightContentGrid.PreviewMouseMove += OnRightContentGridPreviewMouseMove;
                _rightContentGrid.MouseLeave += OnRightContentGridMouseLeave;
                _hoverLogger?.Debug("已订阅鼠标事件到 _rightContentGrid");
            }

            if (_hoverIndicatorCanvas != null)
            {
                _hoverIndicatorCanvas.IsHitTestVisible = false;
                _hoverIndicatorCanvas.SizeChanged += OnHoverIndicatorCanvasSizeChanged;
                _hoverLogger?.Debug("Canvas 配置完成: IsHitTestVisible={IsHitTestVisible}",
                    _hoverIndicatorCanvas.IsHitTestVisible);
            }

            _isHoverIndicatorInitialized = true;
            _hoverLogger?.Debug("初始化完成");
        };
    }

    private void OnHoverIndicatorCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _hoverLogger?.Debug("Canvas SizeChanged: NewSize={NewWidth}x{NewHeight}, OldSize={OldWidth}x{OldHeight}",
            e.NewSize.Width, e.NewSize.Height, e.PreviousSize.Width, e.PreviousSize.Height);

        if (_hoverLine != null && _hoverIndicatorCanvas != null)
        {
            _hoverLine.Y2 = _hoverIndicatorCanvas.ActualHeight;
            _hoverLogger?.Debug("Line Y2 已更新为: {Y2}", _hoverLine.Y2);
        }
    }

    #endregion

    #region 鼠标事件处理

    private void OnRightContentGridPreviewMouseMove(object sender, MouseEventArgs e)
    {
        _hoverLogger?.Debug("===== 鼠标移动事件触发 =====");

        if (_rightContentGrid == null || _hoverIndicatorCanvas == null)
        {
            _hoverLogger?.Debug("鼠标移动事件: 控件未初始化 - _rightContentGrid={HasGrid}, _hoverIndicatorCanvas={HasCanvas}",
                _rightContentGrid != null, _hoverIndicatorCanvas != null);
            return;
        }

        var position = e.GetPosition(_rightContentGrid);
        var relativeX = position.X;

        _hoverLogger?.Debug("鼠标移动: X={X}, Y={Y}, relativeX={RelativeX}", position.X, position.Y, relativeX);

        if (Math.Abs(relativeX - _lastMouseX) < 1)
        {
            _hoverLogger?.Debug("鼠标移动距离过小，跳过更新");
            return;
        }

        _lastMouseX = relativeX;

        _hoverLogger?.Debug("触发防抖更新");
        this.Debounce(() => UpdateHoverIndicator(relativeX), HoverDebounceMs);
    }

    private void OnRightContentGridMouseLeave(object sender, MouseEventArgs e)
    {
        HideHoverIndicator();
    }

    #endregion

    #region 悬停指示线更新

    private void UpdateHoverIndicator(double relativeX)
    {
        _hoverLogger?.Debug("UpdateHoverIndicator 开始: relativeX={RelativeX}", relativeX);

        if (_hoverIndicatorCanvas == null || _hoverLine == null || _hoverTimeBorder == null || _hoverTimeTextBlock == null)
        {
            _hoverLogger?.Debug("控件为空，无法更新: Canvas={HasCanvas}, Line={HasLine}, Border={HasBorder}, TextBlock={HasTextBlock}",
                _hoverIndicatorCanvas != null, _hoverLine != null, _hoverTimeBorder != null, _hoverTimeTextBlock != null);
            return;
        }

        var scrollOffset = _rightScrollViewer?.HorizontalOffset ?? 0;
        var canvasOffset = _rightScrollViewer?.TranslatePoint(new Point(0, 0), _rightContentGrid).X ?? 0;
        var actualX = relativeX - canvasOffset + scrollOffset;

        _hoverLogger?.Debug("坐标计算: scrollOffset={ScrollOffset}, canvasOffset={CanvasOffset}, actualX={ActualX}",
            scrollOffset, canvasOffset, actualX);

        var timeInSeconds = CalculateTimeFromPosition(actualX);
        var formattedTime = FormatTime(timeInSeconds);

        _hoverLogger?.Debug("时间计算: timeInSeconds={TimeInSeconds}, formattedTime={FormattedTime}",
            timeInSeconds, formattedTime);

        _hoverLine.X1 = relativeX;
        _hoverLine.X2 = relativeX;

        var canvasHeight = _hoverIndicatorCanvas.ActualHeight;
        if (canvasHeight > 0)
        {
            _hoverLine.Y2 = canvasHeight;
        }
        else
        {
            _hoverLine.Y2 = 1000;
        }

        _hoverLogger?.Debug("Line 设置: X1={X1}, X2={X2}, Y1={Y1}, Y2={Y2}",
            _hoverLine.X1, _hoverLine.X2, _hoverLine.Y1, _hoverLine.Y2);

        _hoverIndicatorCanvas.Visibility = Visibility.Visible;

        Canvas.SetLeft(_hoverTimeBorder, relativeX + 5);
        Canvas.SetTop(_hoverTimeBorder, 5);

        _hoverLogger?.Debug("Border 位置: Left={Left}, Top={Top}",
            Canvas.GetLeft(_hoverTimeBorder), Canvas.GetTop(_hoverTimeBorder));

        _hoverTimeTextBlock.Text = formattedTime;

        _hoverLogger?.Debug("Canvas 信息: Width={Width}, Height={Height}, ActualWidth={ActualWidth}, ActualHeight={ActualHeight}",
            _hoverIndicatorCanvas.Width, _hoverIndicatorCanvas.Height,
            _hoverIndicatorCanvas.ActualWidth, _hoverIndicatorCanvas.ActualHeight);

        _hoverLogger?.Debug("Line 实际值: X1={X1}, X2={X2}, Y1={Y1}, Y2={Y2}",
            _hoverLine.X1, _hoverLine.X2, _hoverLine.Y1, _hoverLine.Y2);

        _hoverLogger?.Debug("Canvas.Children 数量: {Count}", _hoverIndicatorCanvas.Children.Count);

        _hoverLogger?.Debug("UpdateHoverIndicator 完成");
    }

    private void HideHoverIndicator()
    {
        if (_hoverIndicatorCanvas != null)
        {
            _hoverIndicatorCanvas.Visibility = Visibility.Collapsed;
        }
        _lastMouseX = -1;
    }

    #endregion

    #region 时间计算

    private double CalculateTimeFromPosition(double position)
    {
        if (_zoomFactor <= 0)
        {
            return 0;
        }

        var pixelsPerSecond = _zoomFactor * 100.0;
        var timeInSeconds = position / pixelsPerSecond;

        _hoverLogger?.Debug("时间计算: position={Position}, pixelsPerSecond={PixelsPerSecond}, timeInSeconds={TimeInSeconds}, _totalDuration={TotalDuration}",
            position, pixelsPerSecond, timeInSeconds, _totalDuration);

        return Math.Max(0, Math.Min(timeInSeconds, _totalDuration));
    }

    private string FormatTime(double seconds)
    {
        var totalSeconds = (int)seconds;
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var secs = totalSeconds % 60;
        var milliseconds = (int)((seconds - totalSeconds) * 1000);

        var result = "";

        if (hours > 0)
        {
            result = $"{hours}:{minutes:D2}:{secs:D2}";
        }
        else if (minutes > 0)
        {
            result = $"{minutes}:{secs:D2}";
        }
        else
        {
            result = $"{secs}";
        }

        if (milliseconds > 0)
        {
            result += $".{milliseconds:D3}";
        }

        return result;
    }

    #endregion
}
