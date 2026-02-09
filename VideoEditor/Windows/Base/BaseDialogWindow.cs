using System.Windows;
using System.Windows.Controls;
using SystemWindow = System.Windows.Window;

namespace VideoEditor.Windows;

public partial class BaseDialogWindow : SystemWindow
{
    #region 字段

    private readonly List<Button> _dialogButtons = new();

    #endregion

    #region 属性

    public IEnumerable<Button> DialogButtons => _dialogButtons;

    #endregion

    #region 构造函数

    public BaseDialogWindow()
    {
        ResizeMode = ResizeMode.CanResize;
        Width = 1024;
        Height = 768;
    }

    #endregion

    #region 按钮管理

    public Button AddButton(string content, RoutedEventHandler clickHandler, bool isDefault = false, bool isCancel = false)
    {
        var button = new Button
        {
            Content = content,
            Width = 80,
            Height = 30,
            Margin = new Thickness(10, 0, 0, 0),
            IsDefault = isDefault,
            IsCancel = isCancel
        };

        button.Click += clickHandler;
        _dialogButtons.Add(button);

        return button;
    }

    public Button AddOkButton(RoutedEventHandler clickHandler)
    {
        return AddButton("确定", clickHandler, true, false);
    }

    public Button AddCancelButton(RoutedEventHandler clickHandler)
    {
        return AddButton("取消", clickHandler, false, true);
    }

    public void ClearButtons()
    {
        _dialogButtons.Clear();
    }

    #endregion

    #region 标准对话框方法

    protected void ShowOkCancelDialog(Action onOk, Action onCancel = null)
    {
        ClearButtons();

        AddOkButton((s, e) =>
        {
            onOk?.Invoke();
        });

        AddCancelButton((s, e) =>
        {
            onCancel?.Invoke();
            DialogResult = false;
            Close();
        });
    }

    protected void ShowYesNoDialog(Action onYes, Action onNo = null)
    {
        ClearButtons();

        AddButton("是", (s, e) =>
        {
            onYes?.Invoke();
        });

        AddButton("否", (s, e) =>
        {
            onNo?.Invoke();
            DialogResult = false;
            Close();
        });
    }

    #endregion
}
