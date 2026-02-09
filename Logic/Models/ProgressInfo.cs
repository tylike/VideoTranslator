namespace VideoTranslator.Models;

[Obsolete("Use IProgressService interface instead.")]
public class ProgressInfo
{
    public string? Message { get; set; }
    public double? Percentage { get; set; }

    public ProgressInfo(string? message = null, double? percentage = null)
    {
        Message = message;
        Percentage = percentage;
    }

    public static ProgressInfo FromMessage(string message)
    {
        return new ProgressInfo(message);
    }

    public static ProgressInfo FromPercentage(double percentage, string? message = null)
    {
        return new ProgressInfo(message, percentage);
    }
}
