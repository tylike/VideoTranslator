using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VideoTranslator.Utils;
using VT.Module.BusinessObjects;
using VT.Module.Controllers;
using VT.Win.Forms;


namespace VT.Win.Controllers;

public class ShowVaveViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public ShowVaveViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;

    public ShowVaveViewController() : base()
    {
        var showWaveformViewer = new SimpleAction(this, "ShowWaveformViewer", null);
        showWaveformViewer.Caption = "🎵 音频波形查看器";
        showWaveformViewer.ToolTip = "查看音频片段的波形图";
        showWaveformViewer.Execute += ShowWaveformViewer_Execute;

        var showTimeLine = new SimpleAction(this, "ShowTimeLine", null);
        showTimeLine.Caption = "📊 时间线编辑器";
        showTimeLine.ToolTip = "查看和编辑时间线";
        showTimeLine.Execute += ShowTimeLine_Execute;
    }

    private void ShowWaveformViewer_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        InvokeOnUIThread(() =>
        {
            try
            {
                var form = new WaveformViewerForm(ViewCurrentObject.Clips.OrderBy(x=>x.Index).Select(x=>x.SourceAudioClip.FilePath).ToArray());
                form.Show();
                Application.ShowViewStrategy.ShowMessage("音频波形查看器已打开", InformationType.Success);
            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage($"打开音频波形查看器失败: {ex.Message}", InformationType.Error);
                throw;
            }
        });
    }

    private void ShowTimeLine_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        //InvokeOnUIThread(() =>
        //{
        //    try
        //    {
        //        var timeLineWindow = new TimeLineWindow(ViewCurrentObject);
        //        timeLineWindow.Show();
        //        Application.ShowViewStrategy.ShowMessage("时间线编辑器已打开", InformationType.Success);
        //    }
        //    catch (Exception ex)
        //    {
        //        Application.ShowViewStrategy.ShowMessage($"打开时间线编辑器失败: {ex.Message}", InformationType.Error);
        //        throw;
        //    }
        //});
    }


}
