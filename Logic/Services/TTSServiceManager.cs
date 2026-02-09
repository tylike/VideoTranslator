using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VideoTranslator.Services;

public class TTSServiceManager : ServiceBase, ITTSServiceManager
{
    private readonly Dictionary<string, TTSServiceInstance> _services;
    private readonly HttpClient _httpClient;
    private readonly string _baseDirectory;
    private readonly string _batchScriptPath;
    private readonly object _lock = new();

    public TTSServiceManager():base()
    {
        _services = new Dictionary<string, TTSServiceInstance>();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var rootDir = Path.GetFullPath(Path.Combine(_baseDirectory, "..", "..", "..", ".."));
        _batchScriptPath = Path.Combine(rootDir, "startApi-fixed.bat");
    }

    public async Task<bool> StartServicesAsync(int serviceCount = 5, int startPort = 8000, string[]? gpuIds = null)
    {
        progress?.Report($"[TTSServiceManager] 开始启动 {serviceCount} 个TTS服务...");
        progress?.Report($"起始端口: {startPort}");

        if (gpuIds == null || gpuIds.Length == 0)
        {
            gpuIds = Enumerable.Range(0, serviceCount).Select(i => i.ToString()).ToArray();
        }

        var successCount = 0;

        for (int i = 0; i < serviceCount; i++)
        {
            var port = startPort + i;
            var gpuId = i < gpuIds.Length ? gpuIds[i] : "0";
            var serviceId = $"tts_service_{i}";

            progress?.Report($"\n[{i + 1}/{serviceCount}] 启动服务 {serviceId}...");
            progress?.Report($"  GPU ID: {gpuId}");
            progress?.Report($"  端口: {port}");

            var success = await StartServiceAsync(serviceId, gpuId, port);
            if (success)
            {
                successCount++;
                progress?.Report($"  ✓ 服务启动成功");
            }
            else
            {
                progress?.Error($"  ✗ 服务启动失败");
            }

            await Task.Delay(2000);
        }

        progress?.Report($"\n[TTSServiceManager] 服务启动完成: {successCount}/{serviceCount}");

        await CheckAllServicesHealthAsync();

        return successCount > 0;
    }

    public async Task<bool> AutoDetectAndStartServicesAsync(int serviceCount = 5, int startPort = 8000)
    {
        progress?.Report($"[TTSServiceManager] 检测TTS服务...");
        progress?.Report($"目标服务数量: {serviceCount}");
        progress?.Report($"起始端口: {startPort}");

        var detectedServices = new List<TTSServiceInstance>();

        for (int i = 0; i < serviceCount; i++)
        {
            var port = startPort + i;
            var serviceId = $"tts_service_{i}";

            progress?.Report($"\n[{i + 1}/{serviceCount}] 检查服务 {serviceId}...");
            progress?.Report($"  端口: {port}");

            if (IsPortInUse(port))
            {
                progress?.Report($"  端口 {port} 已被占用，检查是否为TTS服务...");

                var detectedUrl = await IsTTSServiceRunning(port);
                if (detectedUrl != null)
                {
                    progress?.Report($"  ✓ 检测到TTS服务已在运行");
                    var existingService = new TTSServiceInstance
                    {
                        ServiceId = serviceId,
                        GpuId = "0",
                        Port = port,
                        ApiUrl = detectedUrl,
                        Process = null,
                        IsRunning = true,
                        StartTime = DateTime.Now,
                        IsHealthy = true,
                        LastHealthCheck = DateTime.Now
                    };

                    lock (_lock)
                    {
                        _services[serviceId] = existingService;
                    }

                    detectedServices.Add(existingService);
                }
                else
                {
                    progress?.Report($"  端口 {port} 被其他程序占用，但非TTS服务");
                    progress?.Report($"  跳过此端口，继续检测下一个...");
                }
            }
            else
            {
                progress?.Report($"  端口 {port} 未被占用，跳过");
            }
        }

        progress?.Report($"\n[TTSServiceManager] 服务检测完成");
        progress?.Report($"检测到 {detectedServices.Count} 个运行中的服务");

        if (detectedServices.Count > 0)
        {
            await CheckAllServicesHealthAsync();
        }
        else
        {
            progress?.Report("[警告] 未检测到任何运行中的TTS服务");
        }

        return detectedServices.Count > 0;
    }

