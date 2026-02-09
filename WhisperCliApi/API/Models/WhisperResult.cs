namespace WhisperCliApi.Models;

public class WhisperResult
{
    public bool Success { get; set; }

    public int ExitCode { get; set; }

    public string? OutputPath { get; set; }

    public List<string> GeneratedFiles { get; set; } = new();

    public string? StandardOutput { get; set; }

    public string? StandardError { get; set; }

    public TimeSpan ProcessingTime { get; set; }

    public string? ErrorMessage { get; set; }
}
