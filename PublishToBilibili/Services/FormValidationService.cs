using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using PublishToBilibili.Models;

namespace PublishToBilibili.Services
{
    public class FormValidationService
    {
        private readonly UIA3Automation _automation;

        public FormValidationService()
        {
            _automation = new UIA3Automation();
        }

        public ValidationResult ValidatePublishForm(IntPtr windowHandle, PublishInfo expectedInfo)
        {
            var result = new ValidationResult();

            try
            {
                var window = _automation.FromHandle(windowHandle);
                if (window == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Window not found.";
                    return result;
                }

                Console.WriteLine("\n=== Validating Publish Form ===");

                #region Validate Title
                var titleValidation = ValidateTitle(window, expectedInfo.Title);
                result.TitleValid = titleValidation.IsValid;
                if (!titleValidation.IsValid)
                {
                    result.Errors.Add($"Title validation failed: {titleValidation.ErrorMessage}");
                }
                #endregion

                #region Validate Type
                var typeValidation = ValidateType(window, expectedInfo.Type, expectedInfo.IsRepost);
                result.TypeValid = typeValidation.IsValid;
                if (!typeValidation.IsValid)
                {
                    result.Errors.Add($"Type validation failed: {typeValidation.ErrorMessage}");
                }
                #endregion

                #region Validate Tags
                var tagsValidation = ValidateTags(window, expectedInfo.Tags);
                result.TagsValid = tagsValidation.IsValid;
                if (!tagsValidation.IsValid)
                {
                    result.Errors.Add($"Tags validation failed: {tagsValidation.ErrorMessage}");
                }
                #endregion

                #region Validate Description
                var descriptionValidation = ValidateDescription(window, expectedInfo.Description);
                result.DescriptionValid = descriptionValidation.IsValid;
                if (!descriptionValidation.IsValid)
                {
                    result.Errors.Add($"Description validation failed: {descriptionValidation.ErrorMessage}");
                }
                #endregion

                #region Validate Repost Type
                if (expectedInfo.IsRepost)
                {
                    var repostValidation = ValidateRepostType(window);
                    result.RepostTypeValid = repostValidation.IsValid;
                    if (!repostValidation.IsValid)
                    {
                        result.Errors.Add($"Repost type validation failed: {repostValidation.ErrorMessage}");
                    }

                    var sourceValidation = ValidateSourceAddress(window, expectedInfo.SourceAddress);
                    result.SourceAddressValid = sourceValidation.IsValid;
                    if (!sourceValidation.IsValid)
                    {
                        result.Errors.Add($"Source address validation failed: {sourceValidation.ErrorMessage}");
                    }
                }
                #endregion

                #region Validate Checkboxes
                var checkboxValidation = ValidateCheckboxes(window, expectedInfo.EnableOriginalWatermark, expectedInfo.EnableNoRepost);
                result.CheckboxesValid = checkboxValidation.IsValid;
                if (!checkboxValidation.IsValid)
                {
                    result.Errors.Add($"Checkboxes validation failed: {checkboxValidation.ErrorMessage}");
                }
                #endregion

                result.IsValid = result.TitleValid && result.TypeValid && result.TagsValid && 
                                 result.DescriptionValid && result.CheckboxesValid;

                if (expectedInfo.IsRepost)
                {
                    result.IsValid = result.IsValid && result.RepostTypeValid && result.SourceAddressValid;
                }

                Console.WriteLine($"\n=== Validation Result: {(result.IsValid ? "PASSED" : "FAILED")} ===");
                PrintValidationDetails(result, expectedInfo.IsRepost);

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                Console.WriteLine($"Validation error: {ex.Message}");
                return result;
            }
        }

        #region Private Validation Methods

