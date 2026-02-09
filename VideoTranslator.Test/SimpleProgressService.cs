using VideoTranslator.Interfaces;

namespace VideoTranslator.Test;

public class SimpleProgressService : IProgressService
{
    public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        Console.WriteLine($"[{type}] {message}");
    }

    public void ShowProgress(bool marquee = false)
    {
    }

    public void HideProgress()
    {
    }

    public void ResetProgress()
    {
    }

    public void ReportProgress(double value)
    {
        Console.WriteLine($"[Progress] {value:F1}%");
    }

    public void SetProgressMaxValue(double value)
    {
    }

    public void Report(string message)
    {
        Console.WriteLine($"[Report] {message}");
    }

    public void Report(string message, MessageType messageType = MessageType.Info)
    {
        Console.WriteLine($"[{messageType}] {message}");
    }

    public void Title(string message)
    {
        Console.WriteLine($"[Title] {message}");
    }

    public void Success(string message)
    {
        Console.WriteLine($"[Success] {message}");
    }

    public void Error(string message)
    {
        Console.WriteLine($"[Error] {message}");
    }

    public void Warning(string message)
    {
        Console.WriteLine($"[Warning] {message}");
    }

    public void Debug(string message)
    {
        Console.WriteLine($"[Debug] {message}");
    }
}
