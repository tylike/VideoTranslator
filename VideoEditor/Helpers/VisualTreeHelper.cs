using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Serilog;

namespace VideoEditor.Helpers;

public class VisualTreeHelperEx
{
    #region 公共方法

    public static void DumpVisualTreeToFile(DependencyObject root, string filePath)
    {
        try
        {
            var logger = Serilog.Log.ForContext(typeof(VisualTreeHelperEx));
            logger.Information("开始导出可视化树到文件: {FilePath}", filePath);

            var sb = new StringBuilder();
            DumpVisualTree(root, sb, 0);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            logger.Information("可视化树导出完成，共 {LineCount} 行", sb.ToString().Split('\n').Length);
        }
        catch (Exception ex)
        {
            var logger = Serilog.Log.ForContext(typeof(VisualTreeHelperEx));
            logger.Error(ex, "导出可视化树失败: {FilePath}", filePath);
        }
    }

    public static void DumpVisualTree(DependencyObject root, StringBuilder sb, int indent)
    {
        if (root == null)
        {
            return;
        }

        var indentStr = new string(' ', indent * 2);
        var typeName = root.GetType().Name;
        var name = string.Empty;

        if (root is FrameworkElement fe)
        {
            name = fe.Name;
        }

        var info = $"{indentStr}{typeName}";

        if (!string.IsNullOrEmpty(name))
        {
            info += $" [Name=\"{name}\"]";
        }

        if (root is FrameworkElement frameworkElement)
        {
            var width = frameworkElement.ActualWidth;
            var height = frameworkElement.ActualHeight;
            var visibility = frameworkElement.Visibility;
            var isEnabled = frameworkElement.IsEnabled;

            info += $" [Width={width:F2}, Height={height:F2}, Visibility={visibility}, Enabled={isEnabled}]";

            if (root is System.Windows.Controls.Control control)
            {
                info += $" [Background={control.Background}]";
            }
        }

        sb.AppendLine(info);

        var children = GetChildren(root);
        foreach (var child in children)
        {
            DumpVisualTree(child, sb, indent + 1);
        }
    }

    public static List<DependencyObject> GetChildren(DependencyObject parent)
    {
        var children = new List<DependencyObject>();

        if (parent is Visual visual)
        {
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(visual);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    children.Add(child);
                }
            }
        }

        return children;
    }

    #endregion
}
