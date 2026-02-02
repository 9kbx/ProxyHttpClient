using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

/// <summary>
/// 
/// </summary>
public static class ProxyHttpClientExtensions
{
    private static IServiceCollection AddProxyHttpClientCore(this IServiceCollection services)
    {
        // services.AddHttpClient();
        services.TryAddSingleton<ProxyHttpClientFactory>(); // 防止多次注册产生的副作用
        // services.AddSingleton<IPostConfigureOptions<HttpClientFactoryOptions>, UniversalProxyPostConfigure>();
        // 使用 TryAddEnumerable 注册 IPostConfigureOptions 是标准做法
        services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IPostConfigureOptions<HttpClientFactoryOptions>, UniversalProxyPostConfigure>());
        return services;
    }

    /// <summary>
    /// 注册通用代理
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureClient">全局默认 HttpClient 配置</param>
    /// <returns></returns>
    public static IHttpClientBuilder AddProxyHttpClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
    {
        var clientName = Consts.DefaultClientName;
        return services.AddProxyHttpClient(clientName, configureClient);
    }

    /// <summary>
    /// 注册强类型客户端的业务配置
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureClient">强类型 HttpClient 配置</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IHttpClientBuilder AddProxyHttpClient<T>(
        this IServiceCollection services,
        Action<HttpClient>? configureClient)
    {
        var clientName = typeof(T).FullName ?? typeof(T).Name;
        return services.AddProxyHttpClient(clientName, configureClient);
    }

    /// <summary>
    /// 注册命名客户端的业务配置
    /// </summary>
    /// <param name="services"></param>
    /// <param name="clientName"></param>
    /// <param name="configureClient">clientName HttpClient 配置</param>
    /// <returns></returns>
    public static IHttpClientBuilder AddProxyHttpClient(
        this IServiceCollection services,
        string clientName,
        Action<HttpClient>? configureClient)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentNullException(nameof(clientName));
        services.AddProxyHttpClientCore();
        return configureClient is not null
            ? services.AddHttpClient(clientName, configureClient)
            : services.AddHttpClient(clientName);
    }
}