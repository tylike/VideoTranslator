using System;
using System.IO;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using PublishToBilibili.Models;

namespace PublishToBilibili.Services
{
    public class PublishFormService
    {
        private readonly UIA3Automation _automation;
        private readonly FormValidationService _validationService;
        private readonly string _debugDir;

        public PublishFormService()
        {
            _automation = new UIA3Automation();
            _validationService = new FormValidationService();
            _debugDir = @"d:\VideoTranslator\logs";
            if (!Directory.Exists(_debugDir))
            {
                Directory.CreateDirectory(_debugDir);
            }
        }

        #region Public Methods

        public bool FillPublishFormWithInfo(IntPtr windowHandle, PublishInfo publishInfo)
        {
            try
            {
                var window = _automation.FromHandle(windowHandle);
                if (window == null)
                {
                    Console.WriteLine("Window not found.");
                    return false;
                }

                Console.WriteLine("\n=== Filling Publish Form ===");
                SaveUiElementsToFile(window, "01_Before_Filling");

                var editBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                Console.WriteLine($"Found {editBoxes.Length} edit boxes");

                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        Console.WriteLine($"  Edit at X={point.X}, Y={point.Y}");
                        
                        if (point.Y >= 315 && point.Y <= 330)
                        {
                            FillType(edit, publishInfo.Type);
                        }
                        else if (point.Y >= 400 && point.Y <= 415)
                        {
                            FillTags(edit, publishInfo.Tags);
                        }
                        else if (point.Y >= 655 && point.Y <= 670)
                        {
                            FillDescription(edit, publishInfo.Description);
                        }
                    }
                }

                if (publishInfo.IsRepost)
                {
                    SelectRepostType(window);
                    FillSourceAddress(window, publishInfo.SourceAddress);
                }

                ClickMoreOptions(window);
                ControlCheckboxes(window, publishInfo.EnableOriginalWatermark, publishInfo.EnableNoRepost);

                Console.WriteLine("\n=== Filling title last to avoid being overwritten ===");
                var titleEditBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                foreach (var edit in titleEditBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        if (point.Y >= 220 && point.Y <= 235)
                        {
                            FillTitle(edit, publishInfo.Title);
                            break;
                        }
                    }
                }

                Console.WriteLine("\n=== Waiting for UI to update after form filling ===");
                System.Threading.Thread.Sleep(2000);
                
                SaveUiElementsToFile(window, "09_After_Form_Filling");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filling form: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Form Filling Methods

        private void FillTitle(AutomationElement editBox, string title)
        {
            try
            {
                Console.WriteLine("  Filling title...");
                editBox.Click();
                System.Threading.Thread.Sleep(200);

                var valuePattern = editBox.Patterns.Value.Pattern;
                if (valuePattern != null)
                {
                    valuePattern.SetValue(title);
                    System.Threading.Thread.Sleep(300);
                }
                else
                {
                    editBox.AsTextBox().Text = title;
                    System.Threading.Thread.Sleep(300);
                }

                var actualValue = GetEditBoxValue(editBox);
                Console.WriteLine($"  Title filled: '{actualValue}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error filling title: {ex.Message}");
            }
        }

        private void FillType(AutomationElement editBox, string type)
        {
            try
            {
                Console.WriteLine("  Filling type...");
                
                if (type == "自制")
                {
                    Console.WriteLine("  Type is '自制' (default), skipping fill.");
                    return;
                }
                
                editBox.Click();
                System.Threading.Thread.Sleep(200);

                var valuePattern = editBox.Patterns.Value.Pattern;
                if (valuePattern != null)
                {
                    valuePattern.SetValue(type);
                    System.Threading.Thread.Sleep(300);
                }
                else
                {
                    editBox.AsTextBox().Text = type;
                    System.Threading.Thread.Sleep(300);
                }

                var actualValue = GetEditBoxValue(editBox);
                Console.WriteLine($"  Type filled: '{actualValue}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error filling type: {ex.Message}");
            }
        }

