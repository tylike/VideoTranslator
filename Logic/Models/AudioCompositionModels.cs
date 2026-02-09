namespace VideoTranslator.Models;

public class ComposeResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}

public class AudioCompositionException : Exception
{
    public AudioCompositionException(string message) : base(message) { }
    public AudioCompositionException(string message, Exception innerException) : base(message, innerException) { }
}
