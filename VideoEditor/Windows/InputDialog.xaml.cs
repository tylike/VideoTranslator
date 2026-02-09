using System.Windows;
using System.Windows.Input;

namespace VideoEditor.Windows;

public partial class InputDialog : Window
{
    #region 属性

    public string InputText
    {
        get { return inputTextBox.Text; }
        set { inputTextBox.Text = value; }
    }

    #endregion

    #region 构造函数

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        promptTextBlock.Text = prompt;
        InputText = defaultValue;
        
        Loaded += (s, e) => inputTextBox.Focus();
    }

    #endregion

    #region 事件处理

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DialogResult = true;
            Close();
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    #endregion
}
