using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;
using VideoTranslator.Utils;
using VT.Core;

namespace VideoTranslator.Test;

class Program
{
    static async Task Main(string[] args)
    {
        

        var logDirectory = @"d:\VideoTranslator\logs";
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(logDirectory, $"test_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
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
        logger.Information("测试程序启动");

        

        

        var services = new ServiceCollection();
        services.AddVideoTranslatorServices(registerProgressService: false);
        services.AddSingleton<IProgressService, SimpleProgressService>();
        var serviceProvider = services.BuildServiceProvider();
        
        

        VideoTranslator.Services.ServiceBase.ServiceProvider = serviceProvider;

        
        
        logger.Information("服务容器初始化完成");

        

        try
        {
            logger.Information("开始测试 AudioTimelineComposer 大量片段处理");
            await TestAudioTimelineComposerWithManySegments(serviceProvider, logger);

            logger.Information("所有测试完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "测试程序运行失败");
            Console.WriteLine($"\n测试程序运行失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }

        Log.CloseAndFlush();
    }

    static async Task TestAudioTimelineComposerWithManySegments(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
        var progressService = serviceProvider.GetRequiredService<IProgressService>();
        var composer = new AudioTimelineComposer(ffmpegService, progressService);

        var testDir = @"d:\VideoTranslator\test_audio";
        Directory.CreateDirectory(testDir);

        logger.Information("开始创建测试音频文件");

        var backgroundAudioPath = Path.Combine(testDir, "background.wav");
        var segments = new List<AudioClipWithTime>();

        var segmentDurationMs = 100;
        var segmentGapMs = 100;
        var totalSegments = 25;

        await CreateTestAudioFile(backgroundAudioPath, 30000, ffmpegService, logger);

        for (int i = 0; i < totalSegments; i++)
        {
            var segmentPath = Path.Combine(testDir, $"segment_{i:D3}.wav");
            await CreateTestAudioFile(segmentPath, segmentDurationMs, ffmpegService, logger);

            var startTime = TimeSpan.FromMilliseconds(i * (segmentDurationMs + segmentGapMs));
            var endTime = startTime + TimeSpan.FromMilliseconds(segmentDurationMs);

            segments.Add(new AudioClipWithTime
            {
                FilePath = segmentPath,
                Start = startTime,
                End = endTime,
                AudioDurationMs = segmentDurationMs,
                Index = i
            });
        }

        logger.Information($"创建了 {totalSegments} 个测试片段");

        var outputPath = Path.Combine(testDir, "output.wav");

        logger.Information("开始合成音频");
        var result = await composer.Compose(backgroundAudioPath, segments, outputPath);

        if (result.Success)
        {
            logger.Information($"合成成功！输出文件: {result.OutputPath}");
            logger.Information($"输出信息: {result.Output}");

            if (File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                logger.Information($"输出文件大小: {fileInfo.Length} 字节");
            }
        }
        else
        {
            logger.Error($"合成失败: {result.Error}");
            logger.Error($"退出代码: {result.ExitCode}");
        }

        logger.Information("测试完成");
    }

    static async Task CreateTestAudioFile(string outputPath, int durationMs, IFFmpegService ffmpegService, Serilog.ILogger logger)
    {
        var durationSec = durationMs / 1000.0;
        var command = $"-f lavfi -i anullsrc=r=22050:cl=mono -t {durationSec:F3} -y \"{outputPath}\"";
        
        logger.Information($"创建测试音频: {outputPath}, 时长: {durationMs}ms");
        await ffmpegService.ExecuteCommandAsync(command);
    }

    

    
}
