using VideoTranslator.Models;

namespace VideoTranslator.Interfaces;

public interface ITTSServiceManager
{
    Task<bool> StartServicesAsync(int serviceCount = 5, int startPort = 8000, string[]? gpuIds = null);
    Task<bool> AutoDetectAndStartServicesAsync(int serviceCount = 5, int startPort = 8000);
    Task<bool> StopAllServicesAsync();
    Task<bool> StopServiceAsync(string serviceId);
    Task<bool> CheckServiceHealthAsync(string serviceId);
    Task<bool> CheckAllServicesHealthAsync();
    List<TTSServiceInstance> GetRunningServices();
    TTSServiceInstance? GetServiceById(string serviceId);
    TTSServiceInstance? GetLeastBusyService();
    Task<bool> RestartServiceAsync(string serviceId);
    string GetServiceStatus();
    void MarkServiceAsUnhealthy(string serviceId);
}
