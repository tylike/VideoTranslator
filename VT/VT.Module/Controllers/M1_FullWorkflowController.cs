using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class M1_FullWorkflowController : VideoProjectController
{
    [ActivatorUtilitiesConstructor]
    public M1_FullWorkflowController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;

    public M1_FullWorkflowController()
    {
        var translateFullWorkflow = new AsyncSimpleAction(this, "ExecuteFullWorkflow", null);
        translateFullWorkflow.Caption = "M1.翻译流程";
        translateFullWorkflow.ToolTip = "自动执行从提取音频到翻译字幕流程";
        translateFullWorkflow.AsyncExecute += ExecuteFullWorkflow_Execute;

        var generateFinalVideo = new AsyncSimpleAction(this, "MakeGenerateFinalVideo", null);
        generateFinalVideo.Caption = "M2.生成视频";
        generateFinalVideo.AsyncExecute += GenerateFinalVideo_Execute;
    }

    private async Task GenerateFinalVideo_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        throw new NotImplementedException();
        //ViewCurrentObject.SaveSRT();
        //await ViewCurrentObject.GenerateTTS();
        ////await GenerateTTSViewController.TTS(this, false);
        //await AdjustAudioSegmentsViewController.AdjustAudioSegments(this);
        //await OverlayAudioViewController.OverlayAudio(this);    
        ////await MergeVideoViewController.MergeVideo(this);
        //await MergeVideoAndSubtitlesViewController.MergeVideoAndSubtitles(this);
        //await MergeVideoAndSubtitlesViewController.PublishToBilibili(this);
    }

    private async Task ExecuteFullWorkflow_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        //var videoProject = GetCurrentVideoProject();

        //videoProject.ValidateSourceVideo();

        //try
        //{
        //    var videoSource = videoProject.GetVideoSource();

        //    #region 步骤1: 提取音频
        //    await  videoSource.ExtractAudio();
        //    #endregion
        //    var audioSource = videoProject.GetAudioSource();
        //    #region 步骤2: 分离音频
        //    await audioSource.SeparateAudio();
        //    #endregion
        //    var vocalsAudio = videoProject.GetVocalsdAudio();
        //    #region 步骤3: 识别源字幕
        //    await vocalsAudio.Audio2SRT(null);
        //    #endregion
        //    var vocalAudioTrack = videoProject.Tracks.OfType<AudioTrackInfo>().Single(x=>x.Media.MediaType == MediaType.说话音频);
        //    var srtTrack = videoProject.Tracks.OfType<SRTTrackInfo>().Single(x=>x.Media.MediaType == MediaType.源字幕Vad);
        //    #region 步骤4: 源字幕分段
        //    await vocalAudioTrack.SegmentSourceAudioBySrt(srtTrack);
        //    #endregion

        //    #region 步骤5: 翻译字幕
        //    //await videoProject.TranslateSRTByGoogle();
        //    #endregion
        //}
        //catch (Exception ex)
        //{
        //    ObjectSpace.CommitChanges();
        //    Application.ShowViewStrategy.ShowMessage($"翻译流程异常: {ex.Message}", InformationType.Error);
        //}
        //finally
        //{
        //    self.ProgressService?.ResetProgress();
        //}
    }
}