        private FieldValidationResult ValidateTitle(AutomationElement window, string expectedTitle)
        {
            try
            {
                Console.WriteLine("  Validating title...");
                
                var editBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                Console.WriteLine($"    Found {editBoxes.Length} edit boxes for title validation");
                
                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        Console.WriteLine($"    Edit box at X={point.X}, Y={point.Y}");
                    }
                }
                
                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        if ((point.Y >= 190 && point.Y <= 210) || (point.Y >= 40 && point.Y <= 60))
                        {
                            Console.WriteLine($"    Found title edit box at X={point.X}, Y={point.Y}");
                            Console.WriteLine($"    Edit box Name: '{edit.Name}', AutomationId: '{edit.AutomationId}'");
                            
                            var actualTitle = GetEditBoxValue(edit);
                            var isValid = actualTitle == expectedTitle;
                            
                            Console.WriteLine($"    Expected: '{expectedTitle}'");
                            Console.WriteLine($"    Actual: '{actualTitle}'");
                            Console.WriteLine($"    Result: {(isValid ? "PASS" : "FAIL")}");
                            
                            if (!isValid)
                            {
                                Console.WriteLine($"    Title mismatch details:");
                                Console.WriteLine($"      Expected length: {expectedTitle?.Length ?? 0}");
                                Console.WriteLine($"      Actual length: {actualTitle?.Length ?? 0}");
                                Console.WriteLine($"      Expected bytes: [{string.Join(", ", System.Text.Encoding.UTF8.GetBytes(expectedTitle ?? ""))}]");
                                Console.WriteLine($"      Actual bytes: [{string.Join(", ", System.Text.Encoding.UTF8.GetBytes(actualTitle ?? ""))}]");
                            }
                            
                            return new FieldValidationResult
                            {
                                IsValid = isValid,
                                ErrorMessage = isValid ? "" : $"Title mismatch. Expected: '{expectedTitle}', Actual: '{actualTitle}'"
                            };
                        }
                    }
                }

                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Title edit box not found"
                };
            }
            catch (Exception ex)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Title validation exception: {ex.Message}"
                };
            }
        }

        private FieldValidationResult ValidateType(AutomationElement window, string expectedType, bool isRepost)
        {
            try
            {
                Console.WriteLine("  Validating type...");
                
                var editBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                Console.WriteLine($"    Found {editBoxes.Length} edit boxes for type validation");
                
                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        Console.WriteLine($"    Edit box at X={point.X}, Y={point.Y}");
                    }
                }
                
                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        if ((point.Y >= 280 && point.Y <= 300) || (point.Y >= 170 && point.Y <= 190))
                        {
                            var actualType = GetEditBoxValue(edit);
                            
                            Console.WriteLine($"    Expected Type: '{expectedType}'");
                            Console.WriteLine($"    Is Repost: {isRepost}");
                            Console.WriteLine($"    Actual Type Field: '{actualType}'");
                            
                            var isValid = true;
                            if (isRepost)
                            {
                                isValid = actualType == "转载";
                                Console.WriteLine($"    Validation: Repost mode, expecting '转载'");
                            }
                            else
                            {
                                isValid = true;
                                Console.WriteLine($"    Validation: Self-made mode - type field can have any value");
                            }
                            
                            Console.WriteLine($"    Result: {(isValid ? "PASS" : "FAIL")}");
                            
                            return new FieldValidationResult
                            {
                                IsValid = isValid,
                                ErrorMessage = isValid ? "" : $"Type mismatch. Expected: '{expectedType}', Actual: '{actualType}'"
                            };
                        }
                    }
                }

                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Type edit box not found"
                };
            }
            catch (Exception ex)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Type validation exception: {ex.Message}"
                };
            }
        }

        private FieldValidationResult ValidateTags(AutomationElement window, List<string> expectedTags)
        {
            try
            {
                Console.WriteLine("  Validating tags...");
                
                #region Validate Tag Buttons
                var buttons = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
                Console.WriteLine($"    Found {buttons.Length} buttons for tags validation");
                
                var foundTags = new List<string>();
                foreach (var button in buttons)
                {
                    if (button.TryGetClickablePoint(out var point))
                    {
                        if ((point.Y >= 340 && point.Y <= 350) || (point.Y >= 260 && point.Y <= 280))
                        {
                            var buttonName = button.Name;
                            Console.WriteLine($"    Found tag button: '{buttonName}' at X={point.X}, Y={point.Y}");
                            
                            if (buttonName.Contains(" ×"))
                            {
                                var tagName = buttonName.Replace(" ×", "");
                                if (!string.IsNullOrEmpty(tagName) && tagName != "必剪创作")
                                {
                                    foundTags.Add(tagName);
                                    Console.WriteLine($"      Extracted tag: '{tagName}'");
                                }
                            }
                        }
                    }
                }
                #endregion

                var allTagsPresent = expectedTags.All(tag => foundTags.Contains(tag));
                var isValid = allTagsPresent && foundTags.Count == expectedTags.Count;
                
                Console.WriteLine($"    Expected tags: [{string.Join(", ", expectedTags)}]");
                Console.WriteLine($"    Found tags: [{string.Join(", ", foundTags)}]");
                Console.WriteLine($"    Expected count: {expectedTags.Count}, Found count: {foundTags.Count}");
                Console.WriteLine($"    Result: {(isValid ? "PASS" : "FAIL")}");
                
                if (!isValid)
                {
                    Console.WriteLine($"    Tags mismatch details:");
                    var missingTags = expectedTags.Except(foundTags).ToList();
                    var extraTags = foundTags.Except(expectedTags).ToList();
                    
                    if (missingTags.Any())
                    {
                        Console.WriteLine($"      Missing tags: [{string.Join(", ", missingTags)}]");
                    }
                    if (extraTags.Any())
                    {
                        Console.WriteLine($"      Extra tags: [{string.Join(", ", extraTags)}]");
                    }
                }
                
                return new FieldValidationResult
                {
                    IsValid = isValid,
                    ErrorMessage = isValid ? "" : $"Tags mismatch. Expected: [{string.Join(", ", expectedTags)}], Found: [{string.Join(", ", foundTags)}]"
                };
            }
            catch (Exception ex)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Tags validation exception: {ex.Message}"
                };
            }
        }

        private FieldValidationResult ValidateDescription(AutomationElement window, string expectedDescription)
        {
            try
            {
                Console.WriteLine("  Validating description...");
                
                var editBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                Console.WriteLine($"    Found {editBoxes.Length} edit boxes for description validation");
                
                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        Console.WriteLine($"    Edit box at X={point.X}, Y={point.Y}");
                    }
                }
                
                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        if ((point.Y >= 620 && point.Y <= 640) || (point.Y >= 450 && point.Y <= 470))
                        {
                            Console.WriteLine($"    Found description edit box at X={point.X}, Y={point.Y}");
                            Console.WriteLine($"    Edit box Name: '{edit.Name}', AutomationId: '{edit.AutomationId}'");
                            
                            var actualDescription = GetEditBoxValue(edit);
                            var isValid = actualDescription == expectedDescription;
                            
                            Console.WriteLine($"    Expected: '{expectedDescription}'");
                            Console.WriteLine($"    Actual: '{actualDescription}'");
                            Console.WriteLine($"    Result: {(isValid ? "PASS" : "FAIL")}");
                            
                            if (!isValid)
                            {
                                Console.WriteLine($"    Description mismatch details:");
                                Console.WriteLine($"      Expected length: {expectedDescription?.Length ?? 0}");
                                Console.WriteLine($"      Actual length: {actualDescription?.Length ?? 0}");
                                Console.WriteLine($"      Expected bytes: [{string.Join(", ", System.Text.Encoding.UTF8.GetBytes(expectedDescription ?? ""))}]");
                                Console.WriteLine($"      Actual bytes: [{string.Join(", ", System.Text.Encoding.UTF8.GetBytes(actualDescription ?? ""))}]");
                            }
                            
                            return new FieldValidationResult
                            {
                                IsValid = isValid,
                                ErrorMessage = isValid ? "" : $"Description mismatch. Expected: '{expectedDescription}', Actual: '{actualDescription}'"
                            };
                        }
                    }
                }

                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Description edit box not found"
                };
            }
            catch (Exception ex)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Description validation exception: {ex.Message}"
                };
            }
        }

        private FieldValidationResult ValidateRepostType(AutomationElement window)
        {
            try
            {
                Console.WriteLine("  Validating repost type...");
                
                var radioButtons = window.FindAllDescendants(cf => cf.ByControlType(ControlType.RadioButton));
                var secondRadio = radioButtons.Skip(1).FirstOrDefault();
                
                if (secondRadio != null)
                {
                    var isSelected = secondRadio.Patterns.SelectionItem.Pattern.IsSelected.Value;
                    
                    Console.WriteLine($"    Second radio button (转载) selected: {isSelected}");
                    Console.WriteLine($"    Result: {(isSelected ? "PASS" : "FAIL")}");
                    
                    return new FieldValidationResult
                    {
                        IsValid = isSelected,
                        ErrorMessage = isSelected ? "" : "Repost type radio button not selected"
                    };
                }

                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Repost type radio button not found"
                };
            }
            catch (Exception ex)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Repost type validation exception: {ex.Message}"
                };
            }
        }

        private FieldValidationResult ValidateSourceAddress(AutomationElement window, string expectedSourceAddress)
        {
            try
            {
                Console.WriteLine("  Validating source address...");
                
                var editBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                
                foreach (var edit in editBoxes)
                {
                    if (edit.TryGetClickablePoint(out var point))
                    {
                        if ((point.Y >= 305 && point.Y <= 315) || (point.Y >= 120 && point.Y <= 140))
                        {
                            var actualSourceAddress = GetEditBoxValue(edit);
                            var isValid = actualSourceAddress == expectedSourceAddress;
                            
                            Console.WriteLine($"    Expected: '{expectedSourceAddress}'");
                            Console.WriteLine($"    Actual: '{actualSourceAddress}'");
                            Console.WriteLine($"    Result: {(isValid ? "PASS" : "FAIL")}");
                            
                            return new FieldValidationResult
                            {
                                IsValid = isValid,
                                ErrorMessage = isValid ? "" : $"Source address mismatch. Expected: '{expectedSourceAddress}', Actual: '{actualSourceAddress}'"
                            };
                        }
                    }
                }

                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Source address edit box not found"
                };
            }
            catch (Exception ex)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Source address validation exception: {ex.Message}"
                };
            }
        }

        private FieldValidationResult ValidateCheckboxes(AutomationElement window, bool expectedOriginalWatermark, bool expectedNoRepost)
        {
            try
            {
                Console.WriteLine("  Validating checkboxes...");
                
                var checkBoxes = window.FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox));
                var allValid = true;
                var errorMessages = new List<string>();

                foreach (var checkBox in checkBoxes)
                {
                    if (expectedOriginalWatermark && checkBox.Name.Contains("原创视频专属水印"))
                    {
                        var isChecked = checkBox.Patterns.Toggle.Pattern.ToggleState.Value.HasFlag(ToggleState.On);
                        var isValid = isChecked;
                        
                        Console.WriteLine($"    Checkbox '{checkBox.Name}': Expected=Checked, Actual={(isChecked ? "Checked" : "Unchecked")}, Result={(isValid ? "PASS" : "FAIL")}");
                        
                        if (!isValid)
                        {
                            allValid = false;
                            errorMessages.Add($"Original watermark checkbox not checked");
                        }
                    }
                    else if (expectedNoRepost && checkBox.Name.Contains("未经作者授权"))
                    {
                        var isChecked = checkBox.Patterns.Toggle.Pattern.ToggleState.Value.HasFlag(ToggleState.On);
                        var isValid = isChecked;
                        
                        Console.WriteLine($"    Checkbox '{checkBox.Name}': Expected=Checked, Actual={(isChecked ? "Checked" : "Unchecked")}, Result={(isValid ? "PASS" : "FAIL")}");
                        
                        if (!isValid)
                        {
                            allValid = false;
                            errorMessages.Add($"No repost checkbox not checked");
                        }
                    }
                }

                return new FieldValidationResult
                {
                    IsValid = allValid,
                    ErrorMessage = allValid ? "" : string.Join("; ", errorMessages)
                };
            }
            catch (Exception ex)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Checkboxes validation exception: {ex.Message}"
                };
            }
        }

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

        private void PrintValidationDetails(ValidationResult result, bool isRepost)
        {
            Console.WriteLine("\n--- Validation Details ---");
            Console.WriteLine($"  Title: {(result.TitleValid ? "✓" : "✗")}");
            Console.WriteLine($"  Type: {(result.TypeValid ? "✓" : "✗")}");
            Console.WriteLine($"  Tags: {(result.TagsValid ? "✓" : "✗")}");
            Console.WriteLine($"  Description: {(result.DescriptionValid ? "✓" : "✗")}");
            
            if (isRepost)
            {
                Console.WriteLine($"  Repost Type: {(result.RepostTypeValid ? "✓" : "✗")}");
                Console.WriteLine($"  Source Address: {(result.SourceAddressValid ? "✓" : "✗")}");
            }
            
            Console.WriteLine($"  Checkboxes: {(result.CheckboxesValid ? "✓" : "✗")}");
            
            if (result.Errors.Any())
            {
                Console.WriteLine("\n--- Errors ---");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            Console.WriteLine("------------------------\n");
        }

        #endregion
    }

    #region Validation Result Models

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public List<string> Errors { get; set; } = new List<string>();
        public bool TitleValid { get; set; }
        public bool TypeValid { get; set; }
        public bool TagsValid { get; set; }
        public bool DescriptionValid { get; set; }
        public bool RepostTypeValid { get; set; }
        public bool SourceAddressValid { get; set; }
        public bool CheckboxesValid { get; set; }
    }

    public class FieldValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    #endregion
}
