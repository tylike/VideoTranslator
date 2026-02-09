using MudBlazor.Services;
using VideoTranslator.UI.Components;
using VideoTranslator.UI.Tests;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VideoTranslator.Config;
using VideoTranslator.Utils;
using VT.Module;

var runTests = args.Contains("--run-tests");
if (runTests)
{
    progress.Report("检测到 --run-tests 参数，将运行启动前测试");
}

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var settings = ServiceHelper.AppSettings;

builder.Services.AddMudServices();
builder.Services.AddHttpClient("UploadClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5008");
});

builder.Services.AddVideoTranslatorServices(configuration);
builder.Services.AddControllers();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days you may want to change this for production scenarios, see https://aka.ms/aspnetcore/hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

if (runTests)
{
    progress.Report("\n");
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<StartupTests>>();
        var startupTests = new StartupTests(scope.ServiceProvider, logger);
        var allTestsPassed = await startupTests.RunAllTestsAsync();

        if (!allTestsPassed)
        {
            logger.LogError("启动前测试失败，应用程序将退出");
            progress.Report("\n启动前测试失败，应用程序将退出");
            progress.Report("按任意键退出...");
            progress.ReadKey();
            return;
        }

        progress.Report("\n启动前测试完成，应用程序将继续启动...");
        progress.Report("按任意键继续...");
        progress.ReadKey();
    }
}

app.Run();
