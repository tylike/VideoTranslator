using Serilog;
using VideoTranslator.Interfaces;
using MessageType = VideoTranslator.Interfaces.MessageType;

namespace VideoTranslator.Services;

[Obsolete]
public class SimpleProgressService : IProgressService
{
    private readonly ILogger _logger = Log.ForContext<SimpleProgressService>();

    public object Application { get; set; }

    public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        if (!log)
        {
            return;
        }

        Action logAction = type switch
        {
            MessageType.Info => () => _logger.Information(message),
            MessageType.Success => () => _logger.Information($"[SUCCESS] {message}"),
            MessageType.Warning => () => _logger.Warning(message),
            MessageType.Error => () => _logger.Error(message),
            MessageType.Debug => () => _logger.Debug(message),
            MessageType.Title => () => _logger.Information($"[TITLE] {message}"),
            _ => () => _logger.Information(message)
        };
        
        logAction();
    }

    public void ShowProgress(bool marquee = false)
    {
        _logger.Debug("显示进度条: {Marquee}", marquee);
    }

    public void HideProgress()
    {
        _logger.Debug("隐藏进度条");
    }

    public void ResetProgress()
    {
        _logger.Debug("重置进度条");
    }

    public void Report()
    {
        Report("", MessageType.Info);
    }

    public void Report(string message, MessageType messageType = MessageType.Info)
    {
        SetStatusMessage(message, messageType);
    }

    public void Title(string message)
    {
        SetStatusMessage(message, MessageType.Title);
    }

    public void Success(string message)
    {
        SetStatusMessage(message, MessageType.Success);
    }

    public void Error(string message)
    {
        SetStatusMessage(message, MessageType.Error);
    }

    public void Warning(string message)
    {
        SetStatusMessage(message, MessageType.Warning);
    }

    void IProgressService.ReportProgress(double value)
    {
        _logger.Debug("进度: {Value}", value);
    }

    void IProgressService.SetProgressMaxValue(double value)
    {
        _logger.Debug("设置进度最大值: {Maximum}", value);
    }
}
