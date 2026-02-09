using VideoTranslator.Models;

namespace VideoTranslator.Interfaces;

public interface ISegmentProgressCallback
{
    void ReportProgress(string message);
    void ReportProgress(int current, int total, string message);
    void LogOperation(string operation, params (string name, object value)[] parameters);
    void OnSegmentCompleted(int clipCount);
    void OnSegmentFailed(string errorMessage);
}

public class SilentSegmentProgressCallback : ISegmentProgressCallback
{
    public void ReportProgress(string message) { }
    public void ReportProgress(int current, int total, string message) { }
    public void LogOperation(string operation, params (string name, object value)[] parameters) { }
    public void OnSegmentCompleted(int clipCount) { }
    public void OnSegmentFailed(string errorMessage) { }
}

public class ConsoleSegmentProgressCallback : ISegmentProgressCallback
{
    private readonly Action<string> _writeLine;

    public ConsoleSegmentProgressCallback(Action<string>? writeLine = null)
    {
        //_writeLine = writeLine ?? (msg => progress.Report($"[进度] {msg}"));
    }

    public void ReportProgress(string message)
    {
        _writeLine(message);
    }

    public void ReportProgress(int current, int total, string message)
    {
        _writeLine($"[进度] {current}/{total}: {message}");
    }

    public void LogOperation(string operation, params (string name, object value)[] parameters)
    {
        _writeLine($"[操作] {operation}");
        foreach (var (name, value) in parameters)
        {
            _writeLine($"  - {name}: {value}");
        }
    }

    public void OnSegmentCompleted(int clipCount)
    {
        _writeLine($"[完成] 成功创建 {clipCount} 个片段");
    }

    public void OnSegmentFailed(string errorMessage)
    {
        _writeLine($"[错误] {errorMessage}");
    }
}
