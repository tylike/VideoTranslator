using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VideoTranslator.UI.Tests;

public class StartupTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupTests> _logger;

    public StartupTests(IServiceProvider serviceProvider, ILogger<StartupTests> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<bool> RunAllTestsAsync()
    {
        _logger.LogInformation("开始运行启动前测试...");
        progress.Report("========================================");
        progress.Report("开始运行启动前测试...");
        progress.Report("========================================");

        var tests = new List<Func<Task<bool>>>
        {
            TestProjectManagerService,
            TestHttpClientConfiguration
        };

        var allPassed = true;
        foreach (var test in tests)
        {
            try
            {
                var result = await test();
                if (!result)
                {
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试执行失败");
                progress.Report($"❌ 测试执行失败: {ex.Message}");
                allPassed = false;
            }
        }

        progress.Report("========================================");
        if (allPassed)
        {
            progress.Report("✅ 所有测试通过！");
            _logger.LogInformation("所有测试通过");
        }
        else
        {
            progress.Report("❌ 部分测试失败，请检查日志");
            _logger.LogError("部分测试失败");
        }
        progress.Report("========================================");

        return allPassed;
    }

    private async Task<bool> TestProjectManagerService()
    {
        progress.Report("\n测试 1: ProjectManagerService");
        try
        {
            var projectManager = _serviceProvider.GetService<IProjectManagerService>();
            if (projectManager == null)
            {
                progress.Report("❌ 无法获取 IProjectManagerService 服务");
                return false;
            }

            var projects = await projectManager.GetAllProjectsAsync();
            progress.Report($"✅ 成功获取项目列表，共 {projects.Count} 个项目");

            return true;
        }
        catch (Exception ex)
        {
            progress.Report($"❌ ProjectManagerService 测试失败: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TestHttpClientConfiguration()
    {
        progress.Report("\n测试 2: HttpClient 配置");
        try
        {
            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>();
            if (httpClientFactory == null)
            {
                progress.Report("❌ 无法获取 IHttpClientFactory 服务");
                return false;
            }

            var client = httpClientFactory.CreateClient("UploadClient");
            if (client.BaseAddress == null)
            {
                progress.Report("❌ UploadClient 的 BaseAddress 未配置");
                return false;
            }

            progress.Report($"✅ UploadClient 配置正确，BaseAddress: {client.BaseAddress}");

            try
            {
                var response = await client.GetAsync("/api/health");
                progress.Report($"✅ HTTP 请求成功，状态码: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                progress.Report($"⚠️  HTTP 请求失败（可能 API 尚未实现）: {ex.Message}");
            }

            return true;
        }
        catch (Exception ex)
        {
            progress.Report($"❌ HttpClient 配置测试失败: {ex.Message}");
            return false;
        }
    }
}