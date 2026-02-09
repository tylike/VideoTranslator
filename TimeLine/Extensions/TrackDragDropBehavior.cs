using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VT.Module.BusinessObjects;
using TimeLine.Services;
using Serilog;

namespace TimeLine.Extensions;

/// <summary>
/// Track拖拽行为，用于处理左侧面板的拖拽排序功能
/// </summary>
public class TrackDragDropBehavior
{
    #region Attached Properties

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TrackDragDropBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>
    /// 获取是否启用拖拽
    /// </summary>
    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    /// <summary>
    /// 设置是否启用拖拽
    /// </summary>
    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.RegisterAttached(
            "ItemsSource",
            typeof(ObservableCollection<TrackInfo>),
            typeof(TrackDragDropBehavior),
            new PropertyMetadata(null));

    /// <summary>
    /// 获取数据源集合
    /// </summary>
    public static ObservableCollection<TrackInfo>? GetItemsSource(DependencyObject obj)
    {
        return (ObservableCollection<TrackInfo>?)obj.GetValue(ItemsSourceProperty);
    }

    /// <summary>
    /// 设置数据源集合
    /// </summary>
    public static void SetItemsSource(DependencyObject obj, ObservableCollection<TrackInfo>? value)
    {
        obj.SetValue(ItemsSourceProperty, value);
    }

    #endregion

    #region Drag Drop State

    private static readonly Dictionary<Border, DragState> _dragStates = new();

    private class DragState
    {
        public Border DraggedItem { get; set; } = null!;
        public TrackInfo DraggedData { get; set; } = null!;
        public int DragStartIndex { get; set; }
        public double DragStartY { get; set; }
        public bool IsDragging { get; set; }
        public System.Windows.Media.Brush OriginalBackground { get; set; } = null!;
        public System.Windows.Media.Brush OriginalBorderBrush { get; set; } = null!;
        public Thickness OriginalBorderThickness { get; set; }
        public double OriginalOpacity { get; set; }
    }

    #endregion

    private static readonly ILogger _logger = LoggerService.ForContext<TrackDragDropBehavior>();

    #region Event Handlers

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border)
        {
            return;
        }

        var isEnabled = (bool)e.NewValue;

        if (isEnabled)
        {
            border.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            border.PreviewMouseMove += OnPreviewMouseMove;
            border.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            border.MouseLeave += OnMouseLeave;
        }
        else
        {
            border.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            border.PreviewMouseMove -= OnPreviewMouseMove;
            border.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            border.MouseLeave -= OnMouseLeave;
        }
    }

    /// <summary>
    /// 鼠标按下事件处理
    /// </summary>
    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not TrackInfo data)
        {
            return;
        }

        var itemsSource = GetItemsSource(border);
        if (itemsSource == null)
        {
            return;
        }

        var dragState = new DragState
        {
            DraggedItem = border,
            DraggedData = data,
            DragStartY = e.GetPosition(border).Y,
            DragStartIndex = itemsSource.IndexOf(data),
            IsDragging = false,
            OriginalBackground = border.Background,
            OriginalBorderBrush = border.BorderBrush,
            OriginalBorderThickness = border.BorderThickness,
            OriginalOpacity = border.Opacity
        };

        _dragStates[border] = dragState;

        _logger.Debug("[TrackDragDropBehavior] Mouse down on item: {Title}, Index: {Index}", data.Title, dragState.DragStartIndex);
    }

    /// <summary>
    /// 鼠标移动事件处理
    /// </summary>
    private static void OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Border border || !_dragStates.TryGetValue(border, out var dragState))
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (!dragState.IsDragging)
        {
            var currentPosition = e.GetPosition(border);
            var deltaY = Math.Abs(currentPosition.Y - dragState.DragStartY);

            if (deltaY > 3)
            {
                dragState.IsDragging = true;
                border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 240, 255));
                border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                border.BorderThickness = new Thickness(2);
                border.Opacity = 0.8;

                _logger.Debug("[TrackDragDropBehavior] Drag started for item: {Title}, Index: {Index}", dragState.DraggedData.Title, dragState.DragStartIndex);
            }
        }

        if (dragState.IsDragging)
        {
            var itemsSource = GetItemsSource(border);
            if (itemsSource == null)
            {
                return;
            }

            var parentPanel = FindParentPanel(border);
            if (parentPanel == null)
            {
                return;
            }

            var currentY = e.GetPosition(parentPanel).Y;
            var targetIndex = FindTargetIndex(itemsSource, currentY);

            if (targetIndex >= 0 && targetIndex != dragState.DragStartIndex)
            {
                MoveItem(itemsSource, dragState.DragStartIndex, targetIndex);
                dragState.DragStartIndex = targetIndex;
            }
        }
    }

    /// <summary>
    /// 鼠标释放事件处理
    /// </summary>
    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || !_dragStates.TryGetValue(border, out var dragState))
        {
            return;
        }

        ResetItemStyle(border, dragState);

        _logger.Debug("[TrackDragDropBehavior] Drag ended for item: {Title}", dragState.DraggedData.Title);

        _dragStates.Remove(border);
    }

    /// <summary>
    /// 鼠标离开事件处理
    /// </summary>
    private static void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Border border || !_dragStates.TryGetValue(border, out var dragState))
        {
            return;
        }

        if (!dragState.IsDragging)
        {
            _dragStates.Remove(border);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 查找父级面板
    /// </summary>
    private static System.Windows.Controls.Panel? FindParentPanel(DependencyObject child)
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is System.Windows.Controls.Panel panel)
            {
                return panel;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    /// <summary>
    /// 根据鼠标位置查找目标索引
    /// </summary>
    private static int FindTargetIndex(ObservableCollection<TrackInfo> itemsSource, double mouseY)
    {
        if (itemsSource.Count == 0)
        {
            return -1;
        }

        var currentY = 0.0;
        for (int i = 0; i < itemsSource.Count; i++)
        {
            var rowHeight = itemsSource[i].Height;
            if (mouseY >= currentY && mouseY < currentY + rowHeight)
            {
                return i;
            }
            currentY += rowHeight;
        }

        return itemsSource.Count - 1;
    }

    /// <summary>
    /// 移动项到新位置
    /// </summary>
    private static void MoveItem(ObservableCollection<TrackInfo> itemsSource, int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= itemsSource.Count || toIndex >= itemsSource.Count)
        {
            return;
        }

        var item = itemsSource[fromIndex];
        itemsSource.RemoveAt(fromIndex);
        itemsSource.Insert(toIndex, item);

        _logger.Debug("[TrackDragDropBehavior] Moved item from {FromIndex} to {ToIndex}", fromIndex, toIndex);
    }

    /// <summary>
    /// 重置项样式
    /// </summary>
    private static void ResetItemStyle(Border border, DragState dragState)
    {
        border.Background = dragState.OriginalBackground;
        border.BorderBrush = dragState.OriginalBorderBrush;
        border.BorderThickness = dragState.OriginalBorderThickness;
        border.Opacity = dragState.OriginalOpacity;
    }

    #endregion
}
