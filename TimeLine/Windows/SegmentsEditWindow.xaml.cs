using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpo;
using Serilog;
using TimeLine.Services;
using VT.Module.BusinessObjects;

namespace TimeLine.Windows;

public partial class SegmentsEditWindow : Window
{
    #region 字段

    private readonly TrackInfo _trackInfo;
    private readonly ObservableCollection<ClipViewModel> _viewModels;
    private readonly ILogger _logger = LoggerService.ForContext<SegmentsEditWindow>();

    #endregion

    #region 构造函数

    public SegmentsEditWindow(TrackInfo trackInfo)
    {
        InitializeComponent();
        _trackInfo = trackInfo;
        _viewModels = new ObservableCollection<ClipViewModel>();
        
        foreach (var clip in trackInfo.Segments)
        {
            _viewModels.Add(new ClipViewModel(clip));
        }
        
        segmentsDataGrid.ItemsSource = _viewModels;
        _logger.Debug("[SegmentsEditWindow] 窗口初始化完成，片段数量: {Count}", _viewModels.Count);
    }

    #endregion

    #region 事件处理

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var session = _trackInfo.Session;
            Clip? newClip;
            
            if (_trackInfo is AudioTrackInfo)
            {
                newClip = new AudioClip(session);
            }
            else if (_trackInfo is SRTTrackInfo)
            {
                newClip = new SRTClip(session);
            }
            else if (_trackInfo is VadTrackInfo)
            {
                newClip = new VadClip(session);
            }
            else if (_trackInfo is TTSTrackInfo)
            {
                newClip = new TTSClip(session);
            }
            else if (_trackInfo is VideoTrackInfo)
            {
                newClip = new TimeScaleClip(session);
            }
            else
            {
                newClip = new SRTClip(session);
            }
            
            newClip.Track = _trackInfo;
            newClip.Index = _viewModels.Count;
            newClip.Start = TimeSpan.Zero;
            newClip.End = TimeSpan.FromSeconds(1);
            newClip.Text = string.Empty;
            
            var viewModel = new ClipViewModel(newClip);
            _viewModels.Add(viewModel);
            segmentsDataGrid.SelectedItem = viewModel;
            segmentsDataGrid.ScrollIntoView(viewModel);
            
            _logger.Debug("[SegmentsEditWindow] 添加新片段，索引: {Index}", newClip.Index);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[SegmentsEditWindow] 添加片段失败");
            MessageBox.Show($"添加片段失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedItems = segmentsDataGrid.SelectedItems;
            
            if (selectedItems == null || selectedItems.Count == 0)
            {
                MessageBox.Show("请先选择要删除的片段", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var itemsToDelete = new System.Collections.Generic.List<ClipViewModel>();
            foreach (var item in selectedItems)
            {
                if (item is ClipViewModel viewModel)
                {
                    itemsToDelete.Add(viewModel);
                }
            }
            
            var deletedIndices = new System.Collections.Generic.List<int>();
            foreach (var viewModel in itemsToDelete)
            {
                deletedIndices.Add(viewModel.Index);
                _viewModels.Remove(viewModel);
                var seg = _trackInfo.Segments.FirstOrDefault(x => x.Index == viewModel.Index);
                if (seg != null)
                {
                    this._trackInfo.Segments.Remove(seg);
                }
            }
            
            ReorderIndices();
            
            _logger.Debug("[SegmentsEditWindow] 删除片段，删除数量: {Count}, 索引: {Indices}", 
                deletedIndices.Count, string.Join(", ", deletedIndices));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[SegmentsEditWindow] 删除片段失败");
            MessageBox.Show($"删除片段失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyChanges();
            DialogResult = true;
            Close();
            _logger.Debug("[SegmentsEditWindow] 保存更改并关闭窗口");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[SegmentsEditWindow] 保存更改失败");
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
        _logger.Debug("[SegmentsEditWindow] 取消更改并关闭窗口");
    }

    private void SegmentsDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Column.DisplayIndex == 0)
        {
            e.Cancel = true;
        }
    }

    #endregion

    #region 私有方法

    private void ApplyChanges()
    {
        var segments = _trackInfo.Segments;
        
        foreach (var viewModel in _viewModels)
        {
            var clip = viewModel.Clip;
            clip.Index = viewModel.Index;
            clip.Start = viewModel.Start;
            clip.End = viewModel.End;
            clip.Text = viewModel.Text;
        }
        
        _trackInfo.Validate();
        _logger.Debug("[SegmentsEditWindow] 应用更改完成");
    }

    private void ReorderIndices()
    {
        for (int i = 0; i < _viewModels.Count; i++)
        {
            _viewModels[i].Index = i;
        }
    }

    #endregion

    #region 内部类

    private class ClipViewModel
    {
        #region 属性

        public Clip Clip { get; }

        public int Index
        {
            get => Clip.Index;
            set => Clip.Index = value;
        }

        public string Text
        {
            get => Clip.Text ?? string.Empty;
            set => Clip.Text = value;
        }

        public TimeSpan Start
        {
            get => Clip.Start;
            set => Clip.Start = value;
        }

        public TimeSpan End
        {
            get => Clip.End;
            set => Clip.End = value;
        }

        #endregion

        #region 构造函数

        public ClipViewModel(Clip clip)
        {
            Clip = clip;
        }

        #endregion
    }

    #endregion
}
