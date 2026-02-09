using System.IO;
using System.Text;

namespace VT.Win.Services;
using MessageType = VideoTranslator.Interfaces.MessageType;
public class FileLoggerService
{
    #region Private Fields

    private static FileLoggerService _instance;
    private static readonly object _lock = new object();
    private readonly string _logDirectory;
    private readonly string _currentLogFile;
    private readonly DateTime _applicationStartTime;

    #endregion

    #region Public Properties

    public static FileLoggerService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FileLoggerService();
                    }
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Constructor

    private FileLoggerService()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        _applicationStartTime = DateTime.Now;
        _currentLogFile = GenerateLogFileName();
        EnsureLogDirectoryExists();
    }

    #endregion

    #region Public Methods

    public void Log(string message, MessageType type = MessageType.Info)
    {
        #region Prepare Log Entry

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{type}] {message}";

        #endregion

        #region Write to File

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_currentLogFile, logEntry + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
            }
        }

        #endregion
    }

    #endregion

    #region Private Methods

    private string GenerateLogFileName()
    {
        var fileName = $"VT_{_applicationStartTime:yyyyMMdd_HHmmss}.log";
        return Path.Combine(_logDirectory, fileName);
    }

    private void EnsureLogDirectoryExists()
    {
        #region Create Directory if Not Exists

        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }

        #endregion
    }

    #endregion
}
