using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace VideoEditor;

public static class LoggerService
{
    #region 字段

    private static Serilog.ILogger? _logger;
    private static readonly string LogDirectory = @"d:\VideoTranslator\logs";

    #endregion

    #region 初始化

    public static void Initialize()
    {
        #region 清理旧日志目录
        if (Directory.Exists(LogDirectory))
        {
            try
            {
                Directory.Delete(LogDirectory, true);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"清理旧日志目录失败: {ex.Message}");
            }
        }
        #endregion

        #region 创建日志目录
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
        #endregion

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(LogDirectory, $"videoeditor_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            //.MinimumLevel.Error()
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

        _logger = Log.Logger;
        _logger.Information("VideoEditor 日志系统初始化完成，日志文件: {LogFilePath}", logFilePath);
    }

    #endregion

    #region 公共方法

    public static Serilog.ILogger ForContext<T>()
    {
        return _logger?.ForContext<T>() ?? Serilog.Log.Logger;
    }

    public static void Close()
    {
        _logger?.Information("VideoEditor 日志系统正在关闭");
        Log.CloseAndFlush();
        _logger = null;
    }

    #endregion
}
