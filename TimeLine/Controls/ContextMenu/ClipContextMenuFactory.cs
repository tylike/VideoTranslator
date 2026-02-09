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
/// 片段菜单工厂，使用反射创建基于特性的菜单项
/// </summary>
public class ClipContextMenuFactory
{
    private readonly ILogger _logger = LoggerService.ForContext<ClipContextMenuFactory>();

    /// <summary>
    /// 为指定的片段对象创建菜单项
    /// </summary>
    /// <param name="clip">片段对象</param>
    /// <param name="viewModel">视图模型（可选）</param>
    /// <param name="playRequested">播放请求回调（可选）</param>
    /// <returns>菜单项列表</returns>
    public List<Control> CreateMenuItems(Clip clip, VideoEditorContext? viewModel, Action<Clip>? playRequested = null)
    {
        if (clip == null)
        {
            _logger.Warning("[ClipContextMenuFactory] Clip 对象为空，无法创建菜单项");
            return new List<Control>();
        }

        _logger.Debug("[ClipContextMenuFactory] 开始为片段创建菜单项: ClipType={ClipType}, Index={Index}",
            clip.GetType().Name, clip.Index);

        var menuItems = new List<Control>();

        #region 获取标记了 ContextMenuAction 的方法

        var clipType = clip.GetType();
        var methods = clipType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

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
                var menuItem = CreateMenuItem(clip, item.Method, item.Attribute, viewModel, playRequested);
                if (menuItem != null)
                {
                    menuItems.Add(menuItem);
                }
            }
        }

        #endregion

        _logger.Debug("[ClipContextMenuFactory] 菜单项创建完成: Count={Count}", menuItems.Count);

        return menuItems;
    }

    /// <summary>
    /// 创建单个菜单项
    /// </summary>
    private MenuItem? CreateMenuItem(Clip clip, MethodInfo method, ContextMenuActionAttribute attribute, VideoEditorContext? viewModel, Action<Clip>? playRequested = null)
    {
        try
        {
            _logger.Information("[ClipContextMenuFactory] 创建菜单项: DisplayName={DisplayName}, Method={Method}, Group={Group}, Order={Order}, PlayRequested={PlayRequested}",
                attribute.DisplayName, method.Name, attribute.Group, attribute.Order, playRequested != null);

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

            var parameters = method.GetParameters();
            var paramInfo = parameters.Length > 0 
                ? string.Join(", ", parameters.Select(p => p.ParameterType.Name)) 
                : "无参数";

            _logger.Debug("[ClipContextMenuFactory] 菜单项方法信息: Method={Method}, Parameters={ParamInfo}, ReturnType={ReturnType}",
                method.Name, paramInfo, method.ReturnType.Name);

            menuItem.Click += async (sender, e) =>
            {
                _logger.Information("[ClipContextMenuFactory] 菜单项被点击: DisplayName={DisplayName}, Method={Method}, ClipType={ClipType}, ClipIndex={ClipIndex}",
                    attribute.DisplayName, method.Name, clip.GetType().Name, clip.Index);

                try
                {
                    if (method.Name == "PlayAudio" && playRequested != null)
                    {
                        _logger.Information("[ClipContextMenuFactory] 执行播放操作: ClipType={ClipType}, ClipIndex={ClipIndex}",
                            clip.GetType().Name, clip.Index);
                        playRequested(clip);
                    }
                    else
                    {
                        _logger.Information("[ClipContextMenuFactory] 执行方法调用: Method={Method}, Parameters={ParamInfo}",
                            method.Name, paramInfo);
                        await ExecuteMenuAction(clip, method, attribute, viewModel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[ClipContextMenuFactory] 执行菜单操作失败: DisplayName={DisplayName}, Method={Method}, ClipType={ClipType}, ClipIndex={ClipIndex}",
                        attribute.DisplayName, method.Name, clip.GetType().Name, clip.Index);
                }
            };

            return menuItem;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[ClipContextMenuFactory] 创建菜单项失败: Method={Method}", method.Name);
            return null;
        }
    }

    /// <summary>
    /// 执行菜单操作
    /// </summary>
    private async Task ExecuteMenuAction(Clip clip, MethodInfo method, ContextMenuActionAttribute attribute, VideoEditorContext? viewModel)
    {
        _logger.Information("[ClipContextMenuFactory] 执行菜单操作: Method={Method}, ClipIndex={ClipIndex}",
            method.Name, clip.Index);

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
            _logger.Warning("[ClipContextMenuFactory] 方法参数不支持: Method={Method}, Parameters={Parameters}",
                method.Name, string.Join(", ", parameters.Select(p => p.ParameterType.Name)));
            return;
        }

        #endregion

        #region 执行方法

        var result = method.Invoke(clip, args);
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
                _logger.Debug("[ClipContextMenuFactory] 自动提交更改: Method={Method}", method.Name);
                viewModel.ObjectSpace.CommitChanges();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[ClipContextMenuFactory] 自动提交更改失败: Method={Method}", method.Name);
            }
        }

        #endregion
    }
}
