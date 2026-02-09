﻿﻿﻿﻿using Serilog;
using Serilog.Events;
using System;

namespace TimeLine.Services;

public static class LoggerService
{
    #region 字段

    private static ILogger? _logger;

    #endregion

    #region 初始化

    public static void Initialize()
    {
        _logger = Log.Logger;
        _logger.Information("TimeLine 日志系统已连接到全局日志配置");
    }

    #endregion

    #region 公共方法

    public static ILogger ForContext<T>()
    {
        return Log.ForContext<T>();
    }

    public static ILogger ForContext(string sourceContext)
    {
        return Log.ForContext("SourceContext", sourceContext);
    }

    public static void CloseAndFlush()
    {
        _logger?.Information("TimeLine 日志系统断开连接");
        _logger = null;
    }

    #endregion
}
