using System.Windows;
using Microsoft.Win32;
using VideoEditor.Windows;
using VT.Module.BusinessObjects;
using VideoTranslator.Interfaces;

namespace VideoEditor;

public partial class MainWindow
{


    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("打开项目菜单项被点击");
            var projectSelectionWindow = new ProjectSelectionWindow(_objectSpace!)
            {
                Owner = this
            };

            if (projectSelectionWindow.ShowDialog() == true)
            {
                var selectedProject = projectSelectionWindow.SelectedProject;
                if (selectedProject != null)
                {
                    LoadProject(selectedProject);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "打开项目失败");
            MessageBox.Show($"打开项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("编辑项目菜单项被点击");

            if (_currentProject == null)
            {
                MessageBox.Show("请先打开一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editProjectWindow = new Windows.EditProjectWindow(_currentProject, _objectSpace!)
            {
                Owner = this
            };

            if (editProjectWindow.ShowDialog() == true)
            {
                _logger.Information("项目信息已更新");
                UpdateStatus("项目信息已更新");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "编辑项目失败");
            MessageBox.Show($"编辑项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("保存项目菜单项被点击");

            if (_objectSpace == null)
            {
                MessageBox.Show("未初始化 ObjectSpace", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _objectSpace.CommitChanges();
            _logger.Information("项目已保存");

            #region 保存调试信息到日志文件

            SaveProjectDebugInfo();

            #endregion

            MessageBox.Show("项目保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存项目失败");
            MessageBox.Show($"保存项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportSrt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("导入SRT菜单项被点击");

            if (_currentProject == null)
            {
                MessageBox.Show("请先打开或创建一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "SRT字幕文件 (*.srt)|*.srt|所有文件 (*.*)|*.*",
                Title = "选择SRT字幕文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var srtFilePath = openFileDialog.FileName;
                _logger.Information("用户选择SRT文件: {FilePath}", srtFilePath);

                UpdateStatus("正在导入SRT字幕...");
#warning 导入时，没有vad?
                var srt = await _currentProject.ImportSrt(srtFilePath, MediaType.Subtitles,null);
                var refAudioSegTrack = _currentProject.Tracks.OfType<AudioTrackInfo>().FirstOrDefault(t => t.TrackType == MediaType.源音分段);
                foreach (SRTClip item in srt.Segments)
                {
                    item.TTSReference = refAudioSegTrack?.Segments.OfType<AudioClip>().FirstOrDefault(x=>x.Index == item.Index);
                }
                _objectSpace.CommitChanges();

                _logger.Information("SRT导入成功: {FilePath}", srtFilePath);
                UpdateStatus("SRT字幕导入成功");
                MessageBox.Show("SRT字幕导入成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导入SRT失败");
            UpdateStatus("就绪");
            MessageBox.Show($"导入SRT失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AutoProcessAudio_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Information("自动处理音频菜单项被点击");

            if (_currentProject == null)
            {
                MessageBox.Show("请先打开或创建一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "是否重新生成？\n\n选择\"是\"将清除所有处理数据，从源视频重新开始\n选择\"否\"将继续使用现有数据",
                "自动处理音频",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            bool regenerate = false;

            if (result == MessageBoxResult.Yes)
            {
                regenerate = true;
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            await ProcessProjectAudio(_currentProject, regenerate);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "自动处理音频失败");
            MessageBox.Show($"自动处理音频失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}