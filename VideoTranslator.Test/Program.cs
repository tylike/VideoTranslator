using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
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
            logger.Information("开始测试 BrowseVideoButton_Click 逻辑");
            await TestBrowseVideoButtonLogic(serviceProvider, logger);

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

    

    private static async Task TestBrowseVideoButtonLogic(IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        try
        {
            Console.WriteLine($"\n开始测试 BrowseVideoButton_Click 逻辑...");
            Console.WriteLine(new string('=', 80));

            var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
            var whisperService = serviceProvider.GetRequiredService<WhisperRecognitionService>();

            var testVideoPath = @"d:\VideoTranslator\testvideo\1.mp4";

            if (!File.Exists(testVideoPath))
            {
                logger.Error("测试视频文件不存在: {VideoPath}", testVideoPath);
                Console.WriteLine($"测试视频文件不存在: {testVideoPath}");
                Console.WriteLine("请确保测试文件存在或修改路径");
                return;
            }

            Console.WriteLine($"测试视频文件: {testVideoPath}");
            Console.WriteLine($"\n开始模拟 BrowseVideoButton_Click 逻辑...");
            Console.WriteLine(new string('-', 80));

            

            Console.WriteLine("\n步骤1: 获取视频流信息");
            Console.WriteLine(new string('-', 80));

            var streamInfo = await ffmpegService.GetVideoStreamInfo(testVideoPath);
            Console.WriteLine($"有视频流: {streamInfo.HasVideo}");
            Console.WriteLine($"有音频流: {streamInfo.HasAudio}");

            logger.Information("视频流信息 - 有视频: {HasVideo}, 有音频: {HasAudio}", 
                streamInfo.HasVideo, streamInfo.HasAudio);

            

            Console.WriteLine("\n步骤2: 模拟仅视频模式逻辑 (_videoOnlyMode.IsChecked == true)");
            Console.WriteLine(new string('-', 80));

            if (!streamInfo.HasAudio)
            {
                Console.WriteLine("视频没有音频流，自动切换到 静音视频+音频模式");
                logger.Warning("视频没有音频流，需要切换模式");
            }
            else
            {
                Console.WriteLine("视频有音频流，开始分离视频和音频");

                var videoDirectory = Path.GetDirectoryName(testVideoPath);
                var audioFilePath = Path.Combine(videoDirectory!, "audio_source.wav");
                var muteVideoPath = Path.Combine(videoDirectory!, $"mute_video_source{Path.GetExtension(testVideoPath)}");

                Console.WriteLine($"音频输出路径: {audioFilePath}");
                Console.WriteLine($"静音视频输出路径: {muteVideoPath}");

                try
                {
                    

                    Console.WriteLine("\n步骤3: 分离视频和音频");
                    Console.WriteLine(new string('-', 80));

                    await ffmpegService.SeparateMainVideoAndAudio(testVideoPath, muteVideoPath, audioFilePath);

                    Console.WriteLine("分离完成");
                    logger.Information("视频和音频分离完成");
                    
                    

                    Console.WriteLine("\n验证生成的文件:");
                    if (File.Exists(muteVideoPath))
                    {
                        var muteVideoInfo = new FileInfo(muteVideoPath);
                        Console.WriteLine($"✓ 静音视频文件存在: {muteVideoPath}");
                        Console.WriteLine($"  文件大小: {muteVideoInfo.Length / (1024.0 * 1024.0):F2} MB");
                        logger.Information("静音视频文件已生成: {FilePath}, 大小: {SizeMB:F2} MB", 
                            muteVideoPath, muteVideoInfo.Length / (1024.0 * 1024.0));
                    }
                    else
                    {
                        Console.WriteLine($"✗ 静音视频文件不存在: {muteVideoPath}");
                        logger.Error("静音视频文件未生成: {FilePath}", muteVideoPath);
                    }

                    if (File.Exists(audioFilePath))
                    {
                        var audioInfo = new FileInfo(audioFilePath);
                        Console.WriteLine($"✓ 音频文件存在: {audioFilePath}");
                        Console.WriteLine($"  文件大小: {audioInfo.Length / (1024.0 * 1024.0):F2} MB");
                        logger.Information("音频文件已生成: {FilePath}, 大小: {SizeMB:F2} MB", 
                            audioFilePath, audioInfo.Length / (1024.0 * 1024.0));
                    }
                    else
                    {
                        Console.WriteLine($"✗ 音频文件不存在: {audioFilePath}");
                        logger.Error("音频文件未生成: {FilePath}", audioFilePath);
                    }

                    

                    

                    Console.WriteLine("\n步骤4: 检测音频语言");
                    Console.WriteLine(new string('-', 80));

                    if (File.Exists(audioFilePath))
                    {
                        var detectedLanguage = await whisperService.DetectLanguageAsync(audioFilePath);
                        Console.WriteLine($"检测到的语言: {detectedLanguage}");
                        logger.Information("检测到的语言: {Language}", detectedLanguage);

                        

                        Console.WriteLine("\n步骤5: 根据检测结果设置语言");
                        Console.WriteLine(new string('-', 80));

                        Language sourceLanguage;
                        Language targetLanguage;

                        if (detectedLanguage == Language.English)
                        {
                            sourceLanguage = Language.English;
                            targetLanguage = Language.Chinese;
                            Console.WriteLine($"源语言: {sourceLanguage}");
                            Console.WriteLine($"目标语言: {targetLanguage}");
                        }
                        else if (detectedLanguage == Language.Chinese)
                        {
                            sourceLanguage = Language.Chinese;
                            targetLanguage = Language.English;
                            Console.WriteLine($"源语言: {sourceLanguage}");
                            Console.WriteLine($"目标语言: {targetLanguage}");
                        }
                        else
                        {
                            sourceLanguage = detectedLanguage;
                            targetLanguage = Language.Chinese;
                            Console.WriteLine($"源语言: {sourceLanguage}");
                            Console.WriteLine($"目标语言: {targetLanguage}");
                        }

                        logger.Information("语言设置完成 - 源语言: {SourceLanguage}, 目标语言: {TargetLanguage}", 
                            sourceLanguage, targetLanguage);

                        
                    }
                    else
                    {
                        Console.WriteLine("音频文件不存在，跳过语言检测");
                        logger.Warning("音频文件不存在，跳过语言检测");
                    }

                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n分离视频和音频失败: {ex.Message}");
                    logger.Error(ex, "分离视频和音频失败");
                }
            }

            

            Console.WriteLine("\n测试完成");
            Console.WriteLine(new string('=', 80));
            logger.Information("BrowseVideoButton_Click 逻辑测试完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "BrowseVideoButton_Click 逻辑测试失败");
            Console.WriteLine($"\n测试失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex}");
        }
    }

    
}