        private void FillTags(AutomationElement editBox, List<string> tags)
        {
            try
            {
                Console.WriteLine("  Filling tags...");
                editBox.Click();
                System.Threading.Thread.Sleep(300);

                foreach (var tag in tags)
                {
                    Console.WriteLine($"    Filling tag: {tag}");
                    
                    foreach (var c in tag)
                    {
                        FlaUI.Core.Input.Keyboard.Type(c.ToString());
                        System.Threading.Thread.Sleep(15);
                    }
                    
                    System.Threading.Thread.Sleep(200);
                    FlaUI.Core.Input.Keyboard.Type("\n");
                    System.Threading.Thread.Sleep(500);
                }

                Console.WriteLine("  Tags filled successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error filling tags: {ex.Message}");
            }
        }

        private void FillDescription(AutomationElement editBox, string description)
        {
            try
            {
                Console.WriteLine("  Filling description...");
                editBox.Click();
                System.Threading.Thread.Sleep(200);

                var valuePattern = editBox.Patterns.Value.Pattern;
                if (valuePattern != null)
                {
                    valuePattern.SetValue(description);
                    System.Threading.Thread.Sleep(300);
                }
                else
                {
                    editBox.AsTextBox().Text = description;
                    System.Threading.Thread.Sleep(300);
                }

                var actualValue = GetEditBoxValue(editBox);
                Console.WriteLine($"  Description filled: '{actualValue}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error filling description: {ex.Message}");
            }
        }

