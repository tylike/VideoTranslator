using System.Diagnostics;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.SRT.Core.Models;
using VideoTranslator.Utils;

namespace VideoTranslator.Services;

//public interface IVideoTranslationWorkflowService
//{
//    Task<WorkflowResult> ExecuteFullWorkflowAsync(WorkflowContext context);
//}

//public class WorkflowContext
//{
//    public string SourceVideoPath { get; set; } = string.Empty;
//    public string ProjectPath { get; set; } = string.Empty;
//    public string? SourceAudioPath { get; set; }
//    public string? SourceSubtitlePath { get; set; }
//    public string? SourceVocalAudioPath { get; set; }
//    public string? SourceBackgroundAudioPath { get; set; }
//    public string? TranslatedSubtitlePath { get; set; }
//}

//public class WorkflowResult
//{
//    public bool Success { get; set; }
//    public string? ErrorMessage { get; set; }
//    public List<WorkflowStepResult> StepResults { get; set; } = new();
//    public TimeSpan TotalDuration { get; set; }
//}

//public class WorkflowStepResult
//{
//    public string StepName { get; set; } = string.Empty;
//    public bool Success { get; set; }
//    public string? ErrorMessage { get; set; }
//    public TimeSpan Duration { get; set; }
//    public int ProcessedCount { get; set; }
//    public string? OutputPath { get; set; }
//}

//public class SegmentWorkflowStepResult : WorkflowStepResult
//{
//    public List<ClipInfo> Clips { get; set; } = new();
//}

//public enum WorkflowStep
//{
//    ExtractAudio,
//    SeparateAudio,
//    ParseSourceSRT,
//    SegmentSourceSRT,
//    TranslateSRT
//}
//public class VideoTranslationWorkflowService : ServiceBase, IVideoTranslationWorkflowService
//{
//    private readonly IFFmpegService _ffmpegService;
//    private readonly ISpeechRecognitionService _speechRecognitionService;
//    private readonly ISubtitleService _subtitleService;
//    private readonly IAudioService _audioService;
//    private readonly IAudioSeparationService _audioSeparationService;
//    private readonly ITranslationService _translationService;
    

//    public VideoTranslationWorkflowService(
//        IFFmpegService ffmpegService,
//        ISpeechRecognitionService speechRecognitionService,
//        ISubtitleService subtitleService,
//        IAudioService audioService,
//        IAudioSeparationService audioSeparation,
//        ITranslationService translationService,
//        IProgressService? progress = null
//        ) : base(progress)
//    {
//        _ffmpegService = ffmpegService;
//        _speechRecognitionService = speechRecognitionService;
//        _subtitleService = subtitleService;
//        _audioService = audioService;
//        _audioSeparationService = audioSeparation;
//        _translationService = translationService;
        
//    }

//    public async Task<WorkflowResult> ExecuteFullWorkflowAsync(WorkflowContext context)
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var result = new WorkflowResult();

//        try
//        {
//            progress?.Report("开始执行完整翻译工作流...");

//            #region 步骤1: 提取音频
//            var step1Result = await ExtractAudioAsync(context);
//            result.StepResults.Add(step1Result);
//            if (!step1Result.Success)
//            {
//                result.Success = false;
//                result.ErrorMessage = $"步骤1失败: {step1Result.ErrorMessage}";
//                return result;
//            }
//            #endregion

//            #region 步骤2: 分离音频
//            var step2Result = await SeparateAudioAsync(context);
//            result.StepResults.Add(step2Result);
//            if (!step2Result.Success)
//            {
//                result.Success = false;
//                result.ErrorMessage = $"步骤2失败: {step2Result.ErrorMessage}";
//                return result;
//            }
//            #endregion

//            #region 步骤3: 识别源字幕
//            var step3Result = await ParseSourceSRTAsync(context);
//            result.StepResults.Add(step3Result);
//            if (!step3Result.Success)
//            {
//                result.Success = false;
//                result.ErrorMessage = $"步骤3失败: {step3Result.ErrorMessage}";
//                return result;
//            }
//            #endregion

