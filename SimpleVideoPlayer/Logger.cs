using System;
using System.IO;
using System.Threading;

namespace SimpleVideoPlayer
{
    public class Logger
    {
        #region 字段

        private static Logger _instance;
        private static readonly object _lock = new object();
        private string _logFilePath;
        private StreamWriter _writer;

        #endregion

        #region 属性

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 构造函数

        private Logger()
        {
            Initialize();
        }

        #endregion

        #region 初始化方法

        private void Initialize()
        {
            try
            {
                var logDir = @"d:\VideoTranslator\logs";
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logFilePath = Path.Combine(logDir, $"videoplayer_{timestamp}.log");
                _writer = new StreamWriter(_logFilePath, false)
                {
                    AutoFlush = true
                };

                WriteLog("Logger", $"日志系统初始化完成，日志文件: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志初始化失败: {ex.Message}");
            }
        }

        #endregion

        #region 公共方法

        public void WriteLog(string category, string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}";
                
                lock (_lock)
                {
                    _writer?.WriteLine(logEntry);
                }

                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志失败: {ex.Message}");
            }
        }

        public void WriteLog(string category, string message, Exception ex)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}\n异常: {ex.GetType().Name}\n消息: {ex.Message}\n堆栈: {ex.StackTrace}";
                
                lock (_lock)
                {
                    _writer?.WriteLine(logEntry);
                }

                Console.WriteLine(logEntry);
            }
            catch
            {
                Console.WriteLine($"写入日志失败");
            }
        }

        public void Dispose()
        {
            try
            {
                lock (_lock)
                {
                    WriteLog("Logger", "日志系统正在关闭");
                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;
                }
            }
            catch
            {
            }
        }

        #endregion
    }
}