        private void SelectRepostType(AutomationElement window)
        {
            try
            {
                Console.WriteLine("  Selecting repost type...");
                
                var radioButtons = window.FindAllDescendants(cf => cf.ByControlType(ControlType.RadioButton));
                Console.WriteLine($"  Found {radioButtons.Length} radio buttons");

                foreach (var radio in radioButtons)
                {
                    Console.WriteLine($"    Radio button: Name='{radio.Name}', AutomationId='{radio.AutomationId}', IsEnabled={radio.IsEnabled}");
                }

                var secondRadio = radioButtons.Skip(1).FirstOrDefault();
                if (secondRadio != null)
                {
                    Console.WriteLine($"  Selecting second radio button (转载)");
                    secondRadio.Focus();
                    System.Threading.Thread.Sleep(100);
                    
                    var invokePattern = secondRadio.Patterns.Invoke.Pattern;
                    if (invokePattern != null)
                    {
                        invokePattern.Invoke();
                        Console.WriteLine("  '转载' radio button selected successfully.");
                    }
                    else if (secondRadio.TryGetClickablePoint(out var point))
                    {
                        secondRadio.Click();
                        Console.WriteLine("  '转载' radio button clicked successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error selecting repost type: {ex.Message}");
            }
        }

        private void FillSourceAddress(AutomationElement window, string sourceAddress)
        {
            try
            {
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine("  Filling source address...");
                
                var editBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                Console.WriteLine($"  Found {editBoxes.Length} edit boxes for source address");

                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        if (point.Y >= 340 && point.Y <= 360)
                        {
                            Console.WriteLine($"  Found source address edit box at X={point.X}, Y={point.Y}");
                            edit.Click();
                            System.Threading.Thread.Sleep(300);

                            var valuePattern = edit.Patterns.Value.Pattern;
                            if (valuePattern != null)
                            {
                                var currentText = valuePattern.Value;
                                if (!string.IsNullOrEmpty(currentText))
                                {
                                    valuePattern.SetValue("");
                                    System.Threading.Thread.Sleep(200);
                                }
                                valuePattern.SetValue(sourceAddress);
                                System.Threading.Thread.Sleep(300);
                            }
                            else
                            {
                                var textBox = edit.AsTextBox();
                                if (!string.IsNullOrEmpty(textBox.Text))
                                {
                                    textBox.Text = "";
                                    System.Threading.Thread.Sleep(200);
                                }
                                textBox.Text = sourceAddress;
                                System.Threading.Thread.Sleep(300);
                            }

                            var actualValue = GetEditBoxValue(edit);
                            Console.WriteLine($"  Source address filled: '{actualValue}'");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error filling source address: {ex.Message}");
            }
        }

        private void ClickMoreOptions(AutomationElement window)
        {
            try
            {
                Console.WriteLine("  Clicking more options...");
                SaveUiElementsToFile(window, "02_Before_Clicking_MoreOptions");
                
                var textElements = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
                Console.WriteLine($"  Found {textElements.Length} text elements");

                foreach (var text in textElements)
                {
                    if (text.Name.Contains("更多选项"))
                    {
                        Console.WriteLine($"  Found '更多选项' text: {text.Name}");
                        
                        var textPoint = text.TryGetClickablePoint(out var point) ? point : System.Drawing.Point.Empty;
                        Console.WriteLine($"  '更多选项' text position: X={textPoint.X}, Y={textPoint.Y}");
                        
                        Console.WriteLine("\n  === Listing all elements near '更多选项' ===");
                        var allElements = window.FindAllDescendants();
                        Console.WriteLine($"  Total elements found: {allElements.Length}");
                        
                        foreach (var element in allElements)
                        {
                            if (element.TryGetClickablePoint(out var elemPoint))
                            {
                                var xDistance = Math.Abs(elemPoint.X - textPoint.X);
                                var yDistance = Math.Abs(elemPoint.Y - textPoint.Y);
                                
                                if (xDistance < 300 && yDistance < 100)
                                {
                                    var name = element.Name ?? "";
                                    var nameDisplay = string.IsNullOrEmpty(name) ? "[Empty]" : name;
                                    Console.WriteLine($"    Element: Type={element.ControlType}, Name='{nameDisplay}', AutomationId='{element.AutomationId}', X={elemPoint.X}, Y={elemPoint.Y}, XDist={xDistance}, YDist={yDistance}");
                                    
                                    if (name.Contains(">") || name.Contains("▼") || name.Contains("v") || name.Contains("∨"))
                                    {
                                        Console.WriteLine($"      *** This might be the arrow button! ***");
                                    }
                                }
                            }
                        }
                        Console.WriteLine("  === End of element listing ===\n");
                        
                        Console.WriteLine("  Looking for arrow button between '更多选项' and '（自制声明、水印和粉丝动态）'...");
                        foreach (var element in allElements)
                        {
                            if (element.TryGetClickablePoint(out var elemPoint))
                            {
                                var xDistance = Math.Abs(elemPoint.X - textPoint.X);
                                var yDistance = Math.Abs(elemPoint.Y - textPoint.Y);
                                
                                if (xDistance > 20 && xDistance < 200 && yDistance < 10)
                                {
                                    var name = element.Name ?? "";
                                    var nameDisplay = string.IsNullOrEmpty(name) ? "[Empty]" : name;
                                    
                                    if (!name.Contains("《哔哩哔哩创作公约》"))
                                    {
                                        Console.WriteLine($"  Found potential arrow button: Type={element.ControlType}, Name='{nameDisplay}', X={elemPoint.X}, Y={elemPoint.Y}");
                                        
                                        if (element.ControlType == ControlType.Button || element.ControlType == ControlType.Text || element.ControlType == ControlType.CheckBox || element.ControlType == ControlType.RadioButton)
                                        {
                                            Console.WriteLine($"  Clicking arrow button: Name='{nameDisplay}'");
                                            
                                            element.Focus();
                                            System.Threading.Thread.Sleep(200);
                                            
                                            try
                                            {
                                                var invokePattern = element.Patterns.Invoke.Pattern;
                                                if (invokePattern != null)
                                                {
                                                    invokePattern.Invoke();
                                                    Console.WriteLine("  Arrow button invoked successfully.");
                                                }
                                                else
                                                {
                                                    element.Click();
                                                    Console.WriteLine("  Arrow button clicked successfully.");
                                                }
                                            }
                                            catch
                                            {
                                                element.Click();
                                                Console.WriteLine("  Arrow button clicked successfully.");
                                            }
                                            
                                            SaveUiElementsToFile(window, "03_After_Clicking_MoreOptions");
                                            
                                            Console.WriteLine("  Waiting for more options panel to expand...");
                                            System.Threading.Thread.Sleep(2000);
                                            
                                            var checkBoxesAfterExpand = window.FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox));
                                            Console.WriteLine($"  Found {checkBoxesAfterExpand.Length} checkboxes after expanding more options");
                                            
                                            foreach (var checkBox in checkBoxesAfterExpand)
                                            {
                                                Console.WriteLine($"    Checkbox: Name='{checkBox.Name}', AutomationId='{checkBox.AutomationId}', IsEnabled={checkBox.IsEnabled}");
                                            }
                                            
                                            SaveUiElementsToFile(window, "04_After_Expanding_MoreOptions");
                                            
                                            Console.WriteLine("  Checking if we need to scroll to see more checkboxes...");
                                            var hasOriginalWatermark = checkBoxesAfterExpand.Any(cb => cb.Name.Contains("原创视频专属水印"));
                                            var hasNoRepost = checkBoxesAfterExpand.Any(cb => cb.Name.Contains("未经作者授权"));
                                            
                                            if (!hasOriginalWatermark || !hasNoRepost)
                                            {
                                                Console.WriteLine("  Not all checkboxes found, scrolling down to show more options...");
                                                
                                                var scrollBarsForCheckboxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.ScrollBar));
                                                foreach (var scrollBar in scrollBarsForCheckboxes)
                                                {
                                                    if (scrollBar.TryGetClickablePoint(out var scrollBarPoint))
                                                    {
                                                        var rect = scrollBar.BoundingRectangle;
                                                        var scrollDownPoint = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height - 20);
                                                        
                                                        for (int i = 0; i < 3; i++)
                                                        {
                                                            FlaUI.Core.Input.Mouse.MoveTo(scrollDownPoint);
                                                            System.Threading.Thread.Sleep(100);
                                                            FlaUI.Core.Input.Mouse.Click(scrollDownPoint);
                                                            System.Threading.Thread.Sleep(500);
                                                        }
                                                    }
                                                }
                                                
                                                Console.WriteLine("  Waiting for UI to update after scrolling...");
                                                System.Threading.Thread.Sleep(2000);
                                                
                                                checkBoxesAfterExpand = window.FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox));
                                                Console.WriteLine($"  Found {checkBoxesAfterExpand.Length} checkboxes after scrolling");
                                                
                                                foreach (var checkBox in checkBoxesAfterExpand)
                                                {
                                                    Console.WriteLine($"    Checkbox: Name='{checkBox.Name}', AutomationId='{checkBox.AutomationId}', IsEnabled={checkBox.IsEnabled}");
                                                }
                                                
                                                SaveUiElementsToFile(window, "05_After_Scrolling_Checkboxes");
                                            }
                                            
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        
                        Console.WriteLine("  Arrow button not found, trying to click '更多选项' text directly...");
                        if (textPoint != System.Drawing.Point.Empty)
                        {
                            try
                            {
                                text.Focus();
                                System.Threading.Thread.Sleep(200);
                                
                                try
                                {
                                    var invokePattern = text.Patterns.Invoke.Pattern;
                                    if (invokePattern != null)
                                    {
                                        invokePattern.Invoke();
                                        Console.WriteLine("  '更多选项' text invoked successfully.");
                                    }
                                    else
                                    {
                                        text.Click();
                                        Console.WriteLine("  '更多选项' text clicked successfully.");
                                    }
                                }
                                catch
                                {
                                    text.Click();
                                    Console.WriteLine("  '更多选项' text clicked successfully.");
                                }
                                
                                Console.WriteLine("  Waiting for more options panel to expand...");
                                System.Threading.Thread.Sleep(2000);
                                
                                var checkBoxesAfterExpand = window.FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox));
                                Console.WriteLine($"  Found {checkBoxesAfterExpand.Length} checkboxes after expanding more options");
                                
                                foreach (var checkBox in checkBoxesAfterExpand)
                                {
                                    Console.WriteLine($"    Checkbox: Name='{checkBox.Name}', AutomationId='{checkBox.AutomationId}', IsEnabled={checkBox.IsEnabled}");
                                }
                                
                                return;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  Failed to click '更多选项' text: {ex.Message}");
                            }
                        }
                        
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error clicking more options: {ex.Message}");
            }
        }

        private void ControlCheckboxes(AutomationElement window, bool enableOriginalWatermark, bool enableNoRepost)
        {
            try
            {
                System.Threading.Thread.Sleep(500);
                Console.WriteLine("  Controlling checkboxes...");
                
                SaveUiElementsToFile(window, "08_Before_Controlling_Checkboxes");
                
                var checkBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox));
                Console.WriteLine($"  Found {checkBoxes.Length} checkboxes");

                foreach (var checkBox in checkBoxes)
                {
                    Console.WriteLine($"    Checkbox: Name='{checkBox.Name}', AutomationId='{checkBox.AutomationId}', IsEnabled={checkBox.IsEnabled}");
                }

                foreach (var checkBox in checkBoxes)
                {
                    if (checkBox.Name.Contains("原创视频专属水印"))
                    {
                        var togglePattern = checkBox.Patterns.Toggle.Pattern;
                        if (togglePattern != null)
                        {
                            var currentState = togglePattern.ToggleState.Value;
                            var isChecked = currentState.HasFlag(FlaUI.Core.Definitions.ToggleState.On);
                            
                            if (enableOriginalWatermark && !isChecked)
                            {
                                Console.WriteLine($"  Checking checkbox: {checkBox.Name}");
                                checkBox.Focus();
                                System.Threading.Thread.Sleep(100);
                                togglePattern.Toggle();
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' checked successfully.");
                            }
                            else if (!enableOriginalWatermark && isChecked)
                            {
                                Console.WriteLine($"  Unchecking checkbox: {checkBox.Name}");
                                checkBox.Focus();
                                System.Threading.Thread.Sleep(100);
                                togglePattern.Toggle();
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' unchecked successfully.");
                            }
                        }
                        else if (checkBox.TryGetClickablePoint(out var point))
                        {
                            if (enableOriginalWatermark)
                            {
                                Console.WriteLine($"  Checking checkbox: {checkBox.Name}");
                                checkBox.Focus();
                                System.Threading.Thread.Sleep(100);
                                checkBox.Click();
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' clicked successfully.");
                            }
                        }
                    }
                    else if (checkBox.Name.Contains("未经作者授权"))
                    {
                        var togglePattern = checkBox.Patterns.Toggle.Pattern;
                        if (togglePattern != null)
                        {
                            var currentState = togglePattern.ToggleState.Value;
                            var isChecked = currentState.HasFlag(FlaUI.Core.Definitions.ToggleState.On);
                            
                            if (enableNoRepost && !isChecked)
                            {
                                Console.WriteLine($"  Checking checkbox: {checkBox.Name}");
                                checkBox.Focus();
                                System.Threading.Thread.Sleep(100);
                                togglePattern.Toggle();
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' checked successfully.");
                            }
                            else if (!enableNoRepost && isChecked)
                            {
                                Console.WriteLine($"  Unchecking checkbox: {checkBox.Name}");
                                checkBox.Focus();
                                System.Threading.Thread.Sleep(100);
                                togglePattern.Toggle();
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' unchecked successfully.");
                            }
                        }
                        else if (checkBox.TryGetClickablePoint(out var point))
                        {
                            if (enableNoRepost)
                            {
                                Console.WriteLine($"  Checking checkbox: {checkBox.Name}");
                                checkBox.Focus();
                                System.Threading.Thread.Sleep(100);
                                checkBox.Click();
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' clicked successfully.");
                            }
                        }
                    }
                    else if (checkBox.Name.Contains("我已阅读并接受"))
                    {
                        var togglePattern = checkBox.Patterns.Toggle.Pattern;
                        if (togglePattern != null)
                        {
                            var currentState = togglePattern.ToggleState.Value;
                            var isChecked = currentState.HasFlag(FlaUI.Core.Definitions.ToggleState.On);
                            
                            if (!isChecked)
                            {
                                Console.WriteLine($"  Checking checkbox: {checkBox.Name}");
                                checkBox.Focus();
                                System.Threading.Thread.Sleep(100);
                                togglePattern.Toggle();
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' checked successfully.");
                            }
                            else
                            {
                                Console.WriteLine($"  Checkbox '{checkBox.Name}' is already checked.");
                            }
                        }
                        else if (checkBox.TryGetClickablePoint(out var point))
                        {
                            checkBox.Focus();
                            System.Threading.Thread.Sleep(100);
                            checkBox.Click();
                            Console.WriteLine($"  Checkbox '{checkBox.Name}' clicked successfully.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error controlling checkboxes: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private string GetEditBoxValue(AutomationElement editBox)
        {
            try
            {
                var valuePattern = editBox.Patterns.Value.Pattern;
                if (valuePattern != null)
                {
                    return valuePattern.Value ?? "";
                }
                else
                {
                    var textBox = editBox.AsTextBox();
                    return textBox.Text ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region Public Methods

        public ValidationResult ValidateForm(IntPtr windowHandle, PublishInfo publishInfo)
        {
            return _validationService.ValidatePublishForm(windowHandle, publishInfo);
        }

        #endregion

        #region Helper Methods

        private void SaveUiElementsToFile(AutomationElement window, string stageName)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = Path.Combine(_debugDir, $"UI_Elements_{stageName}_{timestamp}.txt");
                
                using (var writer = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine($"=== UI Elements at Stage: {stageName} ===");
                    writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    writer.WriteLine();
                    
                    var allElements = window.FindAllDescendants();
                    writer.WriteLine($"Total elements: {allElements.Length}");
                    writer.WriteLine();
                    
                    var elementsByType = allElements.GroupBy(e => e.ControlType)
                        .OrderBy(g => g.Key.ToString());
                    
                    foreach (var group in elementsByType)
                    {
                        writer.WriteLine($"--- {group.Key} ({group.Count()}) ---");
                        
                        foreach (var element in group.OrderBy(e => e.TryGetClickablePoint(out var p) ? p.Y : 0))
                        {
                            var point = element.TryGetClickablePoint(out var p) ? p : System.Drawing.Point.Empty;
                            var name = element.Name ?? "";
                            var nameDisplay = string.IsNullOrEmpty(name) ? "[Empty]" : name;
                            var automationId = element.AutomationId ?? "";
                            
                            writer.WriteLine($"  Type: {element.ControlType}");
                            writer.WriteLine($"  Name: '{nameDisplay}'");
                            writer.WriteLine($"  AutomationId: '{automationId}'");
                            writer.WriteLine($"  Position: X={point.X}, Y={point.Y}");
                            writer.WriteLine($"  IsEnabled: {element.IsEnabled}");
                            writer.WriteLine($"  IsOffscreen: {element.IsOffscreen}");
                            
                            if (element.ControlType == ControlType.Edit)
                            {
                                try
                                {
                                    var value = GetEditBoxValue(element);
                                    writer.WriteLine($"  Value: '{value}'");
                                }
                                catch
                                {
                                    writer.WriteLine($"  Value: [Failed to read]");
                                }
                            }
                            
                            if (element.ControlType == ControlType.CheckBox)
                            {
                                try
                                {
                                    var toggleState = element.Patterns.Toggle.Pattern?.ToggleState.Value ?? ToggleState.Off;
                                    writer.WriteLine($"  ToggleState: {toggleState}");
                                }
                                catch
                                {
                                    writer.WriteLine($"  ToggleState: [Failed to read]");
                                }
                            }
                            
                            writer.WriteLine();
                        }
                        
                        writer.WriteLine();
                    }
                }
                
                Console.WriteLine($"  UI elements saved to: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error saving UI elements: {ex.Message}");
            }
        }

        #endregion
    }
}