//            #region 步骤4: 源字幕分段
//            var step4Result = await SegmentSourceSRTAsync(context);
//            result.StepResults.Add(step4Result);
//            if (!step4Result.Success)
//            {
//                result.Success = false;
//                result.ErrorMessage = $"步骤4失败: {step4Result.ErrorMessage}";
//                return result;
//            }
//            #endregion

//            #region 步骤5: 翻译字幕
//            var step5Result = await TranslateSRTAsync(context, step4Result.Clips);
//            result.StepResults.Add(step5Result);
//            if (!step5Result.Success)
//            {
//                result.Success = false;
//                result.ErrorMessage = $"步骤5失败: {step5Result.ErrorMessage}";
//                return result;
//            }
//            #endregion

//            result.Success = true;
//            result.TotalDuration = stopwatch.Elapsed;
//            progress?.Report($"完整工作流完成！总耗时: {result.TotalDuration.TotalSeconds:F1}秒");

//            return result;
//        }
//        catch (Exception ex)
//        {
//            result.Success = false;
//            result.ErrorMessage = ex.Message;
//            result.TotalDuration = stopwatch.Elapsed;
//            return result;
//        }
//    }

//    #region 步骤1: 提取音频
//    private async Task<WorkflowStepResult> ExtractAudioAsync(WorkflowContext context)
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var result = new WorkflowStepResult { StepName = "提取音频" };

//        try
//        {
//            progress?.Title("步骤1/5: 正在提取音频...");

//            var audioPath = System.IO.Path.Combine(context.ProjectPath, "source_audio.wav");
//            var args = $"-i \"{context.SourceVideoPath}\" -vn -acodec pcm_s16le -ar 44100 -ac 2 -y \"{audioPath}\"";

//            await _ffmpegService.ExecuteCommandAsync(args);

//            context.SourceAudioPath = audioPath;
//            result.Success = true;
//            result.OutputPath = audioPath;
//            result.Duration = stopwatch.Elapsed;

//            progress?.Report($"步骤1/5: 音频提取完成，耗时 {result.Duration.TotalSeconds:F1}秒");

//            return result;
//        }
//        catch (Exception ex)
//        {
//            result.Success = false;
//            result.ErrorMessage = ex.Message;
//            result.Duration = stopwatch.Elapsed;
//            return result;
//        }
//    }
//    #endregion

//    #region 步骤2: 分离音频
//    private async Task<WorkflowStepResult> SeparateAudioAsync(WorkflowContext context)
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var result = new WorkflowStepResult { StepName = "分离音频" };

//        try
//        {
//            progress?.Title("步骤2/5: 正在进行音频分离...");

//            var audioTestDir = System.IO.Path.Combine(context.ProjectPath, "audio_test");
//            System.IO.Directory.CreateDirectory(audioTestDir);

//            var separationResult = await _audioSeparationService.SeparateVocalAndBackgroundAsync(
//                context.SourceAudioPath,
//                audioTestDir);

//            if (!separationResult.Success)
//            {
//                throw new Exception(separationResult.ErrorMessage ?? "音频分离失败");
//            }

//            context.SourceVocalAudioPath = separationResult.VocalAudioPath;
//            context.SourceBackgroundAudioPath = separationResult.BackgroundAudioPath;

//            result.Success = true;
//            result.OutputPath = separationResult.VocalAudioPath;
//            result.Duration = stopwatch.Elapsed;

//            progress?.Report($"步骤2/5: 音频分离完成，耗时 {result.Duration.TotalSeconds:F1}秒");

//            return result;
//        }
//        catch (Exception ex)
//        {
//            result.Success = false;
//            result.ErrorMessage = ex.Message;
//            result.Duration = stopwatch.Elapsed;
//            return result;
//        }
//    }
//    #endregion

//    #region 步骤3: 识别源字幕
//    private async Task<WorkflowStepResult> ParseSourceSRTAsync(WorkflowContext context)
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var result = new WorkflowStepResult { StepName = "识别源字幕" };

//        try
//        {
//            progress?.Title("步骤3/5: 正在识别源字幕...");

