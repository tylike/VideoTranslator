using System.IO;
using System.Windows;
using VideoEditor.Windows;
using VT.Module;
using VT.Module.BusinessObjects;

namespace VideoEditor;

public partial class MainWindow
{

    private async void NewProject_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteNewProject(true);
    }
    private async void NewProjectManual_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteNewProject(false);
    }
    private async Task ExecuteNewProject(bool autoProcessAudio)
    {
        try
        {
            _logger.Information("新建项目菜单项被点击");            
            var newProjectWindow = new NewProjectWindow(_objectSpace!)
            {
                Owner = this
            };

            if (newProjectWindow.ShowDialog() == true)
            {
                var createdProject = newProjectWindow.CreatedProject;
                if (createdProject != null)
                {
                    LoadProject(createdProject);
                    await ProcessProjectAudio(createdProject);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "新建项目失败");
            MessageBox.Show($"新建项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<VideoProject?> CreateProjectDirectly()
    {
        try
        {
            _logger.Information("开始直接创建项目");

            var project = _objectSpace!.CreateObject<VideoProject>();
            project.ProjectName = $"DEBUG项目_{DateTime.Now:yyyyMMdd_HHmmss}";

            _objectSpace.CommitChanges();
            _logger.Information("项目已保存到数据库，Oid: {Oid}", project.Oid);

            project.Create();
            _logger.Information("项目目录已创建: {ProjectPath}", project.ProjectPath);

            project.ImportVideoFile(DEBUG_VIDEO_PATH);
            _logger.Information("视频已导入: {SourceVideoPath}", project.SourceVideoPath);

            _objectSpace.CommitChanges();
            _logger.Information("项目创建完成");

            return project;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "直接创建项目时发生错误");
            MessageBox.Show($"直接创建项目时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

}