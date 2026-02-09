using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VideoTranslator.SRT.Core.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class TranslateSRTViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public TranslateSRTViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public TranslateSRTViewController()
    {
        var translateSRT = new AsyncSimpleAction(this, "TranslateSRT", null);
        translateSRT.Caption = "4.翻译字幕";
        translateSRT.AsyncExecute += TranslateSRT_Execute;

        var translateSRT2 = new AsyncSimpleAction(this, "TranslateSRTByGoogle", null);
        translateSRT2.Caption = "4.1Google翻译";
        translateSRT2.AsyncExecute += TranslateSRTByGoogle_Execute;

        var translateSRT3 = new AsyncSimpleAction(this, "TranslateSRTByGoogleBatch", null);
        translateSRT3.Caption = "4.2应用Google翻译";
        translateSRT3.AsyncExecute += TranslateSRT3_Execute;

        var applyTranslatedSRT = new AsyncSimpleAction(this, "ApplyTranslatedSRT", null);
        applyTranslatedSRT.Caption = "4.3应用翻译字幕";
        applyTranslatedSRT.AsyncExecute += ApplyTranslatedSRT_Execute;

        var saveSRT = new SimpleAction(this, "SaveSRT", null);
        saveSRT.Execute += SaveSRT_Execute;
    }

    private void SaveSRT_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var videoProject = GetCurrentVideoProject();
        videoProject.SaveSRT();
    }

    private async Task TranslateSRTByGoogle_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        throw new NotImplementedException();
        //var videoProject = GetCurrentVideoProject();
        //await videoProject.TranslateSRTByGoogle();
    }

    private async Task TranslateSRT3_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var videoProject = GetCurrentVideoProject();
        var clips = videoProject.Clips.ToList();
        foreach (var item in clips)
        {
            item.ApplyTranslation();
        }
        ObjectSpace.CommitChanges();
    }

    private async Task TranslateSRT_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var action = sender as SimpleAction;
        if (action == null) return;

        var videoProject = GetCurrentVideoProject();
        await videoProject.TranslateSRT();

        Application.ShowViewStrategy.ShowMessage($"字幕翻译成功: {videoProject.TranslatedSubtitlePath}");
    }

    #region 应用翻译字幕

    private async Task ApplyTranslatedSRT_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var action = sender as SimpleAction;
        if (action == null) return;

        var videoProject = GetCurrentVideoProject();
        await videoProject.ApplyTranslatedSRT();

        Application.ShowViewStrategy.ShowMessage($"翻译字幕已应用");
    }

    #endregion
}
