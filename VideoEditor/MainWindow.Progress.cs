using System.Windows;
using System.Windows.Controls;
using VideoEditor.Services;

namespace VideoEditor;

public partial class MainWindow
{
    #region 控件公开属性

    public TextBlock StatusTextControl => this.statusText;
    public ProgressBar StatusProgressBar => statusProgressBar;

    #endregion

    #region 事件处理

    private void DetailLogButton_Click(object sender, RoutedEventArgs e)
    {
        var progressService = (VideoEditorProgressService)this._progressService;
        progressService?.ShowDetailLogWindow();
    }

    #endregion
}
