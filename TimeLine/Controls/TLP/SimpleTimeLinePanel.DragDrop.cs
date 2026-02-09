using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Serilog;
using VT.Module.BusinessObjects;

namespace TimeLine.Controls;

public partial class SimpleTimeLinePanel
{
    #region 拖拽状态

    private TrackHeaderControl? _draggedHeader;
    private TrackInfo? _draggedTrack;
    private double _dragStartY;
    private bool _isDragging = false;
    private int _dragStartIndex = -1;
    private int _lastTargetIndex = -1;

    #endregion

    #region 编辑状态

    private DateTime _lastClickTime = DateTime.MinValue;
    private TrackHeaderControl? _lastClickedHeader;

    #endregion

    #region 拖拽实现

    private void OnTrackHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TrackHeaderControl headerControl || headerControl.TrackInfo == null)
        {
            return;
        }

        var currentTime = DateTime.Now;
        var timeSinceLastClick = currentTime - _lastClickTime;

        if (timeSinceLastClick.TotalMilliseconds < 500 && _lastClickedHeader == headerControl)
        {
            StartEditing(headerControl);
            e.Handled = true;
            _lastClickTime = DateTime.MinValue;
            _lastClickedHeader = null;
            return;
        }

        _lastClickTime = currentTime;
        _lastClickedHeader = headerControl;

        _draggedHeader = headerControl;
        _draggedTrack = headerControl.TrackInfo;
        _dragStartY = e.GetPosition(headerControl).Y;
        _isDragging = false;

        _logger.Debug("[SimpleTimeLinePanel] 鼠标按下: Title={Title}", headerControl.Title);
    }

    private void OnTrackHeaderMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedHeader == null || _draggedTrack == null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (!_isDragging)
        {
            var currentPosition = e.GetPosition(_draggedHeader);
            var deltaY = Math.Abs(currentPosition.Y - _dragStartY);

            if (deltaY > 3)
            {
                _isDragging = true;
                _draggedHeader.Background = new SolidColorBrush(Color.FromRgb(230, 240, 255));
                _draggedHeader.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                _draggedHeader.BorderThickness = new Thickness(2);
                _draggedHeader.Opacity = 0.8;

                if (tracks != null)
                {
                    _dragStartIndex = tracks.IndexOf(_draggedTrack);
                }

                _logger.Debug("[SimpleTimeLinePanel] 拖拽开始: Title={Title}, Index={Index}", _draggedTrack.Title, _dragStartIndex);
            }
        }

        if (_isDragging)
        {
            if (_leftPanelStack == null || _rightPanelStack == null || tracks == null)
            {
                return;
            }

            var currentY = e.GetPosition(_leftPanelStack).Y;
            var targetIndex = FindTargetIndex(currentY);

            if (targetIndex >= 0 && targetIndex != _lastTargetIndex)
            {
                MoveTrack(_dragStartIndex, targetIndex);
                _dragStartIndex = targetIndex;
                _lastTargetIndex = targetIndex;
            }
        }
    }

    private void OnTrackHeaderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _logger.Debug("[SimpleTimeLinePanel] 拖拽结束: Title={Title}", _draggedTrack?.Title);

        if (_draggedHeader != null)
        {
            _draggedHeader.Background = Brushes.Transparent;
            _draggedHeader.BorderBrush = null;
            _draggedHeader.BorderThickness = new Thickness(0);
            _draggedHeader.Opacity = 1.0;
        }

        _draggedHeader = null;
        _draggedTrack = null;
        _dragStartIndex = -1;
        _lastTargetIndex = -1;
        _isDragging = false;
    }

    private void OnTrackHeaderMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            _draggedHeader = null;
            _draggedTrack = null;
        }
    }

    private int FindTargetIndex(double y)
    {
        if (_leftPanelStack == null || tracks == null)
        {
            return -1;
        }

        double currentY = 0;
        for (int i = 0; i < _leftPanelStack.Children.Count; i++)
        {
            if (_leftPanelStack.Children[i] is TrackHeaderControl header)
            {
                currentY += header.ActualHeight;
                if (y < currentY)
                {
                    return i;
                }
            }
        }

        return _leftPanelStack.Children.Count - 1;
    }

    private void MoveTrack(int fromIndex, int toIndex)
    {
        if (tracks == null || _leftPanelStack == null || _rightPanelStack == null)
        {
            return;
        }

        if (fromIndex < 0 || toIndex < 0 || fromIndex >= tracks.Count || toIndex >= tracks.Count)
        {
            return;
        }

        if (fromIndex == toIndex)
        {
            return;
        }

        var trackControl = tracks.TrackControls[fromIndex];
        var trackInfo = trackControl.Info;

        tracks.Remove(trackControl);

        if (toIndex >= tracks.TrackControls.Count)
        {
            tracks.Add(trackControl);
        }
        else
        {
            tracks.TrackControls.Insert(toIndex, trackControl);
            _leftPanelStack.Children.Insert(toIndex, trackControl.Header);
            _rightPanelStack.Children.Insert(toIndex, trackControl.Control);
        }

        UpdateTrackIndexes();

        _logger.Debug("[SimpleTimeLinePanel] 移动轨道: From={FromIndex}, To={ToIndex}, Title={Title}", fromIndex, toIndex, trackInfo.Title);
    }

    private void UpdateTrackIndexes()
    {
        if (tracks == null)
        {
            return;
        }

        for (int i = 0; i < tracks.TrackControls.Count; i++)
        {
            tracks.TrackControls[i].Info.Index = i;
        }
    }

    #endregion

    #region 编辑功能

    private void StartEditing(TrackHeaderControl headerControl)
    {
        _logger.Debug("[SimpleTimeLinePanel] 开始编辑: Title={Title}", headerControl.Title);
    }

    #endregion
}
