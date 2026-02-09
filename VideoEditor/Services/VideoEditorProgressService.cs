using Serilog;
using VideoTranslator.Interfaces;
using System.Windows;
using System.Text;
using VideoEditor.Windows;

namespace VideoEditor.Services;

public class VideoEditorProgressService : IProgressService
{
    #region 字段和属性

    private readonly ILogger _logger = LoggerService.ForContext<VideoEditorProgressService>();
    private MainWindow? _mainWindow;
    private DateTime _lastStatusUpdateTime = DateTime.MinValue;
    private const int STATUS_UPDATE_INTERVAL_MS = 200;
    private DetailLogWindow? _detailLogWindow;
    private readonly StringBuilder _statusLog = new StringBuilder();

    public string StatusLog => _statusLog.ToString();

    #endregion

    #region 初始化

    public VideoEditorProgressService()
    {
        _logger.Information("VideoEditorProgressService 实例创建");
    }

    public void Initialize(MainWindow mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _logger.Information("VideoEditorProgressService 已初始化，MainWindow 已设置");
    }

    #endregion

    #region IProgressService 接口实现

    void IProgressService.ReportProgress(double value)
    {
        UpdateProgress(value);
    }

    void IProgressService.SetProgressMaxValue(double value)
    {
        if (_mainWindow?.StatusProgressBar != null)
        {
            _mainWindow.Dispatcher.BeginInvoke(() =>
            {
                _mainWindow.StatusProgressBar.Maximum = value;
            });
        }
    }

    #endregion

    #region 进度条更新

    public void UpdateProgressBar(bool isIndeterminate)
    {
        if (_mainWindow?.StatusProgressBar != null)
        {
            _mainWindow.Dispatcher.BeginInvoke(() =>
            {
                _mainWindow.StatusProgressBar.IsIndeterminate = isIndeterminate;
                if (!isIndeterminate)
                {
                    _mainWindow.StatusProgressBar.Value = 0;
                }
            });
        }
    }

    public void UpdateProgress(double progress)
    {
        if (_mainWindow?.StatusProgressBar != null)
        {
            _mainWindow.Dispatcher.BeginInvoke(() =>
            {
                _mainWindow.StatusProgressBar.Value = progress;
            });
        }
    }

    public void ShowProgress(bool marquee = false)
    {
        UpdateProgressBar(marquee);
        _logger.Debug("显示进度条: {Marquee}", marquee);
    }

    public void HideProgress()
    {
        UpdateProgressBar(false);
        _logger.Debug("隐藏进度条");
    }

    public void ResetProgress()
    {
        UpdateProgress(0);
        _logger.Debug("重置进度条");
    }

    #endregion

    #region 状态消息更新

    public void UpdateStatus(string message, MessageType type = MessageType.Info, bool log = true, bool newline = true)
    {
        var now = DateTime.Now;
        var elapsed = (now - _lastStatusUpdateTime).TotalMilliseconds;

        _lastStatusUpdateTime = now;

        if (_mainWindow?.StatusTextControl != null)
        {
            _mainWindow.Dispatcher.BeginInvoke(() =>
            {
                _mainWindow.StatusTextControl.Text = message;
            });
        }

        if (log)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (newline)
            {
                _statusLog.AppendLine(logEntry);
            }
            else
            {
                _statusLog.Append(logEntry);
            }
            _logger.Debug(logEntry);
        }

        if (_detailLogWindow != null && _detailLogWindow.IsVisible)
        {
            _detailLogWindow.AppendMessage(message, type, newline);
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

    #region 详细日志窗口

    public void ShowDetailLogWindow()
    {
        if (_mainWindow == null)
        {
            return;
        }

        if (_detailLogWindow != null && _detailLogWindow.IsVisible)
        {
            _detailLogWindow.Activate();
            return;
        }

        _detailLogWindow = new DetailLogWindow
        {
            Owner = _mainWindow
        };

        foreach (var logEntry in _statusLog.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
        {
            _detailLogWindow.AppendMessage(logEntry, VideoTranslator.Interfaces.MessageType.Info);
        }

        _detailLogWindow.Closed += (s, args) => _detailLogWindow = null;
        _detailLogWindow.Show();
    }

    #endregion
}
