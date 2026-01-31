using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

public static class ProxyHttpClientExtensions
{
    /// <summary>
    /// 注册通用代理
    /// </summary>
    /// <param name="services"></param>
    /// <param name="clientAction">全局默认 HttpClient 配置</param>
    /// <param name="handlerAction">全局默认 Handler 配置</param>
    /// <returns></returns>
    public static IServiceCollection AddProxyHttpClient(this IServiceCollection services,
        Action<HttpClient>? clientAction = null,
        Action<SocketsHttpHandler>? handlerAction = null)
    {
        services.AddHttpClient();
        services.AddSingleton<ProxyHttpClientFactory>();
        services.AddSingleton<IPostConfigureOptions<HttpClientFactoryOptions>, UniversalProxyPostConfigure>();

        var clientName = Consts.DefaultClientName;
        if (clientAction != null) ProxyConfigRegistry.NamedClientConfigs[clientName] = clientAction;
        if (handlerAction != null) ProxyConfigRegistry.SocketsHttpHandlers[clientName] = handlerAction;

        return services;
    }

    /// <summary>
    /// 注册强类型客户端的业务配置
    /// </summary>
    /// <param name="services"></param>
    /// <param name="clientAction">强类型 HttpClient 配置</param>
    /// <param name="handlerAction">强类型 Handler 配置</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddProxyHttpClient<T>(this IServiceCollection services,
        Action<HttpClient>? clientAction,
        Action<SocketsHttpHandler>? handlerAction = null)
    {
        var clientName = typeof(T).FullName ?? typeof(T).Name;
        if (clientAction != null) ProxyConfigRegistry.NamedClientConfigs[clientName] = clientAction;
        if (handlerAction != null) ProxyConfigRegistry.SocketsHttpHandlers[clientName] = handlerAction;
        return services;
    }

    /// <summary>
    /// 注册命名客户端的业务配置
    /// </summary>
    /// <param name="services"></param>
    /// <param name="clientName"></param>
    /// <param name="clientAction">clientName HttpClient 配置</param>
    /// <param name="handlerAction">clientName Handler 配置</param>
    /// <returns></returns>
    public static IServiceCollection AddProxyHttpClient(this IServiceCollection services, 
        string clientName,
        Action<HttpClient>? clientAction,
        Action<SocketsHttpHandler>? handlerAction = null)
    {
        if (clientAction != null) ProxyConfigRegistry.NamedClientConfigs[clientName] = clientAction;
        if (handlerAction != null) ProxyConfigRegistry.SocketsHttpHandlers[clientName] = handlerAction;
        return services;
    }
}