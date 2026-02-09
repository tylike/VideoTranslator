using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using VideoTranslator.Utils;
using VideoTranslator.Interfaces;
using VT.Module.Services;
using PublishToBilibili.Interfaces;
using PublishToBilibili.Services;
using PublishToBilibili.Models;
using VideoEditor.Services;
using VT.Module;
using IWindowService = PublishToBilibili.Interfaces.IWindowService;

namespace VideoEditor;

public partial class App : Application
{
    private static Serilog.ILogger? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            
            LoggerService.Initialize();
            _logger = Log.ForContext<App>();
            
            TimeLine.Services.LoggerService.Initialize();

            LibVLCSharp.Shared.Core.Initialize();
            
            _logger.Information("VideoEditor 应用程序启动");
            
            SetupGlobalExceptionHandling();
            
            #region 初始化基础服务容器
            
            InitializeBaseServices();
            
            #endregion
            
            #region 创建并显示主窗口
            
            var mainWindow = new MainWindow();
            InitializeProgressService(mainWindow);
            mainWindow.Show();
            _logger.Information("主窗口已显示");
            
            #endregion
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "应用程序启动失败");
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.Information("VideoEditor 应用程序退出");
            ServiceHelper.Dispose();
           
            
            base.OnExit(e);
            
            TimeLine.Services.LoggerService.CloseAndFlush();
            LoggerService.Close();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "应用程序退出时发生错误");
        }
    }

    private void SetupGlobalExceptionHandling()
    {
        _logger.Information("设置全局异常处理");

        #region UI线程异常处理
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        _logger.Information("已注册 DispatcherUnhandledException 事件");
        #endregion

        #region 非UI线程异常处理
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        _logger.Information("已注册 AppDomain.UnhandledException 事件");
        #endregion

        #region 任务调度器异常处理
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        _logger.Information("已注册 TaskScheduler.UnobservedTaskException 事件");
        #endregion
    }

    private void InitializeBaseServices()
    {
        try
        {
            _logger.Information("开始初始化服务容器");
            
            ServiceHelper.InitializeServices(services =>
            {
                services.AddSingleton<IProgressService, VideoEditorProgressService>();
                services.AddSingleton<IWindowService, WindowService>();
                services.AddSingleton<IBilibiliPublishService, VideoEditorBilibiliPublishService>();
                services.AddSingleton<IProcessService, ProcessService>();
            });
            
            _logger.Information("服务容器初始化完成");
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "初始化服务容器失败");
            //MessageBox.Show($"初始化服务容器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void InitializeProgressService(MainWindow mainWindow)
    {
        try
        {
            var progressService = ServiceHelper.GetService<IProgressService>();
            if (progressService is VideoEditorProgressService videoEditorProgressService)
            {
                videoEditorProgressService.Initialize(mainWindow);
                _logger.Information("VideoEditorProgressService 已初始化");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "初始化ProgressService失败");
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error(e.Exception, "未处理的UI线程异常");
        _logger?.Error($"异常类型: {e.Exception.GetType().FullName}");
        _logger?.Error($"异常消息: {e.Exception.Message}");
        _logger?.Error($"堆栈跟踪: {e.Exception.StackTrace}");
        
        if (e.Exception.InnerException != null)
        {
            _logger?.Error($"内部异常: {e.Exception.InnerException.GetType().FullName}");
            _logger?.Error($"内部异常消息: {e.Exception.InnerException.Message}");
            _logger?.Error($"内部异常堆栈: {e.Exception.InnerException.StackTrace}");
        }
        
        MessageBox.Show($"发生未处理的异常: {e.Exception.Message}\n\n详细信息已记录到日志文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        
        _logger?.Error(exception, "未处理的非UI线程异常");
        _logger?.Error($"是否正在终止: {e.IsTerminating}");
        _logger?.Error($"异常类型: {exception?.GetType().FullName}");
        _logger?.Error($"异常消息: {exception?.Message}");
        _logger?.Error($"堆栈跟踪: {exception?.StackTrace}");
        
        if (exception?.InnerException != null)
        {
            _logger?.Error($"内部异常: {exception.InnerException.GetType().FullName}");
            _logger?.Error($"内部异常消息: {exception.InnerException.Message}");
            _logger?.Error($"内部异常堆栈: {exception.InnerException.StackTrace}");
        }
        
        if (e.IsTerminating)
        {
            _logger?.Fatal("应用程序即将因未处理异常而终止");
            MessageBox.Show($"发生严重的未处理异常，应用程序即将终止:\n{exception?.Message}\n\n详细信息已记录到日志文件。", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            _logger?.Error("应用程序将继续运行");
            MessageBox.Show($"发生未处理的异常:\n{exception?.Message}\n\n详细信息已记录到日志文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.Error(e.Exception, "未观察到的任务异常");
        _logger?.Error($"异常类型: {e.Exception.GetType().FullName}");
        _logger?.Error($"异常消息: {e.Exception.Message}");
        _logger?.Error($"堆栈跟踪: {e.Exception.StackTrace}");
        
        if (e.Exception.InnerException != null)
        {
            _logger?.Error($"内部异常: {e.Exception.InnerException.GetType().FullName}");
            _logger?.Error($"内部异常消息: {e.Exception.InnerException.Message}");
            _logger?.Error($"内部异常堆栈: {e.Exception.InnerException.StackTrace}");
        }
        
        e.SetObserved();
    }
}
