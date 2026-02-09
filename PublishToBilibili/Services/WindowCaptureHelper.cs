using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace PublishToBilibili.Services
{
    public static class WindowCaptureHelper
    {
        private static readonly string _debugDir = @"d:\VideoTranslator\logs";

        static WindowCaptureHelper()
        {
            if (!Directory.Exists(_debugDir))
            {
                Directory.CreateDirectory(_debugDir);
            }
        }

        public static void SaveUiElementsToFile(AutomationElement window, string stageName, bool write = false)
        {
            if (!write)
            {
                Console.WriteLine("暂不保存文件!");
                return;
            }
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = Path.Combine(_debugDir, $"UI_Elements_{stageName}_{timestamp}.txt");
                var content = CaptureWindowContent(window);
                File.WriteAllText(filename, content, System.Text.Encoding.UTF8);
                Console.WriteLine($"  UI elements saved to: {filename}", MessageType.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to save UI elements: {ex.Message}", MessageType.Error);
            }
        }

        private static string CaptureWindowContent(AutomationElement element)
        {
            var content = new System.Text.StringBuilder();

            try
            {
                #region Capture Window Header
                content.AppendLine("=== Window Information ===");
                AppendProperty(content, "Name", element.Name);
                AppendProperty(content, "AutomationId", element.AutomationId);

                try
                {
                    AppendProperty(content, "ClassName", element.Properties.ClassName.Value);
                }
                catch
                {
                }

                AppendProperty(content, "ControlType", element.ControlType.ToString());

                try
                {
                    AppendProperty(content, "LocalizedControlType", element.Properties.LocalizedControlType.Value);
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "FrameworkId", element.Properties.FrameworkId.Value);
                }
                catch
                {
                }

                AppendProperty(content, "IsEnabled", element.IsEnabled.ToString());
                AppendProperty(content, "IsOffscreen", element.IsOffscreen.ToString());

                try
                {
                    AppendProperty(content, "IsKeyboardFocusable", element.Properties.IsKeyboardFocusable.Value.ToString());
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "HasKeyboardFocus", element.Properties.HasKeyboardFocus.Value.ToString());
                }
                catch
                {
                }

                if (element.Properties.BoundingRectangle.TryGetValue(out var rect))
                {
                    AppendProperty(content, "Position", $"({rect.X}, {rect.Y})");
                    AppendProperty(content, "Size", $"{rect.Width}x{rect.Height}");
                }

                try
                {
                    AppendProperty(content, "ProcessId", GetWindowProcessId(element.Properties.NativeWindowHandle.Value).ToString());
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "NativeWindowHandle", element.Properties.NativeWindowHandle.Value.ToString("X8"));
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "HelpText", element.Properties.HelpText.Value);
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "FullDescription", element.Properties.FullDescription.Value);
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "AcceleratorKey", element.Properties.AcceleratorKey.Value);
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "AccessKey", element.Properties.AccessKey.Value);
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "IsRequiredForForm", element.Properties.IsRequiredForForm.Value.ToString());
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "IsContentElement", element.Properties.IsContentElement.Value.ToString());
                }
                catch
                {
                }

                try
                {
                    AppendProperty(content, "IsControlElement", element.Properties.IsControlElement.Value.ToString());
                }
                catch
                {
                }

                content.AppendLine();
                #endregion

                #region Capture All Elements
                content.AppendLine("=== Window Content ===");
                CaptureElementContent(element, content, 0);
                #endregion
            }
            catch (Exception ex)
            {
                content.AppendLine($"Error capturing content: {ex.Message}");
            }

            return content.ToString();
        }

        private static void AppendProperty(System.Text.StringBuilder content, string propertyName, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                content.AppendLine($"{propertyName}: {value}");
            }
        }

        private static void AppendPropertyIfNotEmpty(List<string> properties, string propertyName, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                properties.Add($"{propertyName}='{value}'");
            }
        }

        private static void AppendPropertyIfNotEmpty(System.Text.StringBuilder content, string propertyName, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                content.AppendLine($"{propertyName}: {value}");
            }
        }

        private static void CaptureElementContent(AutomationElement element, System.Text.StringBuilder content, int depth)
        {
            try
            {
                var indent = new string(' ', depth * 2);

                #region Capture Element Basic Information
                content.Append($"{indent}[{element.ControlType}]");

                var properties = new List<string>();
                AppendPropertyIfNotEmpty(properties, "Name", element.Name);
                AppendPropertyIfNotEmpty(properties, "AutomationId", element.AutomationId);

                try
                {
                    AppendPropertyIfNotEmpty(properties, "ClassName", element.Properties.ClassName.Value);
                }
                catch
                {
                }

                try
                {
                    AppendPropertyIfNotEmpty(properties, "LocalizedControlType", element.Properties.LocalizedControlType.Value);
                }
                catch
                {
                }

                try
                {
                    AppendPropertyIfNotEmpty(properties, "FrameworkId", element.Properties.FrameworkId.Value);
                }
                catch
                {
                }

                if (element.Properties.BoundingRectangle.TryGetValue(out var rect))
                {
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        properties.Add($"Pos=({rect.X},{rect.Y})");
                        properties.Add($"Size={rect.Width}x{rect.Height}");
                    }
                }

                if (!element.IsEnabled) properties.Add("Disabled");
                if (element.IsOffscreen) properties.Add("Offscreen");

                try
                {
                    if (element.Properties.HasKeyboardFocus.Value) properties.Add("Focused");
                }
                catch
                {
                }

                try
                {
                    if (element.Properties.IsRequiredForForm.Value) properties.Add("Required");
                }
                catch
                {
                }

                if (properties.Count > 0)
                {
                    content.Append(" " + string.Join(", ", properties));
                }
                content.AppendLine();
                #endregion

                #region Capture Element Value (for Edit, ComboBox, Document, Text)
                if (element.ControlType == ControlType.Edit ||
                    element.ControlType == ControlType.ComboBox ||
                    element.ControlType == ControlType.Document ||
                    element.ControlType == ControlType.Text)
                {
                    try
                    {
                        var textPattern = element.Patterns.Text.Pattern;
                        if (textPattern != null)
                        {
                            var text = textPattern.DocumentRange.GetText(int.MaxValue);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                content.AppendLine($"{indent}  Value: {text.Trim()}");
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                #endregion

                #region Capture Button Specific Properties
                if (element.ControlType == ControlType.Button)
                {
                    try
                    {
                        var invokePattern = element.Patterns.Invoke.Pattern;
                        if (invokePattern != null)
                        {
                            content.AppendLine($"{indent}  Supports: InvokePattern");
                        }
                    }
                    catch
                    {
                    }
                }
                #endregion

                #region Capture Toggle Specific Properties
                if (element.ControlType == ControlType.CheckBox ||
                    element.ControlType == ControlType.RadioButton)
                {
                    try
                    {
                        var togglePattern = element.Patterns.Toggle.Pattern;
                        if (togglePattern != null)
                        {
                            var toggleState = togglePattern.ToggleState.Value;
                            content.AppendLine($"{indent}  ToggleState: {toggleState}");
                        }
                    }
                    catch
                    {
                    }
                }
                #endregion

                #region Capture Selection Specific Properties
                if (element.ControlType == ControlType.List ||
                    element.ControlType == ControlType.ComboBox)
                {
                    try
                    {
                        var selectionPattern = element.Patterns.Selection.Pattern;
                        if (selectionPattern != null)
                        {
                            var selection = selectionPattern.Selection.Value;
                            if (selection != null && selection.Length > 0)
                            {
                                var selectedItems = string.Join(", ", selection.Select(s => s.Name));
                                content.AppendLine($"{indent}  Selected: {selectedItems}");
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                #endregion

                #region Capture RangeValue Specific Properties
                if (element.ControlType == ControlType.ProgressBar ||
                    element.ControlType == ControlType.Slider ||
                    element.ControlType == ControlType.Spinner)
                {
                    try
                    {
                        var rangeValuePattern = element.Patterns.RangeValue.Pattern;
                        if (rangeValuePattern != null)
                        {
                            content.AppendLine($"{indent}  Value: {rangeValuePattern.Value.Value} (Range: {rangeValuePattern.Minimum.Value} - {rangeValuePattern.Maximum.Value})");
                        }
                    }
                    catch
                    {
                    }
                }
                #endregion

                #region Capture HelpText and Description
                try
                {
                    AppendPropertyIfNotEmpty(content, $"{indent}  HelpText", element.Properties.HelpText.Value);
                }
                catch
                {
                }

                try
                {
                    AppendPropertyIfNotEmpty(content, $"{indent}  Description", element.Properties.FullDescription.Value);
                }
                catch
                {
                }

                try
                {
                    AppendPropertyIfNotEmpty(content, $"{indent}  AcceleratorKey", element.Properties.AcceleratorKey.Value);
                }
                catch
                {
                }

                try
                {
                    AppendPropertyIfNotEmpty(content, $"{indent}  AccessKey", element.Properties.AccessKey.Value);
                }
                catch
                {
                }
                #endregion

                #region Recursively Capture Children
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    CaptureElementContent(child, content, depth + 1);
                }
                #endregion
            }
            catch (Exception ex)
            {
                var indent = new string(' ', depth * 2);
                content.AppendLine($"{indent}Error capturing element: {ex.Message}");
            }
        }

        private static int GetWindowProcessId(IntPtr handle)
        {
            try
            {
                return NativeMethods.GetWindowThreadProcessId(handle, out int processId) ? processId : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}

