using System;
using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace PublishToBilibili.Services
{
    public class PublishFormModel
    {
        private readonly AutomationElement _window;
        private readonly UIA3Automation _automation;

        public PublishFormModel(AutomationElement window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _automation = new UIA3Automation();
        }

        public TextBox TitleEditBox => FindTitleEditBox();
        public TextBox TypeEditBox => FindTypeEditBox();
        public TextBox TagsEditBox => FindTagsEditBox();
        public TextBox DescriptionEditBox => FindDescriptionEditBox();
        public TextBox SourceAddressEditBox => FindSourceAddressEditBox();

        public CheckBox OriginalWatermarkCheckbox => FindOriginalWatermarkCheckbox();
        public CheckBox NoRepostCheckbox => FindNoRepostCheckbox();
        public CheckBox AcceptTermsCheckbox => FindAcceptTermsCheckbox();

        public Label MoreOptionsText => FindTextByName("更多选项");
        public RadioButton MoreOptionsExpandButton => FindMoreOptionsExpandButton();

        public RadioButton SelfMadeRadioButton => FindSelfMadeRadioButton();
        public RadioButton RepostRadioButton => FindRepostRadioButton();

        public Button[] TagButtons => FindTagButtons();

        #region Path-based Finding Methods

        private TextBox FindTitleEditBox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindWithSibling<TextBox>(
                x => x.ControlType == ControlType.Text && x.Name == "标题",
                x => x.ControlType == ControlType.Custom,
                x => x.ControlType == ControlType.Edit && x.ClassName == "CBCLineEdit"
            );
        }

        private TextBox FindTypeEditBox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindWithSibling<TextBox>(
                x => x.ControlType == ControlType.Text && x.Name == "类型",
                x => x.ControlType == ControlType.Custom,
                x => x.ControlType == ControlType.Edit && x.ClassName == "CBCLineEdit"
            );
        }

        private TextBox FindTagsEditBox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindWithSibling<TextBox>(
                x => x.ControlType == ControlType.Text && x.Name == "标签",
                x => x.ControlType == ControlType.Custom,
                x => x.ControlType == ControlType.Edit && x.ClassName == "CBCLineEdit"
            );
        }

        private TextBox FindDescriptionEditBox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindWithSibling<TextBox>(
                x => x.ControlType == ControlType.Text && x.Name == "简介",
                x => x.ControlType == ControlType.Custom,
                x => x.ControlType == ControlType.Edit && x.ClassName == "BTextEdit"
            );
        }

        private TextBox FindSourceAddressEditBox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindWithSibling<TextBox>(
                x => x.ControlType == ControlType.Text && x.Name.Contains("来源"),
                x => x.ControlType == ControlType.Custom,
                x => x.ControlType == ControlType.Edit && x.ClassName == "CBCLineEdit"
            );
        }

        private CheckBox FindAcceptTermsCheckbox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindSibling<CheckBox>(
                x => x.ControlType == ControlType.Button && x.Name.Contains("《哔哩哔哩创作公约》"),
                x => x.ControlType == ControlType.CheckBox && x.Name == "我已阅读并接受"
            );
        }

        private CheckBox FindOriginalWatermarkCheckbox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindSibling<CheckBox>(
                x => x.ControlType == ControlType.Text && x.Name == "视频水印",
                x => x.ControlType == ControlType.CheckBox && x.Name.Contains("原创视频专属水印")
            );
        }

        private CheckBox FindNoRepostCheckbox()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindSibling<CheckBox>(
                x => x.ControlType == ControlType.Text && x.Name == "自制声明",
                x => x.ControlType == ControlType.CheckBox && x.Name.Contains("未经作者授权")
            );
        }

        private RadioButton FindMoreOptionsExpandButton()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindSibling<RadioButton>(
                x => x.ControlType == ControlType.Text && x.Name == "更多选项",
                x => x.ControlType == ControlType.RadioButton && x.ClassName == "QRadioButton"
            );
        }

        private RadioButton FindSelfMadeRadioButton()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindWithSibling<RadioButton>(
                x => x.ControlType == ControlType.Text && x.Name == "类型",
                x => x.ControlType == ControlType.Custom,
                x => x.ControlType == ControlType.RadioButton && x.ClassName == "QRadioButton"
            );
        }

        private RadioButton FindRepostRadioButton()
        {
            var finder = new UiPathFinder(_window);
            return finder.FindSibling<RadioButton>(
                x => x.ControlType == ControlType.Text && x.Name == "转载",
                x => x.ControlType == ControlType.RadioButton && x.ClassName == "QRadioButton"
            );
        }

        #endregion

        #region Legacy Index-based Finding Methods

        private TextBox FindEditBoxByIndex(int index)
        {
            var editBoxes = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
            if (index >= 0 && index < editBoxes.Length)
            {
                return editBoxes[index].AsTextBox();
            }
            return null;
        }

        private CheckBox FindCheckboxByName(string name)
        {
            var checkboxes = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox));
            foreach (var checkbox in checkboxes)
            {
                if (checkbox.Name.Contains(name))
                {
                    return checkbox.AsCheckBox();
                }
            }
            return null;
        }

        private Label FindTextByName(string name)
        {
            var texts = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
            foreach (var text in texts)
            {
                if (text.Name.Contains(name))
                {
                    return text.AsLabel();
                }
            }
            return null;
        }

        private Button[] FindTagButtons()
        {
            var buttons = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
            var tagButtons = new List<Button>();
            
            foreach (var button in buttons)
            {
                if (button.TryGetClickablePoint(out var point))
                {
                    if (button.Name.Contains(" ×"))
                    {
                        var tagName = button.Name.Replace(" ×", "");
                        if (!string.IsNullOrEmpty(tagName) && tagName != "必剪创作")
                        {
                            tagButtons.Add(button.AsButton());
                        }
                    }
                }
            }
            
            return tagButtons.ToArray();
        }

        #endregion

        public string[] GetTags()
        {
            var buttons = TagButtons;
            var tags = new List<string>();
            
            foreach (var button in buttons)
            {
                var name = button.Name;
                if (name.Contains(" ×"))
                {
                    var tagName = name.Replace(" ×", "");
                    if (!string.IsNullOrEmpty(tagName) && tagName != "必剪创作")
                    {
                        tags.Add(tagName);
                    }
                }
            }
            
            return tags.ToArray();
        }

        public void SetIsRepost(bool isRepost,string sourceAddress)
        {
            try
            {
                var repostRadio = RepostRadioButton;
                var selfMadeRadio = SelfMadeRadioButton;

                if (repostRadio == null || selfMadeRadio == null)
                {
                    return;
                }

                if (isRepost)
                {
                    if (repostRadio.Patterns.SelectionItem.Pattern != null)
                    {
                        repostRadio.Patterns.SelectionItem.Pattern.Select();
                    }
                    else
                    {
                        repostRadio.Click();
                    }
                    if (SourceAddressEditBox != null)
                        SourceAddressEditBox.Text = sourceAddress;
                }
                else
                {
                    if (selfMadeRadio.Patterns.SelectionItem.Pattern != null)
                    {
                        selfMadeRadio.Patterns.SelectionItem.Pattern.Select();
                    }
                    else
                    {
                        selfMadeRadio.Click();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting repost type: {ex.Message}");
            }
        }

        public bool GetIsRepost()
        {
            var repostRadio = RepostRadioButton;
            if (repostRadio == null)
            {
                return false;
            }

            try
            {
                var togglePattern = repostRadio.Patterns.Toggle.Pattern;
                if (togglePattern != null)
                {
                    return togglePattern.ToggleState.Value == FlaUI.Core.Definitions.ToggleState.On;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}