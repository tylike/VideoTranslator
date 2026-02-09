using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace PublishToBilibili.Interfaces
{
    public interface IWindowService
    {
        AutomationElement? FindButton(IntPtr windowHandle, string buttonText);
        AutomationElement? FindElementByName(IntPtr windowHandle, string name);
        AutomationElement? FindElementByAutomationId(IntPtr windowHandle, string automationId);
        AutomationElement? GetMainWindow(IntPtr windowHandle);
        bool ClickButton(AutomationElement button);
        bool ActivateWindow(IntPtr windowHandle);
        AutomationElement? FindWindowByTitle(string windowTitle);
        bool SelectFileInDialog(string filePath, IntPtr parentWindowHandle);
    }

    public class WindowService : IWindowService
    {
        private readonly UIA3Automation _automation;

        public WindowService()
        {
            _automation = new UIA3Automation();
        }

        public AutomationElement? GetMainWindow(IntPtr windowHandle)
        {
            return _automation.FromHandle(windowHandle);
        }

        public AutomationElement? FindButton(IntPtr windowHandle, string buttonText)
        {
            var window = GetMainWindow(windowHandle);
            if (window == null) return null;

            var button = Retry.WhileNull(() =>
            {
                var buttons = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
                return buttons.FirstOrDefault(b => b.Name.Contains(buttonText));
            }, TimeSpan.FromSeconds(5)).Result;

            return button;
        }

        public AutomationElement? FindElementByName(IntPtr windowHandle, string name)
        {
            var window = GetMainWindow(windowHandle);
            if (window == null) return null;

            return window.FindFirstDescendant(cf => cf.ByName(name));
        }

        public AutomationElement? FindElementByAutomationId(IntPtr windowHandle, string automationId)
        {
            var window = GetMainWindow(windowHandle);
            if (window == null) return null;

            return window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        }

        public bool ClickButton(AutomationElement button)
        {
            if (button == null) return false;

            try
            {
                if (!button.IsEnabled || button.IsOffscreen)
                {
                    Console.WriteLine($"    Button is not clickable. Enabled: {button.IsEnabled}, Offscreen: {button.IsOffscreen}");
                    return false;
                }

                button.Focus();
                System.Threading.Thread.Sleep(200);
                
                var invokePattern = button.Patterns.Invoke.Pattern;
                if (invokePattern != null)
                {
                    Console.WriteLine($"    Using InvokePattern to click button");
                    invokePattern.Invoke();
                    return true;
                }

                if (button.TryGetClickablePoint(out var point))
                {
                    Console.WriteLine($"    Using Click() to click button at {point}");
                    button.Click();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Error clicking button: {ex.Message}");
                return false;
            }
        }

        public bool ActivateWindow(IntPtr windowHandle)
        {
            try
            {
                var window = GetMainWindow(windowHandle);
                if (window == null) return false;

                window.SetForeground();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public AutomationElement? FindWindowByTitle(string windowTitle)
        {
            try
            {
                Console.WriteLine($"    Searching for window containing: {windowTitle}", MessageType.Info);
                
                var targetWindow = Retry.WhileNull(() =>
                {
                    try
                    {
                        var windows = _automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window));
                        
                        Console.WriteLine($"    Found {windows.Length} windows:", MessageType.Info);
                        foreach (var win in windows.Take(10))
                        {
                            Console.WriteLine($"      - {win.Name}", MessageType.Info);
                        }
                        
                        return windows.FirstOrDefault(w => w.Name.Contains(windowTitle));
                    }
                    catch
                    {
                        return null;
                    }
                }, TimeSpan.FromSeconds(8)).Result;

                if (targetWindow != null)
                {
                    Console.WriteLine($"    Found target window: {targetWindow.Name}", MessageType.Success);
                }
                else
                {
                    Console.WriteLine($"    Window not found", MessageType.Warning);
                }

                return targetWindow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Error finding window: {ex.Message}", MessageType.Error);
                return null;
            }
        }

        public bool SelectFileInDialog(string filePath, IntPtr parentWindowHandle)
        {
            try
            {
                Console.WriteLine("  Finding dialog window...", MessageType.Info);
                
                var parentWindow = GetMainWindow(parentWindowHandle);
                if (parentWindow == null)
                {
                    Console.WriteLine("  Parent window not found.", MessageType.Error);
                    return false;
                }

                var dialogWindow = Retry.WhileNull(() =>
                {
                    try
                    {
                        var windows = parentWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Window));
                        return windows.FirstOrDefault(w => w.Name.Contains("打开") || w.Name.Contains("Open"));
                    }
                    catch
                    {
                        return null;
                    }
                }, TimeSpan.FromSeconds(5)).Result;

                if (dialogWindow == null)
                {
                    Console.WriteLine("  Dialog window not found in parent window.", MessageType.Warning);
                    return false;
                }

                Console.WriteLine($"  Found dialog: {dialogWindow.Name}", MessageType.Success);
                dialogWindow.SetForeground();
                System.Threading.Thread.Sleep(500);

                Console.WriteLine("  Finding edit box...", MessageType.Info);
                var fileNameEdit = Retry.WhileNull(() =>
                {
                    var editBoxes = dialogWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                    Console.WriteLine($"    Found {editBoxes.Length} edit boxes:", MessageType.Info);
                    foreach (var edit in editBoxes)
                    {
                        Console.WriteLine($"      - Name: {edit.Name}, AutomationId: {edit.AutomationId}, IsEnabled: {edit.IsEnabled}", MessageType.Info);
                    }
                    
                    return editBoxes.FirstOrDefault(e => e.AutomationId.Contains("FileName") || 
                                                  e.AutomationId.Contains("FilePath") ||
                                                  e.Name.Contains("文件名") ||
                                                  e.Name.Contains("File name"));
                }, TimeSpan.FromSeconds(3)).Result;

                if (fileNameEdit == null)
                {
                    Console.WriteLine("  Edit box not found.", MessageType.Warning);
                    return false;
                }

                Console.WriteLine($"  Found edit box: {fileNameEdit.Name}", MessageType.Success);
                fileNameEdit.Click(true);
                fileNameEdit.Focus();

                System.Threading.Thread.Sleep(100);

                Console.WriteLine($"  Setting file path: {filePath}", MessageType.Info);
                var valuePattern = fileNameEdit.Patterns.Value.Pattern;
                if (valuePattern != null)
                {
                    valuePattern.SetValue(filePath);
                }
                else
                {
                    fileNameEdit.AsTextBox().Text = filePath;
                }

                System.Threading.Thread.Sleep(500);

                Console.WriteLine("  Finding open button...", MessageType.Info);
                var openButton = Retry.WhileNull(() =>
                {
                    var buttons = dialogWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
                    Console.WriteLine($"    Found {buttons.Length} buttons:", MessageType.Info);
                    foreach (var btn in buttons.Take(10))
                    {
                        Console.WriteLine($"      - Name: {btn.Name}, AutomationId: {btn.AutomationId}, IsEnabled: {btn.IsEnabled}", MessageType.Info);
                    }
                    return buttons.FirstOrDefault(b => (b.Name.Contains("打开") || b.Name.Contains("Open")) && 
                                                   !b.AutomationId.Contains("DropDown"));
                }, TimeSpan.FromSeconds(3)).Result;

                if (openButton == null)
                {
                    Console.WriteLine("  Open button not found.", MessageType.Warning);
                    return false;
                }

                Console.WriteLine($"  Found open button: {openButton.Name}, AutomationId: {openButton.AutomationId}, IsEnabled: {openButton.IsEnabled}", MessageType.Success);
                openButton.Focus();
                System.Threading.Thread.Sleep(300);
                
                return ClickButton(openButton);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}", MessageType.Error);
                return false;
            }
        }
    }
}
