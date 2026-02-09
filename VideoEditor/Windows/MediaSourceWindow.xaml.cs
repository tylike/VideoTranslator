using Microsoft.Win32;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VT.Module.BusinessObjects;
using VideoEditor.Services;
using DevExpress.ExpressApp;
using Window = System.Windows.Window;

namespace VideoEditor.Windows;

public partial class MediaSourceWindow : Window
{
    #region 字段

    private readonly VideoProject _project;
    private static readonly ILogger _logger = Log.ForContext<MediaSourceWindow>();
    private readonly MediaSourceContextMenuFactory _contextMenuFactory;
    IObjectSpace objectSpace;
    #endregion

    #region 构造函数

    public MediaSourceWindow(VideoProject project,IObjectSpace objectspace)
    {
        this.objectSpace = objectspace;
        InitializeComponent();
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _contextMenuFactory = new MediaSourceContextMenuFactory();
        
        Loaded += MediaSourceWindow_Loaded;
    }

    #endregion

    #region 窗体事件

    private void MediaSourceWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.Information("媒体源窗口已加载");
        LoadMediaSources();
    }

    private void MediaSourceListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            if (mediaSourceListView.SelectedItem is MediaSource selectedSource)
            {
                _logger.Debug("选中媒体源: {Name}", selectedSource.Name);
                UpdateDynamicToolBar(selectedSource);
            }
            else
            {
                _logger.Debug("取消选中媒体源");
                ClearDynamicToolBar();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理选中事件时发生错误");
        }
    }

    #endregion

    #region 工具栏按钮事件

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("用户点击导入按钮");

            var openFileDialog = new OpenFileDialog
            {
                Title = "选择媒体文件",
                Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv|音频文件|*.mp3;*.wav;*.flac;*.aac|字幕文件|*.srt;*.ass;*.vtt|所有文件|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _logger.Information("用户选择了 {Count} 个文件", openFileDialog.FileNames.Length);

                foreach (var filePath in openFileDialog.FileNames)
                {
                    ImportMediaFile(filePath);
                }

                LoadMediaSources();
                _logger.Information("媒体文件导入完成");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导入媒体文件时发生错误");
            MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("用户点击删除按钮");

            if (mediaSourceListView.SelectedItem is MediaSource selectedSource)
            {
                var result = MessageBox.Show(
                    $"确定要删除媒体源 \"{selectedSource.Name}\" 吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _project.MediaSources.Remove(selectedSource);
                    _project.Session.Delete(selectedSource);
                    _project.Session.Save(_project);
                    
                    LoadMediaSources();
                    _logger.Information("媒体源已删除: {Name}", selectedSource.Name);
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的媒体源", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "删除媒体源时发生错误");
            MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditName_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("用户点击编辑名称按钮");

            if (mediaSourceListView.SelectedItem is MediaSource selectedSource)
            {
                var inputDialog = new InputDialog("编辑名称", "请输入新的名称:", selectedSource.Name);
                
                if (inputDialog.ShowDialog() == true)
                {
                    var newName = inputDialog.InputText;
                    
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        selectedSource.Name = newName;
                        _project.Session.Save(_project);
                        
                        LoadMediaSources();
                        _logger.Information("媒体源名称已更新: {OldName} -> {NewName}", selectedSource.Name, newName);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择要编辑的媒体源", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "编辑媒体源名称时发生错误");
            MessageBox.Show($"编辑失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 私有方法

    private void LoadMediaSources()
    {
        try
        {
            mediaSourceListView.ItemsSource = _project.MediaSources.OrderBy(m => m.MediaType).ThenBy(m => m.Name).ToList();
            _logger.Information("已加载 {Count} 个媒体源", _project.MediaSources.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载媒体源列表时发生错误");
        }
    }

    private void UpdateDynamicToolBar(MediaSource mediaSource)
    {
        try
        {
            _logger.Debug("更新动态工具栏: MediaSource={Name}", mediaSource.Name);

            dynamicToolBar.Items.Clear();
            dynamicToolBar.Items.Add(new TextBlock { Text = "选中操作: ", VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new System.Windows.Thickness(5, 0, 10, 0) });

            var menuItems = _contextMenuFactory.CreateMenuItems(mediaSource, this.objectSpace);

            if (menuItems.Count > 0)
            {
                var groupedMenuItems = menuItems
                    .Where(m => m is not Separator)
                    .Cast<MenuItem>()
                    .GroupBy(m => m.Header?.ToString() ?? "")
                    .ToList();

                foreach (var menuItem in menuItems)
                {
                    if (menuItem is Separator)
                    {
                        dynamicToolBar.Items.Add(new Separator());
                    }
                    else if (menuItem is MenuItem menu)
                    {
                        var button = new Button
                        {
                            Content = menu.Header,
                            IsEnabled = menu.IsEnabled,
                            ToolTip = menu.Header,
                            Height = 25,
                            VerticalAlignment = System.Windows.VerticalAlignment.Center
                        };

                        button.Click += async (s, e) =>
                        {
                            try
                            {
                                menu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "执行动态工具栏按钮操作失败");
                            }
                        };

                        dynamicToolBar.Items.Add(button);
                    }
                }

                _logger.Debug("动态工具栏已更新: Count={Count}", menuItems.Count);
            }
            else
            {
                _logger.Debug("没有可用的上下文菜单项");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "更新动态工具栏时发生错误");
        }
    }

    private void ClearDynamicToolBar()
    {
        try
        {
            dynamicToolBar.Items.Clear();
            dynamicToolBar.Items.Add(new TextBlock { Text = "选中操作: ", VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new System.Windows.Thickness(5, 0, 10, 0) });
            _logger.Debug("动态工具栏已清空");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "清空动态工具栏时发生错误");
        }
    }

    private void ImportMediaFile(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLower();
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            MediaSource? mediaSource = null;

            #region 根据文件类型创建对应的媒体源

            switch (extension)
            {
                case ".mp4":
                case ".avi":
                case ".mkv":
                case ".mov":
                case ".wmv":
                    mediaSource = new VideoSource(_project.Session)
                    {
                        Name = fileName,
                        FileFullName = filePath,
                        MediaType = MediaType.Video
                    };
                    _logger.Information("创建视频源: {FilePath}", filePath);
                    break;

                case ".mp3":
                case ".wav":
                case ".flac":
                case ".aac":
                    mediaSource = new AudioSource(_project.Session)
                    {
                        Name = fileName,
                        FileFullName = filePath,
                        MediaType = MediaType.Audio
                    };
                    _logger.Information("创建音频源: {FilePath}", filePath);
                    break;

                case ".srt":
                case ".ass":
                case ".vtt":
                    mediaSource = new SRTSource(_project.Session)
                    {
                        Name = fileName,
                        FileFullName = filePath,
                        MediaType = MediaType.Subtitles
                    };
                    _logger.Information("创建字幕源: {FilePath}", filePath);
                    break;

                default:
                    _logger.Warning("不支持的文件类型: {Extension}", extension);
                    break;
            }

            #endregion

            if (mediaSource != null)
            {
                _project.MediaSources.Add(mediaSource);
                _project.Session.Save(_project);
                _logger.Information("媒体源已添加到项目: {Name}", mediaSource.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导入媒体文件时发生错误: {FilePath}", filePath);
            throw;
        }
    }

    #endregion
}
