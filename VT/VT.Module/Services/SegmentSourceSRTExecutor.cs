using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VT.Module.BusinessObjects;

namespace VT.Module.Services;

//public interface ISegmentSourceSRTExecutor
//{
//    Task<int> ExecuteAsync(VT.Module.BusinessObjects.VideoProject videoProject, ISegmentProgressCallback? callback = null);
//}

//public class SegmentSourceSRTExecutor : ISegmentSourceSRTExecutor
//{
//    private readonly IServiceProvider _serviceProvider;

//    public SegmentSourceSRTExecutor(IServiceProvider serviceProvider)
//    {
//        _serviceProvider = serviceProvider;
//    }

//    public async Task<int> ExecuteAsync(VT.Module.BusinessObjects.VideoProject videoProject, ISegmentProgressCallback? callback = null)
//    {
//        callback ??= new SilentSegmentProgressCallback();

//        callback.ReportProgress("开始分段处理...");

//        callback.LogOperation("验证源文件", ("字幕", videoProject.SourceSubtitlePath), ("音频", videoProject.SourceAudioPath));

//        if (string.IsNullOrEmpty(videoProject.SourceSubtitlePath) || !File.Exists(videoProject.SourceSubtitlePath))
//        {
//            callback.OnSegmentFailed("字幕文件不存在");
//            throw new InvalidOperationException($"字幕文件不存在: {videoProject.SourceSubtitlePath}");
//        }

//        if (string.IsNullOrEmpty(videoProject.SourceAudioPath) || !File.Exists(videoProject.SourceAudioPath))
//        {
//            callback.OnSegmentFailed("音频文件不存在");
//            throw new InvalidOperationException($"音频文件不存在: {videoProject.SourceAudioPath}");
//        }

//        callback.ReportProgress("正在调用分段服务...");

//        var segmentService = _serviceProvider.GetRequiredService<ISegmentSourceSRTService>();

//        var context = new SegmentContext
//        {
//            SourceSubtitlePath = videoProject.SourceSubtitlePath,
//            SourceAudioPath = videoProject.SourceAudioPath,
//            SourceVocalAudioPath = videoProject.SourceVocalAudioPath,
//            ProjectPath = videoProject.ProjectPath,
//            Progress = new Progress<ProgressInfo>(p => 
//            {

//                if (!string.IsNullOrEmpty(p.Message))
//                {
//                    callback.ReportProgress(p.Message);
//                }
//            }),
//            //LogOperation = (operation, parameters) => callback.LogOperation(operation, parameters)
//        };

//        var result = await segmentService.SegmentAsync(context);

//        if (!result.Success)
//        {
//            callback.OnSegmentFailed(result.ErrorMessage ?? "分段失败");
//            throw new InvalidOperationException(result.ErrorMessage ?? "分段失败");
//        }

//        callback.LogOperation("创建片段对象", ("片段数量", result.Clips.Count));

//        callback.ReportProgress($"正在创建 {result.Clips.Count} 个片段对象...");

//        var objectSpaceProvider = _serviceProvider.GetRequiredService<XPObjectSpaceProvider>();
//        using var objectSpace = objectSpaceProvider.CreateObjectSpace();
        
//        var index = 0;

//        foreach (var clipInfo in result.Clips)
//        {
//            var timeLineClip = objectSpace.CreateObject<TimeLineClip>();
//            timeLineClip.VideoProject = videoProject;
//            timeLineClip.Index = index;

//            var srtClip = timeLineClip.SourceSRTClip;
//            srtClip.Index = clipInfo.Index;
//            srtClip.Start = TimeSpan.FromSeconds(clipInfo.StartSeconds);
//            srtClip.End = TimeSpan.FromSeconds(clipInfo.EndSeconds);
//            srtClip.Text = clipInfo.Text;            

//            if (File.Exists(clipInfo.AudioFilePath))
//            {
//                var audioClip = timeLineClip.SourceAudioClip;
//                audioClip.Index = clipInfo.Index;
//                audioClip.Start = TimeSpan.FromSeconds(clipInfo.StartSeconds);
//                audioClip.End = TimeSpan.FromSeconds(clipInfo.EndSeconds);
//                await audioClip.SetAudioFile(clipInfo.AudioFilePath);
//            }

//            videoProject.Clips.Add(timeLineClip);
//            index++;

//            if (index % 10 == 0)
//            {
//                callback.ReportProgress(index, result.Clips.Count, $"已创建 {index}/{result.Clips.Count} 个片段");
//                objectSpace.CommitChanges();
//            }
//        }

//        objectSpace.CommitChanges();

//        callback.OnSegmentCompleted(result.Clips.Count);

//        return result.Clips.Count;
//    }
//}
