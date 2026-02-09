using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Serilog;
using TrackMenuAttributes;
using VT.Module.BusinessObjects;
using TimeLine.Services;
using DevExpress.ExpressApp;
using VT.Module;

namespace TimeLine.Controls;

/// <summary>
/// 菜单项工厂，使用反射创建基于特性的菜单项
/// </summary>
public class TrackContextMenuFactory
{
    private readonly ILogger _logger = LoggerService.ForContext<TrackContextMenuFactory>();

    /// <summary>
    /// 为指定的轨道对象创建菜单项
    /// </summary>
    /// <param name="track">轨道对象</param>
    /// <param name="viewModel">视图模型（可选）</param>
    /// <returns>菜单项列表</returns>
    public List<Control> CreateMenuItems(TrackInfo track, VideoEditorContext? viewModel)
    {
        if (track == null)
        {
            _logger.Warning("[TrackContextMenuFactory] Track 对象为空，无法创建菜单项");
            return new List<Control>();
        }

        _logger.Debug("[TrackContextMenuFactory] 开始为轨道创建菜单项: TrackType={TrackType}, Title={Title}",
            track.TrackType, track.Title);

        var menuItems = new List<Control>();

        #region 获取标记了 ContextMenuAction 的方法

        var trackType = track.GetType();
        var methods = trackType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        var actionMethods = methods
            .Where(m => m.DeclaringType != typeof(object))
            .Select(m => new
            {
                Method = m,
                Attribute = m.GetCustomAttribute<ContextMenuActionAttribute>()
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Attribute.Order)
            .ThenBy(x => x.Attribute.DisplayName)
            .ToList();

        #endregion

        #region 按分组创建菜单项

        var groupedMethods = actionMethods.GroupBy(x => x.Attribute.Group ?? "Default");

        foreach (var group in groupedMethods)
        {
            if (menuItems.Count > 0 && group.Key != "Default")
            {
                menuItems.Add(new Separator());
            }

            foreach (var item in group)
            {
                var menuItem = CreateMenuItem(track, item.Method, item.Attribute, viewModel);
                if (menuItem != null)
                {
                    menuItems.Add(menuItem);
                }
            }
        }

        #endregion

        _logger.Debug("[TrackContextMenuFactory] 菜单项创建完成: Count={Count}", menuItems.Count);

        return menuItems;
    }

    /// <summary>
    /// 创建单个菜单项
    /// </summary>
    private MenuItem? CreateMenuItem(TrackInfo track, MethodInfo method, ContextMenuActionAttribute attribute, VideoEditorContext? viewModel)
    {
        try
        {
            _logger.Debug("[TrackContextMenuFactory] 创建菜单项: DisplayName={DisplayName}, Method={Method}",
                attribute.DisplayName, method.Name);

            var menuItem = new MenuItem
            {
                Header = attribute.DisplayName,
                IsEnabled = attribute.IsEnabled
            };

            if (!string.IsNullOrEmpty(attribute.Icon))
            {
                menuItem.Icon = new TextBlock { Text = attribute.Icon };
            }

            if (!string.IsNullOrEmpty(attribute.Tooltip))
            {
                menuItem.ToolTip = attribute.Tooltip;
            }

            menuItem.Click += async (sender, e) =>
            {
                try
                {
                    await ExecuteMenuAction(track, method, attribute, viewModel);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[TrackContextMenuFactory] 执行菜单操作失败: DisplayName={DisplayName}, Method={Method}",
                        attribute.DisplayName, method.Name);
                        throw;
                }
            };

            return menuItem;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[TrackContextMenuFactory] 创建菜单项失败: Method={Method}", method.Name);
            throw;
        }
    }

    /// <summary>
    /// 执行菜单操作
    /// </summary>
    private async Task ExecuteMenuAction(TrackInfo track, MethodInfo method, ContextMenuActionAttribute attribute, VideoEditorContext? viewModel)
    {
        _logger.Information("[TrackContextMenuFactory] 执行菜单操作: Method={Method}, Track={Track}",
            method.Name, track.Title);

        var parameters = method.GetParameters();
        object?[]? args = null;

        #region 准备方法参数

        if (parameters.Length == 0)
        {
            args = null;
        }
        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(VideoEditorContext))
        {
            args = new object[] { viewModel };
        }
        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object))
        {
            args = new object[] { viewModel };
        }
        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IObjectSpace))
        {
            args = new object[] { viewModel?.ObjectSpace };
        }
        else
        {
            _logger.Warning("[TrackContextMenuFactory] 方法参数不支持: Method={Method}, Parameters={Parameters}",
                method.Name, string.Join(", ", parameters.Select(p => p.ParameterType.Name)));
            return;
        }

        #endregion

        #region 执行方法

        var result = method.Invoke(track, args);
        if (result is Task task)
        {
            await task;
        }

        #endregion

        #region 自动提交更改

        if (attribute.IsAutoCommit && viewModel?.ObjectSpace != null)
        {
            try
            {
                _logger.Debug("[TrackContextMenuFactory] 自动提交更改: Method={Method}", method.Name);
                viewModel.ObjectSpace.CommitChanges();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[TrackContextMenuFactory] 自动提交更改失败: Method={Method}", method.Name);
            }
        }

        #endregion
    }
}
