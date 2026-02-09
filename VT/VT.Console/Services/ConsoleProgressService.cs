using Serilog;
using VideoTranslator.Interfaces;
using System;
using System.Text;

namespace VT.Console.Services;

public class ConsoleProgressService : IProgressService
{
    #region 字段和属性

    private readonly ILogger _logger = Log.ForContext<ConsoleProgressService>();
    private DateTime _lastStatusUpdateTime = DateTime.MinValue;
    private const int STATUS_UPDATE_INTERVAL_MS = 200;
    private readonly StringBuilder _statusLog = new StringBuilder();
    private double _progressValue = 0;
    private double _progressMax = 100;
    private bool _isIndeterminate = false;

    public string StatusLog => _statusLog.ToString();

    #endregion

    #region 初始化

    public ConsoleProgressService()
    {
        _logger.Information("ConsoleProgressService 实例创建");
    }

    #endregion

    #region IProgressService 接口实现

    void IProgressService.ReportProgress(double value)
    {
        UpdateProgress(value);
    }

    void IProgressService.SetProgressMaxValue(double value)
    {
        _progressMax = value;
        DisplayProgress();
    }

    void IProgressService.SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        SetStatusMessage(message, type, newline, log);
    }

    #endregion

    #region 进度条更新

    public void UpdateProgressBar(bool isIndeterminate)
    {
        _isIndeterminate = isIndeterminate;
        if (!isIndeterminate)
        {
            _progressValue = 0;
        }
        DisplayProgress();
        _logger.Debug("显示进度条: {Marquee}", isIndeterminate);
    }

    public void UpdateProgress(double progress)
    {
        _progressValue = progress;
        DisplayProgress();
    }

    public void ShowProgress(bool marquee = false)
    {
        UpdateProgressBar(marquee);
        _logger.Debug("显示进度条: {Marquee}", marquee);
    }

    public void HideProgress()
    {
        UpdateProgressBar(false);
        System.Console.WriteLine();
        _logger.Debug("隐藏进度条");
    }

    public void ResetProgress()
    {
        UpdateProgress(0);
        _logger.Debug("重置进度条");
    }

    private void DisplayProgress()
    {
        if (_isIndeterminate)
        {
            System.Console.Write($"\r进度: 处理中... ");
        }
        else
        {
            var percentage = _progressMax > 0 ? (_progressValue / _progressMax * 100) : 0;
            System.Console.Write($"\r{percentage:F1}%");
        }
    }

    #endregion

    #region 状态消息更新

    public void UpdateStatus(string message, MessageType type = MessageType.Info, bool log = true, bool newline = true)
    {
        var now = DateTime.Now;
        var elapsed = (now - _lastStatusUpdateTime).TotalMilliseconds;

        _lastStatusUpdateTime = now;

        var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        
        if (newline)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(logEntry);
            _statusLog.AppendLine(logEntry);
        }
        else
        {
            System.Console.Write(message);
            _statusLog.Append(logEntry);
        }

        if (log)
        {
            _logger.Debug(logEntry);
        }
    }

    public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        if (log)
        {
            Action logAction = type switch
            {
                MessageType.Info => () => _logger.Information(" {Message}", message),
                MessageType.Success => () => _logger.Information(" [SUCCESS] {Message}", message),
                MessageType.Warning => () => _logger.Warning(" {Message}", message),
                MessageType.Error => () => _logger.Error(" {Message}", message),
                MessageType.Debug => () => _logger.Debug(" {Message}", message),
                MessageType.Title => () => _logger.Information(" [TITLE] {Message}", message),
                _ => () => _logger.Information(" {Message}", message)
            };
            logAction();
        }
        UpdateStatus(message, type, log, newline);
    }

    #endregion
}
