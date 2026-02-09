using System.Windows;
using System.Windows.Controls;

namespace VideoEditor.Windows;

public class DialogTemplate : ContentControl
{
    #region 依赖属性

    public static readonly DependencyProperty ButtonsProperty =
        DependencyProperty.Register(
            nameof(Buttons),
            typeof(UIElement),
            typeof(DialogTemplate),
            new PropertyMetadata(null));

    #endregion

    #region 属性

    public UIElement Buttons
    {
        get => (UIElement)GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
    }

    #endregion

    #region 构造函数

    static DialogTemplate()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogTemplate), new FrameworkPropertyMetadata(typeof(DialogTemplate)));
    }

    #endregion
}
