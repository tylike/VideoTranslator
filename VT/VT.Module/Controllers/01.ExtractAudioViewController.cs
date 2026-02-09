using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class ExtractAudioViewController : VideoProjectController
{
    public ExtractAudioViewController()
    {
        var extractAudio = new AsyncSimpleAction(this, "ExtractAudio", null);
        extractAudio.Caption = "1.提取音频";
        extractAudio.AsyncExecute += ExtractAudio_Execute;

        var separateAudio = new AsyncSimpleAction(this, "SeparateAudio", null);
        separateAudio.Caption = "1.1.人声分离";
        separateAudio.AsyncExecute += SeparateAudio_Execute;
    }

    [ActivatorUtilitiesConstructor]
    public ExtractAudioViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;

    private async Task ExtractAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await (ViewCurrentObject.MediaSources.Single(x => x.MediaType == MediaType.Video) as VideoSource).ExtractAudio();
    }
    private async Task SeparateAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        //await (ViewCurrentObject.MediaSources.Single(x=>x.MediaType == MediaType.源音频) as AudioSource).SeparateAudio(this.ObjectSpace);
    }


}
