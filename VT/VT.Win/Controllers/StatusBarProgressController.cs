using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Win;
using VideoTranslator.Interfaces;
using System;
using System.Threading.Tasks;

namespace VT.Win.Controllers;

public class StatusBarProgressController : WindowController
{
    private SimpleAction _demoAction;
    private SimpleAction _demoMarqueeAction;
    private IProgressService? _progressService;

    public StatusBarProgressController()
    {
        TargetWindowType = WindowType.Main;
    }

    #region Protected Methods

    protected override void OnActivated()
    {
        base.OnActivated();
        _progressService = Application.ServiceProvider?.GetService(typeof(IProgressService)) as IProgressService;

        _demoAction = new SimpleAction(this, "DemoStatusBarProgress", null);
        _demoAction.Caption = "📊 演示确定进度";
        _demoAction.ToolTip = "演示状态栏进度条的使用（确定进度）";
        _demoAction.Execute += DemoAction_Execute;

        _demoMarqueeAction = new SimpleAction(this, "DemoMarqueeProgress", null);
        _demoMarqueeAction.Caption = "🔄 演示不确定进度";
        _demoMarqueeAction.ToolTip = "演示状态栏进度条的使用（不确定进度/Marquee）";
        _demoMarqueeAction.Execute += DemoMarqueeAction_Execute;
    }

    protected override void OnDeactivated()
    {
        if (_demoAction != null)
        {
            _demoAction.Execute -= DemoAction_Execute;
        }
        if (_demoMarqueeAction != null)
        {
            _demoMarqueeAction.Execute -= DemoMarqueeAction_Execute;
        }
        base.OnDeactivated();
    }

    #endregion

    #region Event Handlers

    private async void DemoAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (_progressService == null) return;

        _progressService.ShowProgress();
        _progressService.SetStatusMessage("开始处理...");

        for (int i = 0; i <= 100; i += 10)
        {
            _progressService.ReportProgress(i, null, 100);
            await Task.Delay(300);
        }

        _progressService.SetStatusMessage("处理完成！");
        await Task.Delay(1000);
        _progressService.ResetProgress();
    }

    private async void DemoMarqueeAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (_progressService == null) return;

        _progressService.ShowProgress(marquee: true);
        _progressService.SetStatusMessage("正在处理（不确定进度）...");

        await Task.Delay(3000);

        _progressService.SetStatusMessage("处理完成！");
        await Task.Delay(1000);
        _progressService.ResetProgress();
    }

    #endregion
}
