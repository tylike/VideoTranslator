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
using DevExpress.ExpressApp;

namespace VideoEditor.Services;

/// <summary>
/// 媒体源菜单工厂，使用反射创建基于特性的菜单项
/// </summary>
public class MediaSourceContextMenuFactory
{
    private readonly ILogger _logger = LoggerService.ForContext<MediaSourceContextMenuFactory>();

    /// <summary>
    /// 为指定的媒体源对象创建菜单项
    /// </summary>
    /// <param name="mediaSource">媒体源对象</param>
    /// <param name="objectSpace">对象空间（可选）</param>
    /// <returns>菜单项列表</returns>
    public List<Control> CreateMenuItems(MediaSource mediaSource, IObjectSpace? objectSpace)
    {
        if (mediaSource == null)
        {
            _logger.Warning("[MediaSourceContextMenuFactory] MediaSource 对象为空，无法创建菜单项");
            return new List<Control>();
        }

        _logger.Debug("[MediaSourceContextMenuFactory] 开始为媒体源创建菜单项: Type={Type}, Name={Name}",
            mediaSource.GetType().Name, mediaSource.Name);

        var menuItems = new List<Control>();

        #region 获取标记了 ContextMenuAction 的方法

        var mediaSourceType = mediaSource.GetType();
        var methods = mediaSourceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

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
                var menuItem = CreateMenuItem(mediaSource, item.Method, item.Attribute, objectSpace);
                if (menuItem != null)
                {
                    menuItems.Add(menuItem);
                }
            }
        }

        #endregion

        _logger.Debug("[MediaSourceContextMenuFactory] 菜单项创建完成: Count={Count}", menuItems.Count);

        return menuItems;
    }

    /// <summary>
    /// 创建单个菜单项
    /// </summary>
    private MenuItem? CreateMenuItem(MediaSource mediaSource, MethodInfo method, ContextMenuActionAttribute attribute, IObjectSpace? objectSpace)
    {
        try
        {
            _logger.Debug("[MediaSourceContextMenuFactory] 创建菜单项: DisplayName={DisplayName}, Method={Method}",
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
                    await ExecuteMenuAction(mediaSource, method, attribute, objectSpace);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[MediaSourceContextMenuFactory] 执行菜单操作失败: DisplayName={DisplayName}, Method={Method}",
                        attribute.DisplayName, method.Name);
                }
            };

            return menuItem;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[MediaSourceContextMenuFactory] 创建菜单项失败: Method={Method}", method.Name);
            return null;
        }
    }

    /// <summary>
    /// 执行菜单操作
    /// </summary>
    private async Task ExecuteMenuAction(MediaSource mediaSource, MethodInfo method, ContextMenuActionAttribute attribute, IObjectSpace? objectSpace)
    {
        _logger.Information("[MediaSourceContextMenuFactory] 执行菜单操作: Method={Method}, MediaSource={Name}",
            method.Name, mediaSource.Name);

        var parameters = method.GetParameters();
        object?[]? args = null;

        #region 准备方法参数

        if (parameters.Length == 0)
        {
            args = null;
        }
        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IObjectSpace))
        {
            args = new object[] { objectSpace };
        }
        else
        {
            _logger.Warning("[MediaSourceContextMenuFactory] 方法参数不支持: Method={Method}, Parameters={Parameters}",
                method.Name, string.Join(", ", parameters.Select(p => p.ParameterType.Name)));
            return;
        }

        #endregion

        #region 执行方法

        var result = method.Invoke(mediaSource, args);
        if (result is Task task)
        {
            await task;
        }

        #endregion

        #region 自动提交更改

        if (attribute.IsAutoCommit && objectSpace != null)
        {
            try
            {
                _logger.Debug("[MediaSourceContextMenuFactory] 自动提交更改: Method={Method}", method.Name);
                objectSpace.CommitChanges();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[MediaSourceContextMenuFactory] 自动提交更改失败: Method={Method}", method.Name);
            }
        }

        #endregion
    }
}
