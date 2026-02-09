using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoTranslator.Interfaces;
using VT.Module.BusinessObjects;
using VT.Module.Services;

namespace VT.Module.Controllers;

public class AdjustAudioSegmentsViewController : VideoProjectController
{

    [ActivatorUtilitiesConstructor]
    public AdjustAudioSegmentsViewController(IServiceProvider serviceProvider) : this()
    {
        ServiceProvider = serviceProvider;
    }
    public AdjustAudioSegmentsViewController()
    {
        var adjustAudioSegments = new SimpleAction(this, "AdjustAudioSegments", null);
        adjustAudioSegments.Caption = "6.调整音频片段";
        adjustAudioSegments.Execute += AdjustAudioSegments_Execute;
    }

    private async void AdjustAudioSegments_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        await AdjustAudioSegments(this);
    }

    public static async Task AdjustAudioSegments(IServices self)
    {
        var videoProject = self.GetCurrentVideoProject();

        var ttsSegmentsDir = Path.Combine(videoProject.ProjectPath, "tts_segments");
        var adjustedDir = Path.Combine(videoProject.ProjectPath, "adjusted_segments");
        var audioInfo = await self.AudioService.GetAudioInfoAsync(videoProject.SourceAudioPath);

        Directory.CreateDirectory(adjustedDir);

        var adjustedCount = 0;
        foreach (var timeLineClip in videoProject.Clips)
        {
            if (timeLineClip.TargetAudioClip != null && timeLineClip.SourceSRTClip != null)
            {
                await timeLineClip.Adjust(adjustedDir, audioInfo);
                adjustedCount++;
            }
        }
        self.ObjectSpace.CommitChanges();
    }

    internal class ControllerSegmentProgressCallback : ISegmentProgressCallback
    {
        private readonly ViewController _controller;
        private readonly SimpleAction _action;

        public ControllerSegmentProgressCallback(ViewController controller, SimpleAction action)
        {
            _controller = controller;
            _action = action;
        }

        public void ReportProgress(string message)
        {
        }

        public void ReportProgress(int current, int total, string message)
        {
        }

        public void LogOperation(string operation, params (string name, object value)[] parameters)
        {
        }

        public void OnSegmentCompleted(int clipCount)
        {
        }

        public void OnSegmentFailed(string errorMessage)
        {
        }
    }
}