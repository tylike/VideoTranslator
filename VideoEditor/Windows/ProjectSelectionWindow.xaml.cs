using System.Windows;
using VT.Module.BusinessObjects;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Serilog;
using WpfWindow = System.Windows.Window;

namespace VideoEditor.Windows;

public partial class ProjectSelectionWindow : WpfWindow
{
    #region 字段

    private readonly ILogger _logger = Log.ForContext<ProjectSelectionWindow>();

    #endregion

    #region 属性

    public VideoProject? SelectedProject => ProjectListBox.SelectedItem as VideoProject;

    #endregion

    #region 构造函数

    public ProjectSelectionWindow(IObjectSpace objectSpace)
    {
        InitializeComponent();
        InitializeProjectsFromObjectSpace(objectSpace);
    }

    public ProjectSelectionWindow(List<VideoProject> projects)
    {
        InitializeComponent();
        InitializeProjects(projects);
    }

    #endregion

    #region 初始化

    private void InitializeProjectsFromObjectSpace(IObjectSpace objectSpace)
    {
        try
        {
            var projects = objectSpace.GetObjectsQuery<VideoProject>().ToList();
            InitializeProjects(projects);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "从ObjectSpace加载项目失败");
        }
    }

    private void InitializeProjects(List<VideoProject> projects)
    {
        try
        {
            _logger.Information("初始化项目列表，项目数量: {Count}", projects.Count);
            ProjectListBox.ItemsSource = projects.OrderByDescending(p => p.Oid).ToList();

            if (projects.Count == 0)
            {
                _logger.Warning("没有项目");
                MessageBox.Show("数据库中没有项目，请先新建项目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = false;
                Close();
                return;
            }

            ProjectListBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "初始化项目列表失败");
        }
    }

    #endregion

    #region 事件处理

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProject == null)
        {
            _logger.Warning("未选择项目");
            MessageBox.Show("请选择一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _logger.Information("选择项目: {ProjectName} (Oid: {Oid})", SelectedProject.ProjectName, SelectedProject.Oid);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Information("取消选择项目");
        DialogResult = false;
        Close();
    }

    private void ProjectListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SelectedProject == null)
        {
            _logger.Warning("未选择项目");
            return;
        }

        _logger.Information("双击选择项目: {ProjectName} (Oid: {Oid})", SelectedProject.ProjectName, SelectedProject.Oid);
        DialogResult = true;
        Close();
    }

    #endregion
}
