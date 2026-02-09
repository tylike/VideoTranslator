using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VT.Module.BusinessObjects;
using VideoProject = VT.Module.BusinessObjects.VideoProject;

namespace VT.Module.Controllers;

public class SegmentSourceSRTViewController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public SegmentSourceSRTViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public SegmentSourceSRTViewController()
    {
        var segmentSourceSRT = new AsyncSimpleAction(this, "SegmentSourceSRT", null);
        segmentSourceSRT.Caption = "3.源音分段";
        segmentSourceSRT.AsyncExecute += SegmentSourceSRT_Execute;
    }

    private async Task SegmentSourceSRT_Execute(object sender, SimpleActionExecuteEventArgs e)
    {        
        //await ViewCurrentObject.MediaSources.OfType<AudioSource>().Single(x=>x.MediaType == MediaType.说话音频).seg();
    }

    
}
