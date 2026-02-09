using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using VideoTranslator.Interfaces;
using VT.Module.BusinessObjects.Whisper;
using VT.Module.Services;

namespace VT.Module.Controllers;

public class WhisperTaskViewController : ObjectViewController<DetailView,WhisperTask>
{
    private readonly IServiceProvider _serviceProvider;

    [ActivatorUtilitiesConstructor]
    public WhisperTaskViewController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public WhisperTaskViewController()
    {
                var executeAction = new SimpleAction(this, "ExecuteWhisperTask", "View")
        {
            Caption = "执行识别",
            ImageName = "Action_Execute",
            Category = "Actions"
        };
        executeAction.Execute += ExecuteAction_Execute;

        var cancelAction = new SimpleAction(this, "CancelWhisperTask", "View")
        {
            Caption = "取消任务",
            ImageName = "Action_Cancel",
            Category = "Actions"
        };
        cancelAction.Execute += CancelAction_Execute;

        var loadFromConfigAction = new SimpleAction(this, "LoadFromConfig", "View")
        {
            Caption = "从配置加载",
            ImageName = "Action_Open",
            Category = "Actions"
        };
        loadFromConfigAction.Execute += LoadFromConfigAction_Execute;

        var createFromPresetAction = new SimpleAction(this, "CreateFromPreset", "View")
        {
            Caption = "从预设创建",
            ImageName = "Action_New",
            Category = "Actions"
        };
        createFromPresetAction.Execute += CreateFromPresetAction_Execute;

        var openResultAction = new SimpleAction(this, "OpenResult", "View")
        {
            Caption = "查看结果",
            ImageName = "BO_DetailView",
            Category = "Actions"
        };
        openResultAction.Execute += OpenResultAction_Execute;

        var openOutputFileAction = new SimpleAction(this, "OpenOutputFile", "View")
        {
            Caption = "打开输出文件",
            ImageName = "Action_Open_File",
            Category = "Actions"
        };
        openOutputFileAction.Execute += OpenOutputFileAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        UpdateActionState();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        UpdateActionState();
    }

    private void UpdateActionState()
    {
        if (View.CurrentObject is not WhisperTask task)
        {
            return;
        }

        //var executeAction = Actions["ExecuteWhisperTask"];
        //var cancelAction = Actions["CancelWhisperTask"];
        //var loadFromConfigAction = Actions["LoadFromConfig"];
        //var createFromPresetAction = Actions["CreateFromPreset"];
        //var openResultAction = Actions["OpenResult"];
        //var openOutputFileAction = Actions["OpenOutputFile"];

        //executeAction.Enabled["TaskStatus"] = task.Status == WhisperTaskStatus.Pending;
        //cancelAction.Enabled["TaskStatus"] = task.Status == WhisperTaskStatus.Running;
        //loadFromConfigAction.Enabled["TaskStatus"] = task.Status == WhisperTaskStatus.Pending;
        //createFromPresetAction.Enabled["TaskStatus"] = task.Status == WhisperTaskStatus.Pending;
        //openResultAction.Enabled["HasResult"] = task.Result != null;
        //openOutputFileAction.Enabled["HasOutput"] = !string.IsNullOrEmpty(task.OutputFilePath) && File.Exists(task.OutputFilePath);
    }

