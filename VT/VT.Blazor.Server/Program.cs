using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.DesignTime;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Utils;
using System.Reflection;
using VideoTranslator.Interfaces;

namespace VT.Blazor.Server
{
    public class Program : IDesignTimeApplicationFactory
    {
        static bool ContainsArgument(string[] args, string argument)
        {
            return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
        }
        public static int Main(string[] args)
        {
            if (ContainsArgument(args, "help") || ContainsArgument(args, "h"))
            {
                IProgressService progress = null;
                progress.Report("Updates the database when its version does not match the application's version.");
                
                progress.Report($"    {Assembly.GetExecutingAssembly().GetName().Name}.exe --updateDatabase [--forceUpdate --silent]");
               
                progress.Report("--forceUpdate - Marks that the database must be updated whether its version matches the application's version or not.");
                progress.Report("--silent - Marks that database update proceeds automatically and does not require any interaction with the user.");
             
                progress.Report($"Exit codes: 0 - {DBUpdaterStatus.UpdateCompleted}");
                progress.Report($"            1 - {DBUpdaterStatus.UpdateError}");
                progress.Report($"            2 - {DBUpdaterStatus.UpdateNotNeeded}");
            }
            else
            {
                DevExpress.ExpressApp.FrameworkSettings.DefaultSettingsCompatibilityMode = DevExpress.ExpressApp.FrameworkSettingsCompatibilityMode.Latest;
                IHost host = CreateHostBuilder(args).Build();
                if (ContainsArgument(args, "updateDatabase"))
                {
                    using (var serviceScope = host.Services.CreateScope())
                    {
                        return serviceScope.ServiceProvider.GetRequiredService<DevExpress.ExpressApp.Utils.IDBUpdater>().Update(ContainsArgument(args, "forceUpdate"), ContainsArgument(args, "silent"));
                    }
                }
                else
                {
                    host.Run();
                }
            }
            return 0;
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        XafApplication IDesignTimeApplicationFactory.Create()
        {
            IHostBuilder hostBuilder = CreateHostBuilder(Array.Empty<string>());
            return DesignTimeApplicationFactoryHelper.Create(hostBuilder);
        }
    }
}
