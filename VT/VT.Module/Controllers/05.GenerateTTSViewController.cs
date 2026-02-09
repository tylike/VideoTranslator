using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VT.Module.BusinessObjects;

namespace VT.Module.Controllers;

public class GenerateTTSViewController : VideoProjectController
{

    [ActivatorUtilitiesConstructor]
    public GenerateTTSViewController(IServiceProvider serviceProvider) : this() => ServiceProvider = serviceProvider;
    public GenerateTTSViewController()
    {
        var generateTTS = new AsyncSimpleAction(this, "GenerateTTS", null);
        generateTTS.Caption = "5.TTS";
        generateTTS.AsyncExecute += GenerateTTS_Execute;

        var regenerateTTS = new AsyncSimpleAction(this, "RegenerateTTS", null);
        regenerateTTS.Caption = "5.1.重新TTS";
        regenerateTTS.AsyncExecute += RegenerateTTS_Execute;

        //增加一个按钮,从已经存在的wav文件 中，保存到clips中去
        var restoreFromFile = new AsyncSimpleAction(this, "RestoreFromFile", null);
        restoreFromFile.Caption = "5.2从文件恢复";
        restoreFromFile.AsyncExecute += RestoreFromFile_Execute;

    }

    private async Task RestoreFromFile_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var videoProject = GetCurrentVideoProject();
        try
        {
            var successCount = await videoProject.RestoreTTSFromFile();
            ObjectSpace.CommitChanges();
            Application.ShowViewStrategy.ShowMessage($"成功恢复 {successCount} 个TTS音频文件");
        }
        catch (Exception ex)
        {
            Application.ShowViewStrategy.ShowMessage($"恢复TTS文件失败: {ex.Message}");
        }
    }

    private async Task GenerateTTS_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        throw new NotImplementedException();
        //var videoProject = GetCurrentVideoProject();
        //try
        //{
        //    var totalCount = await videoProject.GenerateTTS(false);
        //    ObjectSpace.CommitChanges();
        //    Application.ShowViewStrategy.ShowMessage($"TTS生成完成，共处理 {totalCount} 个片段");
        //}
        //catch (Exception ex)
        //{
        //    Application.ShowViewStrategy.ShowMessage($"TTS生成失败: {ex.Message}");
        //}
    }

    private async Task RegenerateTTS_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        throw new NotImplementedException();
        //var videoProject = GetCurrentVideoProject();
        //try
        //{
        //    var totalCount = await videoProject.GenerateTTS(true);
        //    ObjectSpace.CommitChanges();
        //    Application.ShowViewStrategy.ShowMessage($"TTS重新生成完成，共处理 {totalCount} 个片段");
        //}
        //catch (Exception ex)
        //{
        //    Application.ShowViewStrategy.ShowMessage($"TTS重新生成失败: {ex.Message}");
        //}
    }

}