    private async void ExecuteAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not WhisperTask task)
        {
            return;
        }

        var progress = Application.ServiceProvider.GetRequiredService<IProgressService>();
        var executionService = _serviceProvider.GetRequiredService<IWhisperExecutionService>();

        try
        {
            progress.ShowProgress();
            progress.SetStatusMessage("正在执行 Whisper 识别...", MessageType.Info);

            var result = await executionService.ExecuteAsync(task, progress);

            progress.HideProgress();
            progress.Success($"识别完成！处理时长: {task.ProcessingDuration:F2}秒");

            ObjectSpace.CommitChanges();
            View.Refresh();
        }
        catch (Exception ex)
        {
            progress.HideProgress();
            progress.Error($"识别失败: {ex.Message}");
            throw;
        }
    }

    private void CancelAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not WhisperTask task)
        {
            return;
        }

        if (task.Status != WhisperTaskStatus.Running)
        {
            return;
        }

        task.Status = WhisperTaskStatus.Cancelled;
        task.ErrorMessage = "用户取消了任务";
        ObjectSpace.CommitChanges();
        View.Refresh();
    }

    private void LoadFromConfigAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not WhisperTask task)
        {
            return;
        }

        if (task.Status != WhisperTaskStatus.Pending)
        {
            return;
        }

        var config = ObjectSpace.GetObjects<WhisperConfig>().FirstOrDefault();
        if (config is null)
        {
            return;
        }

        task.Config = config;

        task.Language = config.Language;
        task.OutputFormat = config.OutputFormat;
        task.AudioFilePath = config.AudioFilePath ?? task.AudioFilePath;
        task.OutputFilePath = config.OutputFilePath ?? task.OutputFilePath;
        task.ModelPath = config.ModelPath;
        task.DetectLanguage = config.DetectLanguage;
        task.Prompt = config.Prompt;
        task.MaxLength = config.MaxLength;
        task.MaxContext = config.MaxContext;
        task.SplitOnWord = config.SplitOnWord;
        task.BestOf = config.BestOf;
        task.BeamSize = config.BeamSize;
        task.Temperature = config.Temperature;
        task.TemperatureInc = config.TemperatureInc;
        task.WordThreshold = config.WordThreshold;
        task.EntropyThreshold = config.EntropyThreshold;
        task.LogProbThreshold = config.LogProbThreshold;
        task.NoSpeechThreshold = config.NoSpeechThreshold;
        task.NoFallback = config.NoFallback;
        task.Translate = config.Translate;
        task.DebugMode = config.DebugMode;
        task.Diarize = config.Diarize;
        task.TinyDiarize = config.TinyDiarize;
        task.PrintSpecial = config.PrintSpecial;
        task.PrintColors = config.PrintColors;
        task.PrintConfidence = config.PrintConfidence;
        task.NoTimestamps = config.NoTimestamps;
        task.LogScore = config.LogScore;
        task.NoGpu = config.NoGpu;
        task.FlashAttention = config.FlashAttention;
        task.SuppressNonSpeechTokens = config.SuppressNonSpeechTokens;
        task.SuppressRegex = config.SuppressRegex;
        task.Grammar = config.Grammar;
        task.GrammarRule = config.GrammarRule;
        task.GrammarPenalty = config.GrammarPenalty;
        task.EnableVad = config.EnableVad;
        task.VadModelPath = config.VadModelPath;
        task.VadThreshold = config.VadThreshold;
        task.VadMinSpeechDurationMs = config.VadMinSpeechDurationMs;
        task.VadMinSilenceDurationMs = config.VadMinSilenceDurationMs;
        task.VadMaxSpeechDurationS = config.VadMaxSpeechDurationS;
        task.VadSpeechPadMs = config.VadSpeechPadMs;
        task.VadSamplesOverlap = config.VadSamplesOverlap;
        task.FontPath = config.FontPath;
        task.OpenVinoDevice = config.OpenVinoDevice;
        task.DtwModel = config.DtwModel;

        ObjectSpace.CommitChanges();
        View.Refresh();
    }

    private void CreateFromPresetAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not WhisperTask task)
        {
            return;
        }

        if (task.Status != WhisperTaskStatus.Pending)
        {
            return;
        }

        var preset = ObjectSpace.GetObjects<WhisperPreset>().FirstOrDefault();
        if (preset is null)
        {
            return;
        }

        task.Language = preset.Language;
        task.OutputFormat = preset.OutputFormat;
        task.EnableVad = preset.EnableVad;
        task.EnableTranslation = preset.EnableTranslation;
        task.EnableDiarization = preset.EnableDiarization;
        task.Temperature = preset.Temperature;
        task.BestOf = preset.BestOf;
        task.BeamSize = preset.BeamSize;
        task.MaxLength = preset.MaxLength;
        task.SplitOnWord = preset.SplitOnWord;
        task.VadThreshold = preset.VadThreshold;
        task.VadMinSpeechDurationMs = preset.VadMinSpeechDurationMs;
        task.VadMinSilenceDurationMs = preset.VadMinSilenceDurationMs;

        preset.UsageCount++;
        preset.LastUsedTime = DateTime.Now;
        preset.Save();

        ObjectSpace.CommitChanges();
        View.Refresh();
    }

    private void OpenResultAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not WhisperTask task)
        {
            return;
        }

        if (task.Result is null)
        {
            return;
        }

        var result = task.Result;
        Application.ShowViewStrategy.ShowMessage($"识别结果: {result.ResultName}");
    }

    private void OpenOutputFileAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not WhisperTask task)
        {
            return;
        }

        if (string.IsNullOrEmpty(task.OutputFilePath) || !File.Exists(task.OutputFilePath))
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = task.OutputFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"无法打开文件: {task.OutputFilePath}", ex);
        }
    }
}