    private List<int> DetectAvailableGPUs()
    {
        var availableGPUs = new List<int>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=index --format=csv,noheader",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (int.TryParse(line.Trim(), out var gpuId))
                        {
                            availableGPUs.Add(gpuId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            progress?.Error($"GPU检测失败: {ex.Message}");
        }

        return availableGPUs;
    }

    private bool IsPortInUse(int port)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = ipGlobalProperties.GetActiveTcpListeners();

            return tcpConnections.Any(x => x.Port == port);
        }
        catch (Exception ex)
        {
            progress?.Error($"[警告] 端口检测失败: {ex.Message}");
            return false;
        }
    }

    private async Task<string?> IsTTSServiceRunning(int port)
    {
        try
        {
            var testUrl = $"http://127.0.0.1:{port}/";

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
            var testResponse = await _httpClient.GetAsync(testUrl, cts.Token);
            if (testResponse.IsSuccessStatusCode)
            {
                progress?.Report($"  [IsTTSServiceRunning] 检测到服务: {testUrl}");
                return $"http://127.0.0.1:{port}/generate";
            }
        }
        catch
        {
        }
        return null;
    }

    private async Task<bool> StartServiceAsync(string serviceId, string gpuId, int port)
    {
        lock (_lock)
        {
            if (_services.ContainsKey(serviceId))
            {
                progress?.Report($"  警告: 服务 {serviceId} 已存在");
                return false;
            }
        }

        try
        {
            var rootDir = Path.GetFullPath(Path.Combine(_baseDirectory, "..", "..", "..", ".."));
            var apiPyPath = Path.Combine(rootDir, "api.py");
            var uvPath = Path.Combine(rootDir, "uv.exe");

            if (!File.Exists(apiPyPath))
            {
                progress?.Error($"  错误: api.py 不存在: {apiPyPath}");
                return false;
            }

            if (!File.Exists(uvPath))
            {
                progress?.Error($"  错误: uv.exe 不存在: {uvPath}");
                return false;
            }

            var device = $"cuda:{gpuId}";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = uvPath,
                Arguments = $"run -- api.py --mode api --device {device} --api-port {port}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = rootDir
            };

            startInfo.Environment["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "true";
            startInfo.Environment["HF_HOME"] = Path.Combine(rootDir, "checkpoints");
            startInfo.Environment["MODELSCOPE_CACHE"] = Path.Combine(rootDir, "checkpoints");
            startInfo.Environment["UV_INDEX_URL"] = "https://mirrors.aliyun.com/pypi/simple/";
            startInfo.Environment["UV_TRUSTED_HOST"] = "mirrors.aliyun.com";
            startInfo.Environment["UV_PYPI_INDEX_URL"] = "https://mirrors.aliyun.com/pypi/simple/";
            startInfo.Environment["HF_ENDPOINT"] = "https://hf-mirror.com";
            startInfo.Environment["HUGGINGFACE_HUB_CACHE"] = Path.Combine(rootDir, "checkpoints");
            startInfo.Environment["TRANSFORMERS_CACHE"] = Path.Combine(rootDir, "checkpoints");
            startInfo.Environment["MODELSCOPE_DOMAIN"] = "www.modelscope.cn";
            startInfo.Environment["MODELSCOPE_CACHE"] = Path.Combine(rootDir, "checkpoints");
            startInfo.Environment["HF_HUB_ENABLE_HF_TRANSFER"] = "1";

            var process = Process.Start(startInfo);
            if (process == null)
            {
                progress?.Report($"  错误: 无法启动进程");
                return false;
            }

            var service = new TTSServiceInstance
            {
                ServiceId = serviceId,
                GpuId = gpuId,
                Port = port,
                ApiUrl = $"http://127.0.0.1:{port}/generate",
                Process = process,
                IsRunning = true,
                StartTime = DateTime.Now,
                IsHealthy = false,
                LastHealthCheck = DateTime.MinValue
            };

            lock (_lock)
            {
                _services[serviceId] = service;
            }

            progress?.Report($"  进程ID: {process.Id}");
            progress?.Report($"  等待服务启动...");

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line != null)
                    {
                        progress?.Report($"  [输出] {line}");
                        outputBuilder.AppendLine(line);
                    }
                }
            });

            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (line != null)
                    {
                        progress?.Report($"  [错误] {line}");
                        errorBuilder.AppendLine(line);
                    }
                }
            });

            var healthCheckTask = Task.Run(async () =>
            {
                await Task.Delay(5000);
                var isHealthy = await CheckServiceHealthAsync(serviceId);
                return isHealthy;
            });

            await Task.WhenAny(healthCheckTask, Task.Delay(60000));

            var isHealthy = await healthCheckTask;
            service.IsHealthy = isHealthy;

            if (!isHealthy)
            {
                if (process.HasExited)
                {
                    progress?.Report($"  进程已退出，退出代码: {process.ExitCode}");
                }
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            progress?.Error($"  错误: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopAllServicesAsync()
    {
        progress?.Report($"[TTSServiceManager] 停止所有服务...");

        var serviceIds = _services.Keys.ToList();
        var successCount = 0;

        foreach (var serviceId in serviceIds)
        {
            var success = await StopServiceAsync(serviceId);
            if (success) successCount++;
        }

        progress?.Report($"[TTSServiceManager] 已停止 {successCount}/{serviceIds.Count} 个服务");
        return successCount == serviceIds.Count;
    }

    public async Task<bool> StopServiceAsync(string serviceId)
    {
        lock (_lock)
        {
            if (!_services.TryGetValue(serviceId, out var service))
            {
                progress?.Report($"  警告: 服务 {serviceId} 不存在");
                return false;
            }

            if (service.Process != null && !service.Process.HasExited)
            {
                try
                {
                    service.Process.Kill(entireProcessTree: true);
                    service.Process.WaitForExit(5000);
                    progress?.Report($"  已停止服务 {serviceId}");
                }
                catch (Exception ex)
                {
                    progress?.Warning($"  停止服务失败: {ex.Message}");
                }
            }

            service.IsRunning = false;
            service.IsHealthy = false;
            _services.Remove(serviceId);
        }

        return true;
    }

    public async Task<bool> CheckServiceHealthAsync(string serviceId)
    {
        TTSServiceInstance? service;
        lock (_lock)
        {
            if (!_services.TryGetValue(serviceId, out service))
            {
                return false;
            }
        }

        if (service == null)
        {
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync(service.ApiUrl.Replace("/generate", "/"));
            var isHealthy = response.IsSuccessStatusCode;

            lock (_lock)
            {
                service.IsHealthy = isHealthy;
                service.LastHealthCheck = DateTime.Now;
            }

            return isHealthy;
        }
        catch
        {
            lock (_lock)
            {
                service.IsHealthy = false;
                service.LastHealthCheck = DateTime.Now;
            }
            return false;
        }
    }

    public async Task<bool> CheckAllServicesHealthAsync()
    {
        progress?.Report($"[TTSServiceManager] 检查所有服务健康状态...");

        var tasks = _services.Values.Select(s => CheckServiceHealthAsync(s.ServiceId)).ToList();
        await Task.WhenAll(tasks);

        var healthyCount = _services.Values.Count(s => s.IsHealthy);
        var totalCount = _services.Count;

        progress?.Report($"[TTSServiceManager] 健康服务: {healthyCount}/{totalCount}");

        foreach (var service in _services.Values)
        {
            var status = service.IsHealthy ? "✓" : "✗";
            progress?.Report($"  {status} {service.ServiceId} (GPU: {service.GpuId}, Port: {service.Port})");
        }

        return healthyCount > 0;
    }

    public List<TTSServiceInstance> GetRunningServices()
    {
        lock (_lock)
        {
            return _services.Values.Where(s => s.IsRunning && s.IsHealthy).ToList();
        }
    }

    public TTSServiceInstance? GetServiceById(string serviceId)
    {
        lock (_lock)
        {
            return _services.TryGetValue(serviceId, out var service) ? service : null;
        }
    }

    public TTSServiceInstance? GetLeastBusyService()
    {
        lock (_lock)
        {
            var healthyServices = _services.Values.Where(s => s.IsRunning && s.IsHealthy).ToList();
            if (healthyServices.Count == 0)
            {
                return null;
            }

            return healthyServices.OrderBy(s => s.TaskCount).First();
        }
    }

    public async Task<bool> RestartServiceAsync(string serviceId)
    {
        progress?.Report($"[TTSServiceManager] 重启服务 {serviceId}...");

        lock (_lock)
        {
            if (!_services.TryGetValue(serviceId, out var service))
            {
                progress?.Report($"  警告: 服务 {serviceId} 不存在");
                return false;
            }
        }

        var gpuId = _services[serviceId].GpuId;
        var port = _services[serviceId].Port;

        await StopServiceAsync(serviceId);
        await Task.Delay(2000);

        return await StartServiceAsync(serviceId, gpuId, port);
    }

    public string GetServiceStatus()
    {
        lock (_lock)
        {
            if (_services.Count == 0)
            {
                return "没有运行中的服务";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"TTS服务状态 ({_services.Count} 个服务):");
            sb.AppendLine(new string('-', 60));

            foreach (var service in _services.Values.OrderBy(s => s.ServiceId))
            {
                var status = service.IsRunning ? (service.IsHealthy ? "运行中 ✓" : "运行中 (不健康) ✗") : "已停止";
                var uptime = service.IsRunning ? (DateTime.Now - service.StartTime).ToString(@"hh\:mm\:ss") : "N/A";

                sb.AppendLine($"  {service.ServiceId}:");
                sb.AppendLine($"    状态: {status}");
                sb.AppendLine($"    GPU: {service.GpuId}");
                sb.AppendLine($"    端口: {service.Port}");
                sb.AppendLine($"    API: {service.ApiUrl}");
                sb.AppendLine($"    运行时间: {uptime}");
                sb.AppendLine($"    任务数: {service.TaskCount} (成功: {service.SuccessCount}, 失败: {service.FailureCount})");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public void MarkServiceAsUnhealthy(string serviceId)
    {
        lock (_lock)
        {
            if (_services.TryGetValue(serviceId, out var service))
            {
                service.IsHealthy = false;
                service.FailureCount++;
                service.LastHealthCheck = DateTime.Now;
            }
        }
    }
}
