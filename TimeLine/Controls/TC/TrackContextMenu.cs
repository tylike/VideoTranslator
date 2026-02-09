using System.Windows.Controls;
using System.Windows;
using VT.Module.BusinessObjects;
using Serilog;
using TimeLine.Services;
using System.Threading.Tasks;
using VT.Module;

namespace TimeLine.Controls;

/// <summary>
/// 轨道上下文菜单，根据轨道对象上的特性标记动态生成菜单项
/// </summary>
public class TrackContextMenu : ContextMenu
{
    #region 字段

    private readonly TrackInfo _trackData;
    private readonly VideoEditorContext? _viewModel;
    private readonly ILogger _logger = LoggerService.ForContext<TrackContextMenu>();
    private readonly TrackContextMenuFactory _menuFactory;

    #endregion

    #region 构造函数

    private TrackContextMenu(TrackInfo trackData, VideoEditorContext? viewModel)
    {
        _trackData = trackData;
        _viewModel = viewModel;
        _menuFactory = new TrackContextMenuFactory();

        BuildMenu();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 创建轨道上下文菜单
    /// </summary>
    public static TrackContextMenu Create(TrackInfo trackData, VideoEditorContext? viewModel)
    {
        return new TrackContextMenu(trackData, viewModel);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 构建菜单
    /// </summary>
    private void BuildMenu()
    {
        Items.Clear();

        #region 使用反射创建基于特性的菜单项

        var menuItems = _menuFactory.CreateMenuItems(_trackData, _viewModel);
        foreach (var item in menuItems)
        {
            Items.Add(item);
        }

        #endregion

       

        _logger.Debug("[TrackContextMenu] 菜单构建完成: TrackTitle={Title}, ItemCount={Count}",
            _trackData.Title, Items.Count);
    }
    #endregion
}
