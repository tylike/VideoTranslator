using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using VT.Module.BusinessObjects;
using VideoTranslator.Config;
using DevExpress.XtraRichEdit.Layout.Engine;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DevExpress.ExpressApp.MiddleTier;

namespace VideoEditor;

/// <summary>
/// 服务提供者静态访问类，用于在应用程序中访问已注册的服务
/// </summary>
[Obsolete("应使用vt.module中的servicehelper",true)]public static class ServicesHelperX
{
    public static bool AutoInitializeProvider { get; set; } = true;

    private static IServiceProvider? _serviceProvider;
    private static XPObjectSpaceProvider? _xpoObjectSpaceProvider;
    private static readonly object _lock = new object();

    /// <summary>
    /// 设置服务提供者
    /// </summary>
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Log.Information("ServiceProvider 已设置");
    }

    /// <summary>
    /// 获取主服务提供者（用于设置 Session.ServiceProvider）
    /// </summary>
    public static IServiceProvider GetMainServiceProvider()
    {
        if (_serviceProvider == null)
        {
            if (AutoInitializeProvider)
            {

            }
            Log.Error("主 ServiceProvider 未初始化");
            throw new InvalidOperationException("主 ServiceProvider 未初始化，请先调用 SetServiceProvider 方法");
        }
        return _serviceProvider;
    }

    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if (_serviceProvider == null)
        {
            Log.Error("ServiceProvider 未初始化，无法获取服务: {ServiceType}", typeof(T).Name);
            throw new InvalidOperationException("ServiceProvider 未初始化，请先调用 SetServiceProvider 方法");
        }

        var service = _serviceProvider.GetService<T>();
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
        if (_serviceProvider == null)
        {
            Log.Warning("ServiceProvider 未初始化，无法获取服务: {ServiceType}", typeof(T).Name);
            return null;
        }

        var service = _serviceProvider.GetService<T>();
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
        if (_serviceProvider == null)
        {
            Log.Error("ServiceProvider 未初始化，无法获取服务: {ServiceType}", serviceType.Name);
            throw new InvalidOperationException("ServiceProvider 未初始化，请先调用 SetServiceProvider 方法");
        }

        var service = _serviceProvider.GetService(serviceType);
        if (service == null)
        {
            Log.Error("未找到服务: {ServiceType}", serviceType.Name);
            throw new InvalidOperationException($"未找到服务: {serviceType.Name}");
        }

        Log.Debug("成功获取服务: {ServiceType}", serviceType.Name);
        return service;
    }

    #region xpo

    /// <summary>
    /// 获取XPObjectSpaceProvider（单例模式）
    /// 使用主服务容器，确保VideoProject可以访问所有注册的服务
    /// </summary>
    public static XPObjectSpaceProvider GetXpoObjectSpaceProvider()
    {
        lock (_lock)
        {
            if (_xpoObjectSpaceProvider == null)
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("主ServiceProvider未初始化，请先调用 SetServiceProvider 方法");
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
    #endregion
}
