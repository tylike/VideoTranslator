using System;

namespace TrackMenuAttributes;

/// <summary>
/// 标记方法为上下文菜单操作
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ContextMenuActionAttribute : Attribute
{
    /// <summary>
    /// 菜单项显示文本
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 菜单项图标（可选）
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 菜单项分组名称（可选）
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 菜单项排序顺序（可选，越小越靠前）
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否启用（可选，默认为 true）
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 执行后是否自动提交更改（可选，默认为 false）
    /// </summary>
    public bool IsAutoCommit { get; set; } = false;

    /// <summary>
    /// 工具提示文本（可选）
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="displayName">菜单项显示文本</param>
    public ContextMenuActionAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}
