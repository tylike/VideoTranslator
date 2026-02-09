using System;
using System.Diagnostics;
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

        public PublishFormService()
        {
            _automation = new UIA3Automation();
            _validationService = new FormValidationService();
        }

        #region Public Methods

        public bool FillPublishFormWithInfo(IntPtr windowHandle, PublishInfo publishInfo)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var window = _automation.FromHandle(windowHandle);
                if (window == null)
                {
                    Console.WriteLine("Window not found.", MessageType.Error);
                    return false;
                }

                Console.WriteLine("\n=== Filling Publish Form ===");
                
                
                var model = new PublishFormModel(window);

                if (!model.MoreOptionsExpandButton.IsChecked)
                {
                    try
                    {
                        model.MoreOptionsExpandButton.IsChecked = true;
                    }
                    catch (Exception ex)
                    {
                        if (model.MoreOptionsExpandButton.IsChecked)
                        {
                            Console.WriteLine("报错但也能成功!");
                        }
                        else
                        {
                            Console.WriteLine("报错没能成功!");
                        }
                    }
                }

                model.AcceptTermsCheckbox.IsChecked = true;
                WindowCaptureHelper.SaveUiElementsToFile(window, "01_自载转载",true);
                Console.WriteLine("  Filling type...");
                model.SetIsRepost(publishInfo.IsRepost,publishInfo.SourceAddress);
                

                Console.WriteLine("  Filling tags...");
                var tagsEditBox = model.TagsEditBox;
                if (tagsEditBox != null)
                {
                    FillTags(tagsEditBox, publishInfo.Tags);
                }

                Console.WriteLine("  Filling description...");
                var descriptionEditBox = model.DescriptionEditBox;
                if (descriptionEditBox != null)
                {
                    FillDescription(descriptionEditBox, publishInfo.Description);
                }

                WindowCaptureHelper.SaveUiElementsToFile(window, "07_准备填写水印", true);
                //原创视频专属水印
                if (model.OriginalWatermarkCheckbox!=null)
                {
                    model.OriginalWatermarkCheckbox.IsChecked = publishInfo.EnableOriginalWatermark;
                }
                //禁止转发
                if (model.NoRepostCheckbox != null)
                {
                    model.NoRepostCheckbox.IsChecked = publishInfo.EnableNoRepost;
                }
                //ClickMoreOptions(window);
                //ControlCheckboxes(window, publishInfo.EnableOriginalWatermark, publishInfo.EnableNoRepost, publishInfo.IsRepost);

                Console.WriteLine("\n=== Filling title last to avoid being overwritten ===");
                var titleEditBox = model.TitleEditBox;
                if (titleEditBox != null)
                {
                    Console.WriteLine("  Filling title using model...");
                    titleEditBox.Text = publishInfo.Title;
                    Console.WriteLine($"  Title filled: '{titleEditBox.Text}'", MessageType.Success);
                }
                else
                {
                    //Console.WriteLine("  Title edit box not found using model, falling back to manual search...", MessageType.Error);                    
                    throw new Exception("Title edit box not found.");
                }

                Console.WriteLine("\n=== Waiting for UI to update after form filling ===");
                System.Threading.Thread.Sleep(500);

                WindowCaptureHelper.SaveUiElementsToFile(window, "09_After_Form_Filling");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filling form: {ex.Message}", MessageType.Error);
                return false;
            }
            finally
            {
                sw.Stop();
                Console.WriteLine($"=== Form filling completed in {sw.Elapsed.TotalMilliseconds} ms ===\n");
            }
        }

        #endregion

        #region Form Filling Methods

        private void FillTags(TextBox editBox, List<string> tags)
        {
            try
            {
                Console.WriteLine("  Filling tags...");
                editBox.Click();
                editBox.Focus();
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

        private void FillDescription(TextBox editBox, string description)
        {
            try
            {
                Console.WriteLine("  Filling description...");
                editBox.Click();
                System.Threading.Thread.Sleep(200);

                editBox.Text = description;
                System.Threading.Thread.Sleep(300);

                var actualValue = editBox.Text;
                Console.WriteLine($"  Description filled: '{actualValue}'", MessageType.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error filling description: {ex.Message}", MessageType.Error);
            }
        }
        
        #endregion
    }
}