//            var subtitlePath = System.IO.Path.Combine(context.ProjectPath, "source_subtitle.srt");

//            await _speechRecognitionService.RecognizeAudioAsync(
//                context.SourceAudioPath,
//                subtitlePath,
//                "en"
//                );

//            context.SourceSubtitlePath = subtitlePath;
//            result.Success = true;
//            result.OutputPath = subtitlePath;
//            result.Duration = stopwatch.Elapsed;

//            progress?.Report($"步骤3/5: 字幕识别完成，耗时 {result.Duration.TotalSeconds:F1}秒");

//            return result;
//        }
//        catch (Exception ex)
//        {
//            result.Success = false;
//            result.ErrorMessage = ex.Message;
//            result.Duration = stopwatch.Elapsed;
//            return result;
//        }
//    }
//    #endregion

//    #region 步骤4: 源字幕分段
//    private async Task<SegmentWorkflowStepResult> SegmentSourceSRTAsync(WorkflowContext context)
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var result = new SegmentWorkflowStepResult { StepName = "源字幕分段" };

//        try
//        {
//            progress?.Title("步骤4/5: 正在进行源字幕分段...");

//            var segmentContext = new SegmentContext
//            {
//                SourceSubtitlePath = context.SourceSubtitlePath,
//                SourceAudioPath = context.SourceAudioPath,
//                SourceVocalAudioPath = context.SourceVocalAudioPath,
//                ProjectPath = context.ProjectPath
//            };

//            var segmentResult = await _segmentSourceSRTService.SegmentAsync(segmentContext);

//            if (!segmentResult.Success)
//            {
//                throw new Exception(segmentResult.ErrorMessage ?? "分段失败");
//            }

//            result.Success = true;
//            result.ProcessedCount = segmentResult.Clips.Count;
//            result.Duration = stopwatch.Elapsed;
//            result.Clips = segmentResult.Clips;

//            progress?.Success($"步骤4/5: 分段完成，共生成 {segmentResult.Clips.Count} 个片段，耗时 {result.Duration.TotalSeconds:F1}秒");

//            return result;
//        }
//        catch (Exception ex)
//        {
//            result.Success = false;
//            result.ErrorMessage = ex.Message;
//            result.Duration = stopwatch.Elapsed;
//            return result;
//        }
//    }
//    #endregion

//    #region 步骤5: 翻译字幕
//    private async Task<WorkflowStepResult> TranslateSRTAsync(WorkflowContext context, List<ClipInfo> clips)
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var result = new WorkflowStepResult { StepName = "翻译字幕" };

//        try
//        {
//            progress?.Title("步骤5/5: 正在翻译字幕...");

//            var translatedSubtitlePath = System.IO.Path.Combine(context.ProjectPath, "source_subtitle_zh.srt");

//            var subtitles = clips.Select(x => new SrtSubtitle
//            {
//                Index = x.Index,
//                StartTime = TimeSpan.FromSeconds(x.StartSeconds),
//                EndTime = TimeSpan.FromSeconds(x.EndSeconds),
//                Text = x.Text
//            }).ToList();

//            var translatedSubtitles = await _translationService.TranslateSubtitlesAsync(
//                subtitles,
//                TranslationApi.Google,batchSize:80);
            
//            await _subtitleService.SaveSrtAsync(translatedSubtitles, translatedSubtitlePath);

//            context.TranslatedSubtitlePath = translatedSubtitlePath;

//            result.Success = true;
//            result.ProcessedCount = translatedSubtitles.Count;
//            result.OutputPath = translatedSubtitlePath;
//            result.Duration = stopwatch.Elapsed;

//            progress?.Success($"步骤5/5: 翻译完成！共翻译 {translatedSubtitles.Count} 条字幕，耗时 {result.Duration.TotalSeconds:F1}秒");

//            return result;
//        }
//        catch (Exception ex)
//        {
//            result.Success = false;
//            result.ErrorMessage = ex.Message;
//            result.Duration = stopwatch.Elapsed;
//            return result;
//        }
//    }
//    #endregion
//}
