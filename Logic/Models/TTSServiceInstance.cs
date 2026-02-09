using System.Diagnostics;

namespace VideoTranslator.Models;

public class TTSServiceInstance
{
    public string ServiceId { get; set; } = string.Empty;
    public string GpuId { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ApiUrl { get; set; } = string.Empty;
    public Process? Process { get; set; }
    public bool IsRunning { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public int TaskCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
