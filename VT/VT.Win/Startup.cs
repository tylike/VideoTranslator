using DevExpress.DataAccess.UI.Wizard;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.ApplicationBuilder;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.XtraEditors;
using VT.Module.Services;
using VT.Win.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Utils;
using VideoTranslator.Services;

namespace VT.Win
{
    public class ApplicationBuilder : IDesignTimeApplicationFactory
    {
        public static WinApplication BuildApplication(string connectionString)
        {
            var builder = WinApplication.CreateBuilder(); 
            var configuration = builder.Configuration;
            var settings = VT.Module.ServiceHelper.AppSettings;
            // Register custom services for Dependency Injection. For more information, refer to following topic: https://docs.devexpress.com/eXpressAppFramework/404430/
            // builder.Services.AddScoped<CustomService>();
            // Register 3rd-party IoC containers (like Autofac, Dryloc, etc.)
            // builder.UseServiceProviderFactory(new DryIocServiceProviderFactory());
            // builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Services.AddSingleton<RibbonTemplateCustomizer>();
            builder.Services.AddSingleton<IProgressService, WinProgressService>();
            builder.Services.AddScoped<IBilibiliPublishService, WindowsBilibiliPublishService>();

            builder.Services.AddVideoTranslatorServices(true);
            builder.Services.AddScoped<IFileDialogService, WindowsFileDialogService>();
            builder.Services.AddScoped<IYouTubeInputDialogService, WindowsYouTubeInputDialogService>();

            builder.UseApplication<VTWindowsFormsApplication>();
            builder.Modules
                .AddCloning()
                .AddConditionalAppearance()
                .AddFileAttachments()
                .AddNotifications()
                .AddOffice()
                .AddTreeListEditors()
                .AddValidation(options =>
                {
                    options.AllowValidationDetailsAccess = false;
                })
                .AddViewVariants()
                .Add<VT.Module.VTModule>()
                .Add<VTWinModule>();
            builder.ObjectSpaceProviders
                .AddXpo((application, options) =>
                {
                    options.ConnectionString = connectionString;
                })
                .AddNonPersistent();
            builder.AddBuildStep(application =>
            {
                application.ConnectionString = connectionString;
#if DEBUG
                if(System.Diagnostics.Debugger.IsAttached && application.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
                    application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
                }
                
#endif
            });
            var winApplication = builder.Build();
            
            return winApplication;
        }

        XafApplication IDesignTimeApplicationFactory.Create()
            => BuildApplication(XafApplication.DesignTimeConnectionString);
    }
}
