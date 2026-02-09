using System.Windows;
using System.Windows.Controls;

namespace VideoEditor.Windows;

public partial class SampleDialogWindow : BaseDialogWindow
{
    #region 字段

    private TextBox _nameTextBox;
    private TextBox _emailTextBox;

    #endregion

    #region 构造函数

    public SampleDialogWindow()
    {
        InitializeComponent();

        InitializeContent();
        InitializeButtons();
    }

    #endregion

    #region 初始化

    private void InitializeContent()
    {
        var grid = new Grid
        {
            Margin = new Thickness(10)
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var nameLabel = new TextBlock
        {
            Text = "姓名:",
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(nameLabel, 0);

        _nameTextBox = new TextBox
        {
            Margin = new Thickness(0, 0, 0, 15)
        };
        Grid.SetRow(_nameTextBox, 1);

        var emailLabel = new TextBlock
        {
            Text = "邮箱:",
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(emailLabel, 2);

        _emailTextBox = new TextBox
        {
            Margin = new Thickness(0, 0, 0, 15)
        };
        Grid.SetRow(_emailTextBox, 3);

        grid.Children.Add(nameLabel);
        grid.Children.Add(_nameTextBox);
        grid.Children.Add(emailLabel);
        grid.Children.Add(_emailTextBox);

        Content = grid;
    }

    private void InitializeButtons()
    {
        ShowOkCancelDialog(
            onOk: () =>
            {
                if (ValidateInput())
                {
                    DialogResult = true;
                    Close();
                }
            },
            onCancel: () =>
            {
                _nameTextBox.Clear();
                _emailTextBox.Clear();
            }
        );
    }

    #endregion

    #region 验证

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            MessageBox.Show("请输入姓名", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            _nameTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_emailTextBox.Text))
        {
            MessageBox.Show("请输入邮箱", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            _emailTextBox.Focus();
            return false;
        }

        return true;
    }

    #endregion

    #region 属性

    public string Name => _nameTextBox.Text;
    public string Email => _emailTextBox.Text;

    #endregion
}
