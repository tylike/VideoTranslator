using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace PublishToBilibili.Services
{
    public class WindowExplorerService
    {
        private readonly UIA3Automation _automation;

        public WindowExplorerService()
        {
            _automation = new UIA3Automation();
        }

        public void ExploreWindow(IntPtr windowHandle)
        {
            try
            {
                var window = _automation.FromHandle(windowHandle);
                if (window == null)
                {
                    Console.WriteLine("Window not found.");
                    return;
                }

                Console.WriteLine($"\n=== Exploring Window: {window.Name} ===");
                Console.WriteLine($"AutomationId: {window.AutomationId}");
                Console.WriteLine($"ControlType: {window.ControlType}");
                Console.WriteLine($"IsEnabled: {window.IsEnabled}");
                Console.WriteLine($"IsOffscreen: {window.IsOffscreen}");

                ExploreElement(window, 0, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exploring window: {ex.Message}");
            }
        }

        private void ExploreElement(AutomationElement element, int depth, AutomationElement? parent = null)
        {
            try
            {
                var indent = new string(' ', depth * 2);

                if (element.ControlType == ControlType.Edit || 
                    element.ControlType == ControlType.ComboBox ||
                    element.ControlType == ControlType.Document ||
                    element.ControlType == ControlType.List ||
                    element.ControlType == ControlType.Tree)
                {
                    var parentName = parent != null ? parent.Name : "Root";
                    Console.WriteLine($"{indent}[INPUT] {element.ControlType}: Name='{element.Name}', AutomationId='{element.AutomationId}', IsEnabled={element.IsEnabled}, Parent='{parentName}'");
                    
                    if (element.TryGetClickablePoint(out var point))
                    {
                        Console.WriteLine($"{indent}  Position: X={point.X}, Y={point.Y}");
                    }
                }
                else if (element.ControlType == ControlType.Text || 
                         element.ControlType == ControlType.Group ||
                         element.ControlType == ControlType.Header)
                {
                    Console.WriteLine($"{indent}[LABEL] {element.ControlType}: Name='{element.Name}', AutomationId='{element.AutomationId}'");
                }

                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    ExploreElement(child, depth + 1, element);
                }
            }
            catch
            {
            }
        }

        public void FindPublishWindow()
        {
            try
            {
                Console.WriteLine("\n=== Searching for '发布' window ===");
                
                var windows = _automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window));
                Console.WriteLine($"Found {windows.Length} windows on desktop:");

                foreach (var window in windows)
                {
                    Console.WriteLine($"  - {window.Name}");
                    if (window.Name.Contains("发布"))
                    {
                        Console.WriteLine($"\n=== Found '发布' Window ===");
                        ExploreWindow(window.Properties.NativeWindowHandle.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding publish window: {ex.Message}");
            }
        }
    }
}
