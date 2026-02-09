using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VideoTranslator.Utils;
using VT.Module;

namespace VideoTranslator.ExtractVideoTest;

class Program
{
    static async Task Main(string[] args)
    {
        #region 初始化日志

        var logDirectory = @"d:\VideoTranslator\logs";
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(logDirectory, $"extract_video_test_{timestamp}.log");

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
        logger.Information("提取视频流测试程序启动");

        #endregion

        #region 初始化服务容器

        var serviceProvider = ServiceHelper.InitializeServices(services =>
        {
            services.AddSingleton<IProgressService, SimpleProgressService>();
        });
        logger.Information("服务容器初始化完成");

        #endregion

        try
        {
            var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();

            var testVideoPath = @"d:\VideoTranslator\testvideo\1.mp4";
            var outputDirectory = @"d:\VideoTranslator\testvideo\extracted_streams";

            if (!File.Exists(testVideoPath))
            {
                logger.Error("测试视频文件不存在: {VideoPath}", testVideoPath);
                Console.WriteLine($"测试视频文件不存在: {testVideoPath}");
                Console.WriteLine("请确保测试文件存在或修改路径");
                Console.WriteLine("\n按任意键退出...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\n开始提取视频流测试...");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"测试视频文件: {testVideoPath}");
            Console.WriteLine($"输出目录: {outputDirectory}");

            #region 测试1: 使用默认输出路径

            Console.WriteLine($"\n测试1: 使用默认输出路径");
            Console.WriteLine(new string('-', 80));

            var result1 = await ffmpegService.ExtractVideoStream(testVideoPath);

            if (result1 != null)
            {
                Console.WriteLine($"成功: True");
                Console.WriteLine($"输出文件: {result1}");
                Console.WriteLine($"文件存在: {File.Exists(result1)}");

                if (File.Exists(result1))
                {
                    var fileInfo = new FileInfo(result1);
                    Console.WriteLine($"文件大小: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
                }

                logger.Information("测试1完成，输出文件: {OutputPath}", result1);
            }
            else
            {
                Console.WriteLine($"成功: False");
                logger.Warning("测试1完成，但输出文件为空");
            }

            #endregion

            #region 测试2: 使用自定义输出路径

            Console.WriteLine($"\n测试2: 使用自定义输出路径");
            Console.WriteLine(new string('-', 80));

            var customOutputPath = Path.Combine(outputDirectory, "1_video_custom.mp4");
            var result2 = await ffmpegService.ExtractVideoStream(testVideoPath, customOutputPath);

            if (result2 != null)
            {
                Console.WriteLine($"成功: True");
                Console.WriteLine($"输出文件: {result2}");
                Console.WriteLine($"文件存在: {File.Exists(result2)}");

                if (File.Exists(result2))
                {
                    var fileInfo = new FileInfo(result2);
                    Console.WriteLine($"文件大小: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
                }

                logger.Information("测试2完成，输出文件: {OutputPath}", result2);
            }
            else
            {
                Console.WriteLine($"成功: False");
                logger.Warning("测试2完成，但输出文件为空");
            }

            #endregion

            logger.Information("所有测试完成");
            Console.WriteLine("\n所有测试已完成，按任意键退出...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "测试程序运行失败");
            Console.WriteLine($"\n测试程序运行失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        Log.CloseAndFlush();
    }
}

public class SimpleProgressService : IProgressService
{
    public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        Console.WriteLine($"[{type}] {message}");
    }

    public void ShowProgress(bool marquee = false)
    {
    }

    public void HideProgress()
    {
    }

    public void ResetProgress()
    {
    }

    public void ReportProgress(double value)
    {
        Console.WriteLine($"[Progress] {value:F1}%");
    }

    public void SetProgressMaxValue(double value)
    {
    }

    public void Report(string message)
    {
        Console.WriteLine($"[Report] {message}");
    }

    public void Report(string message, MessageType messageType = MessageType.Info)
    {
        Console.WriteLine($"[{messageType}] {message}");
    }

    public void Title(string message)
    {
        Console.WriteLine($"[Title] {message}");
    }

    public void Success(string message)
    {
        Console.WriteLine($"[Success] {message}");
    }

    public void Error(string message)
    {
        Console.WriteLine($"[Error] {message}");
    }

    public void Warning(string message)
    {
        Console.WriteLine($"[Warning] {message}");
    }

    public void Debug(string message)
    {
        Console.WriteLine($"[Debug] {message}");
    }
}
