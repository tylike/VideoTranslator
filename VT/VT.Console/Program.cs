using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VideoTranslator.Utils;
using YoutubeDLSharp.Metadata;
using VT.Console.Services;
using VT.Module.BusinessObjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using VT.Module;
using System.Windows;
using VideoEditor.Windows;
using VideoEditor;
using VideoEditor.Models;
using System.IO;
using System.Linq;

namespace VideoTranslator;

class Program
{
    #region 常量

    private const string DownloadUrlParam = "--download-url";

    #endregion

    [STAThread]
    static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string[] args)
    {
        #region 解析命令行参数

        var downloadUrl = ParseDownloadUrl(args);
        if (!string.IsNullOrEmpty(downloadUrl))
        {
            RunCompleteWorkflowAsync(downloadUrl).GetAwaiter().GetResult();
            return;
        }

        #endregion

        #region 初始化WPF应用程序

        var app = new System.Windows.Application();

        #endregion

        #region 初始化日志

        var logDirectory = @"d:\VideoTranslator\logs";
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(logDirectory, $"console_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        var logger = Log.ForContext<Program>();
        logger.Information("控制台应用程序启动");

        #endregion

        #region 初始化服务容器

        var serviceProvider = ServiceHelper.InitializeServices(services =>
        {
            services.AddSingleton<IProgressService, ConsoleProgressService>();
        });
        logger.Information("服务容器初始化完成");

        #endregion

        #region 初始化XPO

        var xpoObjectSpaceProvider = ServiceHelper.GetXpoObjectSpaceProvider();
        logger.Information("XPO初始化完成");

        #endregion

        #region 示例：处理项目音频

        try
        {
            logger.Information("开始分割方案生成测试");
            TestSplitSchemeGeneration(serviceProvider, logger);

            logger.Information("开始VAD检测测试");
            Task.Run(() => TestVadDetectionAsync(serviceProvider, logger).GetAwaiter().GetResult()).GetAwaiter().GetResult();

            logger.Information("开始VideoVadWorkflowService测试");
            Task.Run(() => TestVideoVadWorkflowServiceAsync(serviceProvider, logger).GetAwaiter().GetResult()).GetAwaiter().GetResult();

            logger.Information("开始FFmpeg命令生成测试");
            TestFFmpegCommandGeneration(serviceProvider, logger);

            logger.Information("开始FFmpeg进度测试");
            Task.Run(() => TestFFmpegProgressAsync(serviceProvider, logger).GetAwaiter().GetResult()).GetAwaiter().GetResult();

            logger.Information("开始GetVideoStreamDetails方法测试");
            await TestGetVideoStreamDetailsAsync(serviceProvider, logger);

            logger.Information("开始SeparateVideoStreams方法测试");
            await TestSeparateVideoStreamsAsync(serviceProvider, logger);

            logger.Information("测试完成，跳过WPF窗口启动");
            Console.WriteLine("\n所有测试已完成，按任意键退出...");
            Console.ReadKey();
            goto Exit;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "处理项目音频失败");
            Console.WriteLine($"处理失败: {ex.Message}");
        }

    #endregion

    Exit:
        logger.Information("控制台应用程序退出");
        Log.CloseAndFlush();
    }

    #region 命令行参数解析

    private static string? ParseDownloadUrl(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(DownloadUrlParam, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1].Trim();
            }
        }

        return null;
    }

    #endregion

    #region 视频下载功能

    private static async Task DownloadVideoByUrl(string url)
    {
        #region 初始化日志

        var logDirectory = @"d:\VideoTranslator\logs";
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(logDirectory, $"console_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        var logger = Log.ForContext<Program>();
        logger.Information("开始下载视频: {Url}", url);

        #endregion

        try
        {
            #region 获取视频信息

            Console.WriteLine($"正在获取视频信息: {url}");
            var videoInfo = await YtDlpService.GetVideoInfoAsync(url, new Progress<string>(message =>
            {
                Console.WriteLine(message);
                logger.Debug("下载进度: {Message}", message);
            }));

            logger.Information("成功获取视频信息: 标题={Title}, ID={Id}, 时长={Duration}",
                videoInfo.Title, videoInfo.ID, videoInfo.GetFormattedDuration());

            Console.WriteLine($"\n视频信息:");
            Console.WriteLine($"  标题: {videoInfo.Title}");
            Console.WriteLine($"  上传者: {videoInfo.Uploader}");
            Console.WriteLine($"  时长: {videoInfo.GetFormattedDuration()}");
            Console.WriteLine($"  观看次数: {videoInfo.GetFormattedViewCount()}");
            Console.WriteLine($"  点赞数: {videoInfo.GetFormattedLikeCount()}");

            #endregion

            #region 选择视频格式

            var videoFormats = videoInfo.GetVideoFormats();
            if (videoFormats.Count == 0)
            {
                logger.Warning("未找到可用的视频格式");
                Console.WriteLine("未找到可用的视频格式");
                return;
            }

            Console.WriteLine($"\n可用视频格式:");
            for (int i = 0; i < Math.Min(5, videoFormats.Count); i++)
            {
                var format = videoFormats[i];
                Console.WriteLine($"  [{i}] {format.Resolution} | {format.Extension} | {format.GetFormattedFileSize()} | {format.VideoCodec}");
            }

            var selectedFormatIndex = 0;
            var bestFormat = videoFormats
                .OrderByDescending(f => f.GetFileSizeBytes())
                .FirstOrDefault(f => !string.IsNullOrEmpty(f.VideoCodec) && f.VideoCodec != "none");

            if (bestFormat != null)
            {
                selectedFormatIndex = videoFormats.IndexOf(bestFormat);
                Console.WriteLine($"\n自动选择最佳格式: [{selectedFormatIndex}] {bestFormat.Resolution} | {bestFormat.Extension} | {bestFormat.GetFormattedFileSize()}");
            }

            #endregion

            #region 下载视频

            var outputDirectory = @"d:\VideoTranslator\downloads";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var sanitizedTitle = SanitizeFileName(videoInfo.Title);
            var outputPath = Path.Combine(outputDirectory, $"{sanitizedTitle}.{bestFormat?.Extension ?? "mp4"}");

            Console.WriteLine($"\n开始下载视频到: {outputPath}");
            logger.Information("开始下载视频: 输出路径={OutputPath}, 格式ID={FormatId}", outputPath, bestFormat?.FormatId);

            var downloadedPath = await YtDlpService.DownloadVideoAsync(
                url,
                outputPath,
                bestFormat?.FormatId,
                new Progress<string>(message =>
                {
                    Console.WriteLine(message);
                    logger.Debug("下载进度: {Message}", message);
                }));

            logger.Information("视频下载完成: {Path}", downloadedPath);
            Console.WriteLine($"\n下载完成: {downloadedPath}");

            #endregion
        }
        catch (Exception ex)
        {
            logger.Error(ex, "下载视频失败: {Url}", url);
            Console.WriteLine($"\n下载失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars));
    }

    #endregion

    #region 项目音频处理

    private static async Task ProcessProjectAudioAsync(VT.Module.BusinessObjects.VideoProject project, Serilog.ILogger logger)
    {
        await project.ProcessProjectAudio(regenerate: false);
    }

    #endregion

    #region VAD检测测试

    private static async Task TestVadDetectionAsync(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            logger.Information("开始VAD检测测试");

            var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
            var progressService = serviceProvider.GetRequiredService<IProgressService>();

            var vadService = new WhisperRecognitionService(
                translateToEnglish: false,
                usePostProcessing: false,
                autoAdjustTimings: false
                );

            var audioPath = @"D:\VideoTranslator\video.downloads\【官方双语】拉普拉斯变换（三）：为什么拉普拉斯变换如此有用？.f30280.m4a";

            if (!File.Exists(audioPath))
            {
                logger.Error("测试音频文件不存在: {AudioPath}", audioPath);
                Console.WriteLine($"测试音频文件不存在: {audioPath}");
                return;
            }

            Console.WriteLine($"\n开始VAD检测测试...");
            Console.WriteLine($"音频文件: {audioPath}");

            var result = await vadService.DetectVadSegmentsAsync(
                audioPath,
                threshold: 0.5m,
                minSpeechDurationMs: 250,
                minSilenceDurationMs: 100);

            Console.WriteLine($"\nVAD检测结果:");
            Console.WriteLine($"音频总时长: {result.AudioDuration:F2}秒");
            Console.WriteLine($"总片段数: {result.Segments.Count}");
            Console.WriteLine($"语音片段数: {result.SpeechSegmentCount}");
            Console.WriteLine($"静音片段数: {result.SilenceSegmentCount}");
            Console.WriteLine($"语音总时长: {result.TotalSpeechDuration:F2}秒");
            Console.WriteLine($"静音总时长: {result.TotalSilenceDuration:F2}秒");

            Console.WriteLine($"\n片段详情:");
            Console.WriteLine($"{"索引",-5} {"类型",-8} {"开始时间",-12} {"结束时间",-12} {"时长(秒)",-10}");
            Console.WriteLine(new string('-', 50));

            foreach (var segment in result.Segments)
            {
                var type = segment.IsSpeech ? "语音" : "静音";
                Console.WriteLine($"{segment.Index,-5} {type,-8} {segment.Start,12:F2} {segment.End,12:F2} {segment.Duration,10:F2}");
            }

            logger.Information("VAD检测测试完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "VAD检测测试失败");
            Console.WriteLine($"\nVAD检测测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    #endregion

    #region VideoVadWorkflowService测试

    private static async Task TestVideoVadWorkflowServiceAsync(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            logger.Information("开始VideoVadWorkflowService测试");

            var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
            var progressService = serviceProvider.GetRequiredService<IProgressService>();
            var whisperRecognitionService = serviceProvider.GetRequiredService<WhisperRecognitionService>();

            var vadWorkflowService = new VideoVadWorkflowService(whisperRecognitionService);

            var videoPath = @"d:\video.download\11\【4K】无耻之徒1~11季 p01 p (1)_30016.mp4";
            var audioPath = @"d:\video.download\11\【4K】无耻之徒1~11季 p01 p (1)_audio_30216.mp3";

            if (!File.Exists(videoPath))
            {
                logger.Error("测试视频文件不存在: {VideoPath}", videoPath);
                Console.WriteLine($"测试视频文件不存在: {videoPath}");
                return;
            }

            Console.WriteLine($"\n开始VideoVadWorkflowService测试...");
            Console.WriteLine($"视频文件: {videoPath}");
            Console.WriteLine($"音频文件: {audioPath}");

            var result = await vadWorkflowService.ExecuteVadWorkflowWithAudioAsync(
                audioPath,
                vadThreshold: 0.5m,
                minSpeechDurationMs: 250,
                minSilenceDurationMs: 100,
                splitVideo: false);

            Console.WriteLine($"\nVAD工作流结果:");
            Console.WriteLine($"成功: {result.Success}");
            Console.WriteLine($"视频路径: {result.VideoPath}");
            Console.WriteLine($"音频路径: {result.AudioPath}");
            Console.WriteLine($"VAD结果: {result.VadResult != null}");
            Console.WriteLine($"分割文件数: {result.SplitFiles.Count}");
            Console.WriteLine($"输出文件夹: {result.OutputFolder}");
            Console.WriteLine($"VAD信息路径: {result.VadInfoPath}");
            Console.WriteLine($"总耗时: {result.TotalDuration.TotalSeconds:F2}秒");

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"错误信息: {result.ErrorMessage}");
            }

            if (result.VadResult != null)
            {
                Console.WriteLine($"\nVAD检测结果:");
                Console.WriteLine($"音频总时长: {result.VadResult.AudioDuration:F2}秒");
                Console.WriteLine($"语音片段数: {result.VadResult.SpeechSegmentCount}");
                Console.WriteLine($"静音片段数: {result.VadResult.SilenceSegmentCount}");
                Console.WriteLine($"语音总时长: {result.VadResult.TotalSpeechDuration:F2}秒");
                Console.WriteLine($"静音总时长: {result.VadResult.TotalSilenceDuration:F2}秒");
            }

            if (result.SplitFiles.Count > 0)
            {
                Console.WriteLine($"\n分割文件列表:");
                foreach (var file in result.SplitFiles)
                {
                    Console.WriteLine($"  - {file}");
                }
            }

            logger.Information("VideoVadWorkflowService测试完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "VideoVadWorkflowService测试失败");
            Console.WriteLine($"\nVideoVadWorkflowService测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    #endregion

    #region 分割方案生成测试

    private static void TestSplitSchemeGeneration(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            logger.Information("开始分割方案生成测试");

            Console.WriteLine($"\n开始分割方案生成测试...");
            Console.WriteLine($"=".PadRight(50, '='));

            var vadResult = CreateMockVadResult();
            Console.WriteLine($"\n模拟VAD检测结果:");
            Console.WriteLine($"音频总时长: {vadResult.AudioDuration:F2}秒 ({vadResult.AudioDuration / 60:F1}分钟)");
            Console.WriteLine($"语音片段数: {vadResult.SpeechSegmentCount}");
            Console.WriteLine($"静音片段数: {vadResult.SilenceSegmentCount}");
            Console.WriteLine($"语音总时长: {vadResult.TotalSpeechDuration:F2}秒");
            Console.WriteLine($"静音总时长: {vadResult.TotalSilenceDuration:F2}秒");

            Console.WriteLine($"\n静音片段详情（前20个）:");
            Console.WriteLine($"{"索引",-5} {"类型",-8} {"开始时间",-12} {"结束时间",-12} {"时长(秒)",-10}");
            Console.WriteLine(new string('-', 50));

            var silenceSegments = vadResult.Segments.Where(s => !s.IsSpeech).OrderByDescending(s => s.Duration).Take(20).ToList();
            foreach (var segment in silenceSegments)
            {
                var type = segment.IsSpeech ? "语音" : "静音";
                Console.WriteLine($"{segment.Index,-5} {type,-8} {segment.Start,12:F2} {segment.End,12:F2} {segment.Duration,10:F2}");
            }

            Console.WriteLine($"\n生成分割方案...");
            Console.WriteLine($"=".PadRight(50, '='));

            var splitter = new VideoEditor.Services.AudioSplitter();
            var schemes = splitter.GenerateSplitSchemes(vadResult);

            Console.WriteLine($"\n共生成 {schemes.Count} 个分割方案:");
            Console.WriteLine($"=".PadRight(50, '='));

            foreach (var scheme in schemes)
            {
                Console.WriteLine($"\n方案{scheme.Index}: {scheme.Description}");
                Console.WriteLine($"  分割点数: {scheme.SplitPoints.Count}");
                Console.WriteLine($"  分段数: {scheme.SegmentCount}");
                Console.WriteLine($"  平均时长: {scheme.AverageDuration:F2}秒 ({scheme.AverageDuration / 60:F1}分钟)");
                Console.WriteLine($"  最小静音时长: {scheme.MinSilenceDuration:F2}秒");

                Console.WriteLine($"\n  分割点详情:");
                Console.WriteLine($"    {"序号",-6} {"时间(秒)",-12} {"时长(分钟)",-12}");
                Console.WriteLine(new string('-', 35));

                var currentTime = 0m;
                for (int i = 0; i < scheme.SplitPoints.Count; i++)
                {
                    var segmentDuration = scheme.SplitPoints[i] - currentTime;
                    Console.WriteLine($"    {i + 1,-6} {scheme.SplitPoints[i],12:F2} {segmentDuration / 60,12:F2}");
                    currentTime = scheme.SplitPoints[i];
                }

                var lastSegmentDuration = vadResult.AudioDuration - currentTime;
                Console.WriteLine($"    {scheme.SplitPoints.Count + 1,-6} {vadResult.AudioDuration,12:F2} {lastSegmentDuration / 60,12:F2}");
            }

            Console.WriteLine($"\n=".PadRight(50, '='));
            Console.WriteLine($"\n分割方案验证:");

            foreach (var scheme in schemes)
            {
                var isValid = ValidateSplitScheme(vadResult, scheme);
                var status = isValid ? "✓ 通过" : "✗ 失败";
                Console.WriteLine($"  方案{scheme.Index}: {status}");
            }

            logger.Information("分割方案生成测试完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "分割方案生成测试失败");
            Console.WriteLine($"\n分割方案生成测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    private static VadDetectionResult CreateMockVadResult()
    {
        var audioDuration = 1800m;
        var segments = new List<VideoTranslator.Models.VadSegment>();

        var currentTime = 0m;

        for (int i = 0; i < 20; i++)
        {
            var speechDuration = 30m + (i % 5) * 10m;
            var silenceDuration = 5m + (i % 3) * 2m;

            segments.Add(new VideoTranslator.Models.VadSegment
            {
                Index = i * 2,
                Start = currentTime,
                End = currentTime + speechDuration,
                Duration = speechDuration,
                IsSpeech = true
            });

            currentTime += speechDuration;

            segments.Add(new VideoTranslator.Models.VadSegment
            {
                Index = i * 2 + 1,
                Start = currentTime,
                End = currentTime + silenceDuration,
                Duration = silenceDuration,
                IsSpeech = false
            });

            currentTime += silenceDuration;

            if (currentTime >= audioDuration)
            {
                break;
            }
        }

        if (currentTime < audioDuration)
        {
            segments.Add(new VideoTranslator.Models.VadSegment
            {
                Index = segments.Count,
                Start = currentTime,
                End = audioDuration,
                Duration = audioDuration - currentTime,
                IsSpeech = true
            });
        }

        var speechSegments = segments.Where(s => s.IsSpeech).ToList();
        var silenceSegments = segments.Where(s => !s.IsSpeech).ToList();

        return new VadDetectionResult
        {
            AudioPath = "mock_audio.mp3",
            AudioDuration = audioDuration,
            Segments = segments,
            SpeechSegmentCount = speechSegments.Count,
            SilenceSegmentCount = silenceSegments.Count,
            TotalSpeechDuration = speechSegments.Sum(s => s.Duration),
            TotalSilenceDuration = silenceSegments.Sum(s => s.Duration)
        };
    }

    private static bool ValidateSplitScheme(VadDetectionResult vadResult, SplitScheme scheme)
    {
        var currentTime = 0m;
        var minDuration = 180m;
        var maxDuration = 300m;

        for (int i = 0; i < scheme.SplitPoints.Count; i++)
        {
            var segmentDuration = scheme.SplitPoints[i] - currentTime;

            if (segmentDuration < minDuration || segmentDuration > maxDuration)
            {
                Console.WriteLine($"    ✗ 分段{i + 1}时长不符合要求: {segmentDuration / 60:F1}分钟 (要求: 3-5分钟)");
                return false;
            }

            currentTime = scheme.SplitPoints[i];
        }

        var lastSegmentDuration = vadResult.AudioDuration - currentTime;

        if (lastSegmentDuration < minDuration || lastSegmentDuration > maxDuration)
        {
            Console.WriteLine($"    ✗ 最后分段时长不符合要求: {lastSegmentDuration / 60:F1}分钟 (要求: 3-5分钟)");
            return false;
        }

        return true;
    }

    #endregion

    #region 完整工作流程（模拟WPF的VideoDownloadWindow功能）

    private static async Task RunCompleteWorkflowAsync(string url)
    {
        #region 初始化日志

        var logDirectory = @"d:\VideoTranslator\logs";
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(logDirectory, $"workflow_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        var logger = Log.ForContext<Program>();
        logger.Information("========================================");
        logger.Information("开始完整工作流程: {Url}", url);
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║               视频下载与VAD检测完整流程                        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        #endregion

        #region 初始化服务

        var serviceProvider = ServiceHelper.InitializeServices(services =>
        {
            services.AddSingleton<IProgressService, ConsoleProgressService>();
        });

        var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
        var progressService = serviceProvider.GetRequiredService<IProgressService>();
        var vadWorkflowService = serviceProvider.GetRequiredService<VideoTranslator.Services.VideoVadWorkflowService>();

        #endregion

        try
        {
            #region 步骤1: 获取视频信息

            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    步骤1: 获取视频信息                        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            VideoData? videoInfo;
            try
            {
                Console.WriteLine($"正在获取视频信息: {url}");
                videoInfo = await YtDlpService.GetVideoInfoAsync(url, new Progress<string>(message =>
                {
                    Console.WriteLine($"  {message}");
                }));

                logger.Information("成功获取视频信息: 标题={Title}, ID={Id}, 时长={Duration}",
                    videoInfo.Title, videoInfo.ID, videoInfo.GetFormattedDuration());

                Console.WriteLine();
                Console.WriteLine("  ✓ 视频信息获取成功:");
                Console.WriteLine($"    标题: {videoInfo.Title}");
                Console.WriteLine($"    上传者: {videoInfo.Uploader}");
                Console.WriteLine($"    时长: {videoInfo.GetFormattedDuration()}");
                Console.WriteLine($"    观看次数: {videoInfo.GetFormattedViewCount()}");
                Console.WriteLine($"    点赞数: {videoInfo.GetFormattedLikeCount()}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "获取视频信息失败");
                Console.WriteLine();
                Console.WriteLine($"  ✗ 获取视频信息失败: {ex.Message}");
                return;
            }

            #endregion

            #region 步骤2: 选择并下载音频

            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  步骤2: 选择并下载音频                        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            if (videoInfo.GetAudioFormats().Count == 0)
            {
                logger.Warning("未找到可用的音频格式");
                Console.WriteLine("  ✗ 未找到可用的音频格式");
                return;
            }

            Console.WriteLine($"  找到 {videoInfo.GetAudioFormats().Count} 个音频格式:");
            Console.WriteLine();

            var audioFormats = videoInfo.GetAudioFormats()
                .OrderByDescending(f => f.GetFileSizeBytes())
                .Take(5)
                .ToList();

            for (int i = 0; i < audioFormats.Count; i++)
            {
                var format = audioFormats[i];
                Console.WriteLine($"    [{i}] {format.Extension} | {format.GetFormattedFileSize()} | {format.AudioCodec}");
            }

            var selectedAudioIndex = 0;
            var bestAudioFormat = audioFormats.FirstOrDefault();
            if (bestAudioFormat != null)
            {
                Console.WriteLine();
                Console.WriteLine($"  ✓ 自动选择最佳音频格式: [{selectedAudioIndex}] {bestAudioFormat.Extension} | {bestAudioFormat.GetFormattedFileSize()}");
            }

            var downloadFolder = CreateDownloadFolder();
            var sanitizedTitle = SanitizeFileName(videoInfo.Title);
            var audioPath = Path.Combine(downloadFolder, $"{sanitizedTitle}_audio.{bestAudioFormat?.Extension ?? "mp3"}");

            Console.WriteLine();
            Console.WriteLine($"  开始下载音频...");
            Console.WriteLine($"  保存位置: {audioPath}");

            string? downloadedAudioPath;
            try
            {
                downloadedAudioPath = await YtDlpService.DownloadAudioAsync(
                    url,
                    audioPath,
                    bestAudioFormat?.FormatId,
                    new Progress<string>(message =>
                    {
                        Console.WriteLine($"    {message}");
                    }));

                logger.Information("音频下载完成: {Path}", downloadedAudioPath);

                downloadedAudioPath = FindAudioFile(downloadFolder, sanitizedTitle);

                Console.WriteLine();
                Console.WriteLine($"  ✓ 音频下载完成: {downloadedAudioPath}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "下载音频失败");
                Console.WriteLine();
                Console.WriteLine($"  ✗ 下载音频失败: {ex.Message}");
                return;
            }

            #endregion

            #region 步骤3: 检测音频时长并执行VAD检测

            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              步骤3: 执行VAD检测                               ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                Console.WriteLine($"  正在检测音频时长...");
                var audioDuration = await vadWorkflowService.GetAudioDurationAsync(downloadedAudioPath);
                Console.WriteLine($"  音频时长: {audioDuration:F2}秒 ({audioDuration / 60:F1}分钟)");
                logger.Information("音频时长: {Duration}秒", audioDuration);

                VadDetectionResult? vadResult;
                if (audioDuration > 300)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  ℹ 音频时长大于5分钟，自动执行VAD检测...");

                    var threshold = 0.5m;
                    var minSpeechDuration = 250;
                    var minSilenceDuration = 100;

                    Console.WriteLine($"  VAD参数: 阈值={threshold}, 最小语音时长={minSpeechDuration}ms, 最小静音时长={minSilenceDuration}ms");
                    Console.WriteLine();

                    Console.WriteLine($"  正在执行VAD检测，请稍候...");

                    var vadService = new WhisperRecognitionService(                        
                        translateToEnglish: false,
                        usePostProcessing: false,
                        autoAdjustTimings: false);

                    vadResult = await vadService.DetectVadSegmentsAsync(
                        downloadedAudioPath,
                        threshold: threshold,
                        minSpeechDurationMs: minSpeechDuration,
                        minSilenceDurationMs: minSilenceDuration);

                    logger.Information("VAD检测完成: 语音段={SpeechCount}, 静音段={SilenceCount}",
                        vadResult.SpeechSegmentCount, vadResult.SilenceSegmentCount);
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine($"  ℹ 音频时长小于5分钟，跳过VAD检测（模拟WPF中的自动逻辑）");

                    var mockVadResult = CreateMockVadResult(audioDuration);
                    vadResult = mockVadResult;
                }

                Console.WriteLine();
                Console.WriteLine("  ✓ VAD检测结果:");
                Console.WriteLine($"    音频总时长: {vadResult.AudioDuration:F2}秒 ({vadResult.AudioDuration / 60:F1}分钟)");
                Console.WriteLine($"    语音片段数: {vadResult.SpeechSegmentCount}");
                Console.WriteLine($"    静音片段数: {vadResult.SilenceSegmentCount}");
                Console.WriteLine($"    语音总时长: {vadResult.TotalSpeechDuration:F2}秒 ({vadResult.TotalSpeechDuration / 60:F1}分钟)");
                Console.WriteLine($"    静音总时长: {vadResult.TotalSilenceDuration:F2}秒 ({vadResult.TotalSilenceDuration / 60:F1}分钟)");

                Console.WriteLine();
                Console.WriteLine("  片段详情（前10个）:");
                Console.WriteLine($"    {"索引",-5} {"类型",-8} {"开始时间",-12} {"结束时间",-12} {"时长(秒)",-10}");
                Console.WriteLine(new string('-', 50));

                foreach (var segment in vadResult.Segments.Take(10))
                {
                    var type = segment.IsSpeech ? "语音" : "静音";
                    Console.WriteLine($"    {segment.Index,-5} {type,-8} {segment.Start,12:F2} {segment.End,12:F2} {segment.Duration,10:F2}");
                }

                if (vadResult.Segments.Count > 10)
                {
                    Console.WriteLine($"    ... 共 {vadResult.Segments.Count} 个片段");
                }

                #endregion

                #region 步骤4: 生成分割方案

                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                步骤4: 生成分割方案建议                         ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine();

                var splitter = new VideoEditor.Services.AudioSplitter();
                var schemes = splitter.GenerateSplitSchemes(vadResult);

                Console.WriteLine($"  ✓ 生成了 {schemes.Count} 个分割方案:");
                Console.WriteLine();

                foreach (var scheme in schemes)
                {
                    Console.WriteLine($"  ┌─────────────────────────────────────────────────────────────────┐");
                    Console.WriteLine($"  │ 方案{scheme.Index}: {scheme.Description,-55} │");
                    Console.WriteLine($"  ├─────────────────────────────────────────────────────────────────┤");
                    Console.WriteLine($"  │  分割点数: {scheme.SplitPoints.Count,-5}  分段数: {scheme.SegmentCount,-5}  平均时长: {scheme.AverageDuration / 60:F1}分钟 │");
                    Console.WriteLine($"  │  最小静音时长: {scheme.MinSilenceDuration:F2}秒                                     │");
                    Console.WriteLine($"  ├─────────────────────────────────────────────────────────────────┤");
                    Console.WriteLine($"  │  分割点详情:                                                   │");
                    Console.WriteLine($"  │    {"序号",-6} {"时间(秒)",-12} {"时长(分钟)",-12}                               │");
                    Console.WriteLine($"  │    {"".PadRight(40, '-')}                                         │");

                    var currentTime = 0m;
                    for (int i = 0; i < scheme.SplitPoints.Count; i++)
                    {
                        var segmentDuration = scheme.SplitPoints[i] - currentTime;
                        Console.WriteLine($"  │    {i + 1,-6} {scheme.SplitPoints[i],12:F2} {segmentDuration / 60,12:F1}                               │");
                        currentTime = scheme.SplitPoints[i];
                    }

                    var lastSegmentDuration = vadResult.AudioDuration - currentTime;
                    Console.WriteLine($"  │    {scheme.SplitPoints.Count + 1,-6} {vadResult.AudioDuration,12:F2} {lastSegmentDuration / 60,12:F1}                               │");
                    Console.WriteLine($"  └─────────────────────────────────────────────────────────────────┘");
                    Console.WriteLine();
                }

                #endregion

                #region 执行分割（自动选择最优方案）

                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    步骤5: 执行分割                           ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine();

                var selectedScheme = FindOptimalScheme(schemes, vadResult);

                if (selectedScheme == null || selectedScheme.SplitPoints.Count == 0)
                {
                    Console.WriteLine("  ✗ 没有可执行的分割方案");
                }
                else
                {
                    var maxSegmentDuration = GetMaxSegmentDuration(selectedScheme, vadResult);
                    Console.WriteLine($"  ✓ 自动选择最优方案: 方案{selectedScheme.Index}");
                    Console.WriteLine($"    分割点数量: {selectedScheme.SplitPoints.Count}");
                    Console.WriteLine($"    最大片段时长: {maxSegmentDuration / 60:F1}分钟");

                    if (maxSegmentDuration > 300)
                    {
                        Console.WriteLine($"  ⚠ 警告: 最大片段时长超过5分钟");
                    }

                    Console.WriteLine();

                    var splitFiles = vadWorkflowService.SplitVideoByPoints(
                        downloadedAudioPath,
                        selectedScheme.SplitPoints,
                        downloadFolder);

                    logger.Information("音频分割完成: {Count} 个文件", splitFiles.Count);
                    Console.WriteLine($"  ✓ 分割完成，生成 {splitFiles.Count} 个音频片段:");

                    foreach (var file in splitFiles)
                    {
                        Console.WriteLine($"    - {Path.GetFileName(file)}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("  ℹ 请使用以下命令验证分割结果:");
                    foreach (var file in splitFiles)
                    {
                        Console.WriteLine($"    ffmpeg -i \"{file}\" -f null -");
                    }
                }

                #endregion

                #region 保存VAD信息

                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                      保存VAD信息                                ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine();

                var vadInfoPath = Path.Combine(downloadFolder, "vad_info.json");

                vadWorkflowService = new VideoTranslator.Services.VideoVadWorkflowService(null!);

                vadWorkflowService.SaveVadInfo(vadInfoPath, vadResult, new List<string>());
                logger.Information("VAD信息已保存: {VadInfoPath}", vadInfoPath);

                Console.WriteLine($"  ✓ VAD信息已保存: {vadInfoPath}");

                #endregion

                #region 流程完成

                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    流程完成总结                                ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine($"  下载文件夹: {downloadFolder}");
                Console.WriteLine($"  音频文件: {Path.GetFileName(downloadedAudioPath)}");
                Console.WriteLine($"  VAD结果: {vadResult.Segments.Count} 个片段");
                Console.WriteLine($"  分割方案: {schemes.Count} 个");
                Console.WriteLine($"  已执行方案: 方案{selectedScheme?.Index ?? 1}");
                Console.WriteLine();
                Console.WriteLine($"  ✓ 已自动选择最优方案（所有片段≤5分钟）");
                Console.WriteLine($"  ℹ 详细日志已保存到: {logFilePath}");
                Console.WriteLine();
                logger.Information("完整工作流程完成");

                #endregion

            }
            catch (Exception ex)
            {
                logger.Error(ex, "VAD检测失败");
                Console.WriteLine();
                Console.WriteLine($"  ✗ VAD检测失败: {ex.Message}");
                Console.WriteLine($"  错误详情: {ex.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "完整工作流程失败");
            Console.WriteLine();
            Console.WriteLine($"  ✗ 流程执行失败: {ex.Message}");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static string CreateDownloadFolder()
    {
        var downloadFolder = @"d:\video.download\workflow";
        if (!Directory.Exists(downloadFolder))
        {
            Directory.CreateDirectory(downloadFolder);
        }
        return downloadFolder;
    }

    private static string FindAudioFile(string folder, string baseName)
    {
        var files = Directory.GetFiles(folder, $"{baseName}_audio.*");
        return files.FirstOrDefault() ?? Directory.GetFiles(folder, "*_audio.*").FirstOrDefault() ?? "";
    }

    private static VadDetectionResult CreateMockVadResult(decimal audioDuration = 600)
    {
        var segments = new List<VideoTranslator.Models.VadSegment>();
        var random = new Random(42);
        var currentTime = 0m;

        while (currentTime < audioDuration)
        {
            var isSpeech = random.NextDouble() > 0.3;
            var duration = isSpeech
                ? (decimal)(random.NextDouble() * 20 + 5)
                : (decimal)(random.NextDouble() * 3 + 0.5);

            duration = Math.Round(duration, 2);

            if (currentTime + duration > audioDuration)
            {
                duration = Math.Round(audioDuration - currentTime, 2);
            }

            segments.Add(new VideoTranslator.Models.VadSegment
            {
                Index = segments.Count,
                Start = Math.Round(currentTime, 2),
                End = Math.Round(currentTime + duration, 2),
                Duration = duration,
                IsSpeech = isSpeech
            });

            currentTime += duration;
        }

        if (currentTime < audioDuration)
        {
            segments.Add(new VideoTranslator.Models.VadSegment
            {
                Index = segments.Count,
                Start = Math.Round(currentTime, 2),
                End = audioDuration,
                Duration = Math.Round(audioDuration - currentTime, 2),
                IsSpeech = true
            });
        }

        var speechSegments = segments.Where(s => s.IsSpeech).ToList();
        var silenceSegments = segments.Where(s => !s.IsSpeech).ToList();

        return new VadDetectionResult
        {
            AudioPath = "mock_audio.mp3",
            AudioDuration = audioDuration,
            Segments = segments,
            SpeechSegmentCount = speechSegments.Count,
            SilenceSegmentCount = silenceSegments.Count,
            TotalSpeechDuration = speechSegments.Sum(s => s.Duration),
            TotalSilenceDuration = silenceSegments.Sum(s => s.Duration)
        };
    }

    private static SplitScheme? FindOptimalScheme(List<SplitScheme> schemes, VadDetectionResult vadResult)
    {
        const decimal MAX_SEGMENT_DURATION = 300m;

        var validSchemes = schemes
            .Where(s => s.SplitPoints.Count > 0)
            .Select(s => new
            {
                Scheme = s,
                MaxSegment = GetMaxSegmentDuration(s, vadResult)
            })
            .Where(x => x.MaxSegment <= MAX_SEGMENT_DURATION)
            .OrderByDescending(x => x.Scheme.SplitPoints.Count)
            .ToList();

        if (validSchemes.Any())
        {
            return validSchemes.First().Scheme;
        }

        return schemes.FirstOrDefault();
    }

    private static decimal GetMaxSegmentDuration(SplitScheme scheme, VadDetectionResult vadResult)
    {
        var currentTime = 0m;
        var maxDuration = 0m;

        foreach (var splitPoint in scheme.SplitPoints)
        {
            var segmentDuration = splitPoint - currentTime;
            if (segmentDuration > maxDuration)
            {
                maxDuration = segmentDuration;
            }
            currentTime = splitPoint;
        }

        var lastSegmentDuration = vadResult.AudioDuration - currentTime;
        if (lastSegmentDuration > maxDuration)
        {
            maxDuration = lastSegmentDuration;
        }

        return maxDuration;
    }
    #endregion

    #region FFmpeg命令生成测试

    private static void TestFFmpegCommandGeneration(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            logger.Information("开始FFmpeg命令生成测试");

            Console.WriteLine($"\n开始FFmpeg命令生成测试...");
            Console.WriteLine($"=".PadRight(80, '='));

            var testCases = new[]
            {
                new
                {
                    Name = "无字幕",
                    WriteSourceSubtitle = false,
                    WriteTargetSubtitle = false,
                    SourceSubtitleType = SubtitleType.SoftSubtitle,
                    TargetSubtitleType = SubtitleType.SoftSubtitle
                },
                new
                {
                    Name = "仅目标字幕-硬烧录",
                    WriteSourceSubtitle = false,
                    WriteTargetSubtitle = true,
                    SourceSubtitleType = SubtitleType.SoftSubtitle,
                    TargetSubtitleType = SubtitleType.HardBurn
                },
                new
                {
                    Name = "仅目标字幕-软字幕",
                    WriteSourceSubtitle = false,
                    WriteTargetSubtitle = true,
                    SourceSubtitleType = SubtitleType.SoftSubtitle,
                    TargetSubtitleType = SubtitleType.SoftSubtitle
                },
                new
                {
                    Name = "仅源字幕-硬烧录",
                    WriteSourceSubtitle = true,
                    WriteTargetSubtitle = false,
                    SourceSubtitleType = SubtitleType.HardBurn,
                    TargetSubtitleType = SubtitleType.SoftSubtitle
                },
                new
                {
                    Name = "仅源字幕-软字幕",
                    WriteSourceSubtitle = true,
                    WriteTargetSubtitle = false,
                    SourceSubtitleType = SubtitleType.SoftSubtitle,
                    TargetSubtitleType = SubtitleType.SoftSubtitle
                },
                new
                {
                    Name = "双字幕-都硬烧录",
                    WriteSourceSubtitle = true,
                    WriteTargetSubtitle = true,
                    SourceSubtitleType = SubtitleType.HardBurn,
                    TargetSubtitleType = SubtitleType.HardBurn
                },
                new
                {
                    Name = "双字幕-都软字幕",
                    WriteSourceSubtitle = true,
                    WriteTargetSubtitle = true,
                    SourceSubtitleType = SubtitleType.SoftSubtitle,
                    TargetSubtitleType = SubtitleType.SoftSubtitle
                },
                new
                {
                    Name = "双字幕-目标硬烧录源软字幕",
                    WriteSourceSubtitle = true,
                    WriteTargetSubtitle = true,
                    SourceSubtitleType = SubtitleType.SoftSubtitle,
                    TargetSubtitleType = SubtitleType.HardBurn
                },
                new
                    {
                    Name = "双字幕-源硬烧录目标软字幕",
                    WriteSourceSubtitle = true,
                    WriteTargetSubtitle = true,
                    SourceSubtitleType = SubtitleType.HardBurn,
                    TargetSubtitleType = SubtitleType.SoftSubtitle
                }
            };

            var inputVideoPath = @"D:\test\video.mp4";
            var outputAudioPath = @"D:\test\audio.wav";
            var translatedSubtitlePath = @"D:\test\chinese.srt";
            var sourceSubtitlePath = @"D:\test\english.srt";
            var outputPath = @"D:\test\output.mp4";
            var videoEncoder = "libx264";
            var preset = "medium";
            var crf = 23;
            var fastSubtitle = false;

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"\n测试用例: {testCase.Name}");
                Console.WriteLine($"-".PadRight(80, '-'));
                Console.WriteLine($"  写入源字幕: {testCase.WriteSourceSubtitle}");
                Console.WriteLine($"  写入目标字幕: {testCase.WriteTargetSubtitle}");
                Console.WriteLine($"  源字幕类型: {testCase.SourceSubtitleType}");
                Console.WriteLine($"  目标字幕类型: {testCase.TargetSubtitleType}");

                var ffmpegArgs = GenerateFFmpegCommand(
                    testCase.WriteSourceSubtitle,
                    testCase.WriteTargetSubtitle,
                    testCase.SourceSubtitleType,
                    testCase.TargetSubtitleType,
                    inputVideoPath,
                    outputAudioPath,
                    translatedSubtitlePath,
                    sourceSubtitlePath,
                    outputPath,
                    videoEncoder,
                    preset,
                    crf,
                    fastSubtitle);

                Console.WriteLine($"\n  生成的FFmpeg命令:");
                Console.WriteLine($"  {ffmpegArgs}");
                Console.WriteLine();
            }

            Console.WriteLine($"=".PadRight(80, '='));
            logger.Information("FFmpeg命令生成测试完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "FFmpeg命令生成测试失败");
            Console.WriteLine($"\nFFmpeg命令生成测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    private static string GenerateFFmpegCommand(
        bool writeSourceSubtitle,
        bool writeTargetSubtitle,
        SubtitleType sourceSubtitleType,
        SubtitleType targetSubtitleType,
        string inputVideoPath,
        string outputAudioPath,
        string translatedSubtitlePath,
        string sourceSubtitlePath,
        string outputPath,
        string videoEncoder,
        string preset,
        int crf,
        bool fastSubtitle)
    {
        var ffmpegArgs = "";
        var inputArgs = $"-i \"{inputVideoPath}\" -i \"{outputAudioPath}\" ";
        var filterComplexArgs = "";
        var mapArgs = "";
        var subtitleMapArgs = "";
        var hasHardSubtitle = false;
        var subtitleIndex = 0;

        var escapedChinesePath = translatedSubtitlePath.EscapeForFFmpeg();
        var escapedEnglishPath = sourceSubtitlePath.EscapeForFFmpeg();

        if (writeTargetSubtitle && !string.IsNullOrEmpty(translatedSubtitlePath))
        {
            if (targetSubtitleType == SubtitleType.HardBurn)
            {
                hasHardSubtitle = true;
                var fontSize = fastSubtitle ? 16 : 14;
                filterComplexArgs = $"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=50,Alignment=2'[v]";
            }
            else
            {
                inputArgs += $"-i \"{translatedSubtitlePath}\" ";
                subtitleMapArgs += $"-map 2:s:{subtitleIndex} -c:s:{subtitleIndex} mov_text ";
                subtitleIndex++;
            }
        }

        if (writeSourceSubtitle && !string.IsNullOrEmpty(sourceSubtitlePath))
        {
            if (sourceSubtitleType == SubtitleType.HardBurn)
            {
                hasHardSubtitle = true;
                if (hasHardSubtitle)
                {
                    var fontSize = fastSubtitle ? 14 : 14;
                    filterComplexArgs += $";[v]subtitles='{escapedEnglishPath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFF00,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=5,Alignment=2'[v]";
                }
                else
                {
                    var fontSize = fastSubtitle ? 16 : 14;
                    filterComplexArgs = $"[0:v]subtitles='{escapedEnglishPath}':force_style='Fontsize={fontSize},PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=2,Shadow=1,MarginV=50,Alignment=2'[v]";
                }
            }
            else
            {
                inputArgs += $"-i \"{sourceSubtitlePath}\" ";
                subtitleMapArgs += $"-map 2:s:{subtitleIndex} -c:s:{subtitleIndex} mov_text ";
                subtitleIndex++;
            }
        }

        if (hasHardSubtitle)
        {
            filterComplexArgs = $"-filter_complex \"{filterComplexArgs}\" ";
            mapArgs = "-map \"[v]\" -map 1:a ";
        }
        else
        {
            mapArgs = "-map 0:v -map 1:a ";
        }

        ffmpegArgs = $"{inputArgs}{filterComplexArgs}{mapArgs}{subtitleMapArgs}-c:v {videoEncoder} -preset {preset} -crf {crf} -c:a aac -b:a 192k -y \"{outputPath}\"";

        return ffmpegArgs;
    }

    #endregion

    #region FFmpeg进度测试

    private static async Task TestFFmpegProgressAsync(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            logger.Information("开始FFmpeg进度测试");

            Console.WriteLine($"\n开始FFmpeg进度测试...");
            Console.WriteLine($"=".PadRight(80, '='));

            var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
            var progressService = serviceProvider.GetRequiredService<IProgressService>();

            var inputVideoPath = @"D:\VideoTranslator\videoProjects\48\source_muted_video.mp4";
            var outputAudioPath = @"D:\VideoTranslator\videoProjects\48\merged_audio.wav";
            var translatedSubtitlePath = @"D:\VideoTranslator\videoProjects\48\translated_subtitle_lmstudio_4682435674794def9a69ac0b3ba5d082.srt";
            var outputPath = @"D:\VideoTranslator\videoProjects\48\test_output.mp4";

            if (!File.Exists(inputVideoPath))
            {
                logger.Error("测试视频文件不存在: {VideoPath}", inputVideoPath);
                Console.WriteLine($"测试视频文件不存在: {inputVideoPath}");
                return;
            }

            if (!File.Exists(outputAudioPath))
            {
                logger.Error("测试音频文件不存在: {AudioPath}", outputAudioPath);
                Console.WriteLine($"测试音频文件不存在: {outputAudioPath}");
                return;
            }

            if (!File.Exists(translatedSubtitlePath))
            {
                logger.Error("测试字幕文件不存在: {SubtitlePath}", translatedSubtitlePath);
                Console.WriteLine($"测试字幕文件不存在: {translatedSubtitlePath}");
                return;
            }

            var escapedChinesePath = translatedSubtitlePath.EscapeForFFmpeg();
            var ffmpegArgs = $"-i \"{inputVideoPath}\" -i \"{outputAudioPath}\" -t 60 -filter_complex \"[0:v]subtitles='{escapedChinesePath}':force_style='Fontsize=14,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000,BorderStyle=1,Outline=1,Shadow=1,MarginV=50,Alignment=2'[v]\" -map \"[v]\" -map 1:a -c:v libx264 -preset slow -crf 20 -c:a aac -b:a 192k -y \"{outputPath}\"";

            Console.WriteLine($"\n执行FFmpeg命令:");
            Console.WriteLine($"  {ffmpegArgs}");
            Console.WriteLine();
            Console.WriteLine($"开始处理，请观察输出中的进度信息...");
            Console.WriteLine();

            var result = await ffmpegService.ExecuteCommandAsync(ffmpegArgs);

            Console.WriteLine($"\nFFmpeg执行完成");
            Console.WriteLine($"输出长度: {result.Length} 字符");

            logger.Information("FFmpeg进度测试完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "FFmpeg进度测试失败");
            Console.WriteLine($"\nFFmpeg进度测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    #endregion

    #region GetVideoStreamDetails方法测试

    private static async Task TestGetVideoStreamDetailsAsync(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            logger.Information("开始GetVideoStreamDetails方法测试");

            Console.WriteLine($"\n开始GetVideoStreamDetails方法测试...");
            Console.WriteLine($"=".PadRight(80, '='));

            var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();

            var testVideoPath = @"d:\VideoTranslator\testvideo\1.mp4";

            if (!File.Exists(testVideoPath))
            {
                logger.Error("测试视频文件不存在: {VideoPath}", testVideoPath);
                Console.WriteLine($"测试视频文件不存在: {testVideoPath}");
                Console.WriteLine("请确保测试文件存在或修改路径");
                return;
            }

            Console.WriteLine($"测试视频文件: {testVideoPath}");
            Console.WriteLine($"\n开始获取视频流详细信息...");

            var streams = await ffmpegService.GetVideoStreamDetails(testVideoPath);

            Console.WriteLine($"\n获取到 {streams.Count} 个流:");
            Console.WriteLine($"=".PadRight(80, '='));

            foreach (var stream in streams)
            {
                Console.WriteLine($"\n流 #{stream.Index} - {stream.Type}");
                Console.WriteLine($"  编解码器: {stream.CodecName}");
                if (!string.IsNullOrEmpty(stream.Language))
                {
                    Console.WriteLine($"  语言: {stream.Language}");
                }
                if (stream.BitRate.HasValue)
                {
                    Console.WriteLine($"  比特率: {stream.BitRate.Value / 1000} kb/s");
                }
                if (stream.Type == StreamType.Video)
                {
                    if (stream.Width.HasValue && stream.Height.HasValue)
                    {
                        Console.WriteLine($"  分辨率: {stream.Width.Value}x{stream.Height.Value}");
                    }
                    if (stream.FrameRate.HasValue)
                    {
                        Console.WriteLine($"  帧率: {stream.FrameRate.Value:F2} fps");
                    }
                }
                if (stream.Type == StreamType.Audio)
                {
                    if (stream.SampleRate.HasValue)
                    {
                        Console.WriteLine($"  采样率: {stream.SampleRate.Value} Hz");
                    }
                    if (stream.Channels.HasValue)
                    {
                        Console.WriteLine($"  声道数: {stream.Channels.Value}");
                    }
                }
            }

            logger.Information("GetVideoStreamDetails方法测试完成，共获取 {StreamCount} 个流", streams.Count);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GetVideoStreamDetails方法测试失败");
            Console.WriteLine($"\nGetVideoStreamDetails方法测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    #endregion

    #region SeparateVideoStreams方法测试

    private static async Task TestSeparateVideoStreamsAsync(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            logger.Information("开始SeparateVideoStreams方法测试");

            Console.WriteLine($"\n开始SeparateVideoStreams方法测试...");
            Console.WriteLine($"=".PadRight(80, '='));

            var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();

            var testVideoPath = @"d:\VideoTranslator\testvideo\1.mp4";
            var outputDirectory = @"d:\VideoTranslator\testvideo\extracted_streams";

            if (!File.Exists(testVideoPath))
            {
                logger.Error("测试视频文件不存在: {VideoPath}", testVideoPath);
                Console.WriteLine($"测试视频文件不存在: {testVideoPath}");
                Console.WriteLine("请确保测试文件存在或修改路径");
                return;
            }

            Console.WriteLine($"测试视频文件: {testVideoPath}");
            Console.WriteLine($"输出目录: {outputDirectory}");
            Console.WriteLine($"\n开始分离视频流...");

            var result = await ffmpegService.SeparateVideoStreams(testVideoPath, outputDirectory);

            Console.WriteLine($"\n分离结果:");
            Console.WriteLine($"=".PadRight(80, '='));
            Console.WriteLine($"成功: {result.Success}");
            Console.WriteLine($"检测到流数量: {result.Streams.Count}");
            Console.WriteLine($"提取文件数量: {result.ExtractedFilePaths.Count}");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"错误信息: {result.ErrorMessage}");
            }

            if (result.Streams.Count > 0)
            {
                Console.WriteLine($"\n检测到的流信息:");
                foreach (var stream in result.Streams)
                {
                    Console.WriteLine($"  流 #{stream.Index} - {stream.Type} ({stream.CodecName})");
                }
            }

            if (result.ExtractedFilePaths.Count > 0)
            {
                Console.WriteLine($"\n提取的文件:");
                foreach (var kvp in result.ExtractedFilePaths)
                {
                    var streamIndex = kvp.Key;
                    var filePath = kvp.Value;
                    var stream = result.Streams.FirstOrDefault(s => s.Index == streamIndex);
                    var streamType = stream?.Type.ToString() ?? "Unknown";
                    Console.WriteLine($"  流 #{streamIndex} ({streamType}): {filePath}");
                }
            }

            logger.Information("SeparateVideoStreams方法测试完成，成功提取 {FileCount} 个文件", result.ExtractedFilePaths.Count);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "SeparateVideoStreams方法测试失败");
            Console.WriteLine($"\nSeparateVideoStreams方法测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    #endregion
}
