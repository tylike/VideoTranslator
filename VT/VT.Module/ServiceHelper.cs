using DevExpress.ExpressApp;
using DevExpress.ExpressApp.MiddleTier;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VideoTranslator.Utils;
using VT.Module.BusinessObjects;

namespace VT.Module;

/// <summary>
/// 服务提供者静态访问类，用于在应用程序中访问已注册的服务
/// </summary>
public static class ServiceHelper
{
    private static IServiceProvider? ServiceProvider { get => field; set { field = value; VideoTranslator.Services.ServiceBase.ServiceProvider = value; } }

    private static ServiceCollection services;
    //private static AppSettings? _appSettings;
    private static readonly object _lock = new object();

    public static IFFmpegService FFmpeg { get => ServiceProvider.GetRequiredService<IFFmpegService>(); }
    public static WhisperRecognitionService Whisper => ServiceProvider.GetRequiredService<WhisperRecognitionService>();
    #region AppSettings管理
    /// <summary>
    /// 获取应用程序设置（单例模式）
    /// </summary>
    public static PathConfig AppSettings
    {
        get
        {
            return ConfigurationService.Configuration.VideoTranslator.Paths;
        }
    }


    #endregion

    #region ServiceProvider管理
    /// <summary>
    /// 获取主服务提供者（用于设置 Session.ServiceProvider）
    /// </summary>
    public static IServiceProvider GetMainServiceProvider()
    {
        if (ServiceProvider == null)
        {
            Log.Error("主 ServiceProvider 未初始化");
            throw new InvalidOperationException("主 ServiceProvider 未初始化");
        }
        return ServiceProvider;
    }

    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if (ServiceProvider == null)
        {
            Log.Error("ServiceProvider 未初始化，无法获取服务: {ServiceType}", typeof(T).Name);
            throw new InvalidOperationException("ServiceProvider 未初始化");
        }

        var service = ServiceProvider.GetService<T>();
        if (service == null)
        {
            Log.Error("未找到服务: {ServiceType}", typeof(T).Name);
            throw new InvalidOperationException($"未找到服务: {typeof(T).Name}");
        }

        Log.Debug("成功获取服务: {ServiceType}", typeof(T).Name);
        return service;
    }

    /// <summary>
    /// 尝试获取指定类型的服务，如果不存在则返回null
    /// </summary>
    public static T? TryGetService<T>() where T : class
    {
        if (ServiceProvider == null)
        {
            Log.Warning("ServiceProvider 未初始化，无法获取服务: {ServiceType}", typeof(T).Name);
            return null;
        }

        var service = ServiceProvider.GetService<T>();
        if (service == null)
        {
            Log.Warning("未找到服务: {ServiceType}", typeof(T).Name);
        }
        else
        {
            Log.Debug("成功获取服务: {ServiceType}", typeof(T).Name);
        }

        return service;
    }

    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    public static object GetService(Type serviceType)
    {
        if (ServiceProvider == null)
        {
            Log.Error("ServiceProvider 未初始化，无法获取服务: {ServiceType}", serviceType.Name);
            throw new InvalidOperationException("ServiceProvider 未初始化");
        }

        var service = ServiceProvider.GetService(serviceType);
        if (service == null)
        {
            Log.Error("未找到服务: {ServiceType}", serviceType.Name);
            throw new InvalidOperationException($"未找到服务: {serviceType.Name}");
        }

        Log.Debug("成功获取服务: {ServiceType}", serviceType.Name);
        return service;
    }

    #endregion

    #region 服务初始化

    /// <summary>
    /// 初始化服务容器
    /// </summary>
    /// <param name="configureServices">配置服务的回调函数</param>
    /// <returns>配置好的服务提供者</returns>
    public static IServiceProvider InitializeServices(Action<IServiceCollection>? configureServices = null)
    {
        Log.Information("开始初始化服务容器");

        services = new ServiceCollection();

        #region 注册基础服务

        services.AddVideoTranslatorServices(registerProgressService: false);

        #endregion

        #region 注册应用程序特定服务

        configureServices?.Invoke(services);

        #endregion

        ServiceProvider = services.BuildServiceProvider();

        Log.Information("服务容器初始化完成");
        return ServiceProvider;
    }

    #endregion

    public static void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }


    //}
    //public static class ServiceExtendesion
    //{
    //static object _lock = new object();
    private static XPObjectSpaceProvider? _xpoObjectSpaceProvider;

    /// <summary>
    /// 重置XPObjectSpaceProvider（用于连接字符串变化时）
    /// </summary>
    public static void ResetXpoObjectSpaceProvider()
    {
        lock (_lock)
        {
            if (_xpoObjectSpaceProvider != null)
            {
                Log.Information("重置XPObjectSpaceProvider");
                _xpoObjectSpaceProvider = null;
            }
        }
    }
    public static IObjectSpace CreateObjectSpace()
    {
        var xpoObjectSpaceProvider = GetXpoObjectSpaceProvider();
        return xpoObjectSpaceProvider.CreateObjectSpace();
    }

    /// <summary>
    /// 获取XPObjectSpaceProvider（单例模式）
    /// 使用主服务容器，确保VideoProject可以访问所有注册的服务
    /// </summary>
    public static XPObjectSpaceProvider GetXpoObjectSpaceProvider()
    {
        //lock (_lock)
        {
            if (_xpoObjectSpaceProvider == null)
            {
                var _serviceProvider = ServiceHelper.GetMainServiceProvider();
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("主ServiceProvider未初始化!");
                }

                var connectionString = ConfigurationService.Configuration.Database.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("未找到数据库连接字符串");
                }

                XpoTypesInfoHelper.GetXpoTypeInfoSource();
                XafTypesInfo.Instance.RegisterEntity(typeof(VideoProject));

                _xpoObjectSpaceProvider = new XPObjectSpaceProvider(_serviceProvider, connectionString);
                Log.Information("创建XPObjectSpaceProvider，连接字符串: {ConnectionString}", connectionString);
            }
            return _xpoObjectSpaceProvider;
        }
    }

}