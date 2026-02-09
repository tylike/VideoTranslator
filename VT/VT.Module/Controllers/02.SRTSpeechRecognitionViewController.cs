using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class SRTSpeechRecognitionViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public SRTSpeechRecognitionViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public SRTSpeechRecognitionViewController()
    {
        var whisperGenerateSRT = new AsyncSimpleAction(this, "ParseSourceSRT", null);
        whisperGenerateSRT.Caption = "2.识别源字幕";
        whisperGenerateSRT.AsyncExecute += WhisperGenerateSRT_Execute;

        var whisperGenerateSRTWithVad = new AsyncSimpleAction(this, "ParseSourceSRTVad", null);
        whisperGenerateSRTWithVad.Caption = "2.1.VAD增强识别";
        whisperGenerateSRTWithVad.AsyncExecute += WhisperGenerateSRTWithVad_Execute;
    }

    private async Task WhisperGenerateSRT_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        //await ViewCurrentObject.MediaSources.OfType<AudioSource>().Single(x => x.MediaType == MediaType.源音频).Audio2SRT();
    }

    private async Task WhisperGenerateSRTWithVad_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        //await ViewCurrentObject.MediaSources.OfType<AudioSource>().Single(x => x.MediaType == MediaType.源音频).SpeechRecognitionWithVad();
    }


}
