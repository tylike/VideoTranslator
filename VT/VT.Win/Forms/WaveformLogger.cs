using System;
using System.IO;

namespace VT.Win.Forms;

public class WaveformLogger
{
    private readonly string logFilePath;

    public WaveformLogger(string logFilePath)
    {
        this.logFilePath = logFilePath;
    }

    #region Public Methods

    public void LogInfo(string message)
    {
        try
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Write log failed: {ex.Message}");
        }
    }

    #endregion
}
