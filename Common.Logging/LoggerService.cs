using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace Common.Logging;

public static class LoggerService
{
    #region 字段

    private static Serilog.ILogger? _logger;
    private static readonly string LogDirectory = @"d:\VideoTranslator\logs";

    #endregion

    #region 初始化

    public static void Initialize(string? applicationName = null, LogEventLevel minimumLevel = LogEventLevel.Error)
    {
        #region 创建日志目录

        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }

        #endregion

        #region 生成日志文件路径

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var appName = string.IsNullOrEmpty(applicationName) ? "application" : applicationName.ToLower().Replace(" ", "_");
        var logFilePath = Path.Combine(LogDirectory, $"{appName}_{timestamp}.log");

        #endregion

        #region 配置 Serilog

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
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
        _logger.Information("日志系统初始化完成，应用程序: {ApplicationName}, 日志文件: {LogFilePath}", applicationName ?? "Unknown", logFilePath);

        #endregion
    }

    #endregion

    #region 公共方法

    public static Serilog.ILogger ForContext<T>()
    {
        return _logger?.ForContext<T>() ?? Serilog.Log.Logger;
    }

    public static Serilog.ILogger ForContext(string sourceContext)
    {
        return _logger?.ForContext("SourceContext", sourceContext) ?? Serilog.Log.Logger;
    }

    public static void Close()
    {
        _logger?.Information("日志系统正在关闭");
        Log.CloseAndFlush();
        _logger = null;
    }

    #endregion
}
