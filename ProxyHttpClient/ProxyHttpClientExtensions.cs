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
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddProxyHttpClient(this IServiceCollection services,
        Action<HttpClient>? configure = null)
    {
        services.AddHttpClient();
        services.AddSingleton<ProxyHttpClientFactory>();
        services.AddSingleton<IPostConfigureOptions<HttpClientFactoryOptions>, UniversalProxyPostConfigure>();
        if (configure != null)
        {
            ProxyConfigRegistry.NamedClientConfigs[Consts.DefaultClientConfigName] = configure;
        }

        return services;
    }

    /// <summary>
    /// 注册强类型客户端的业务配置
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddTypedHttpClient<T>(this IServiceCollection services,
        Action<HttpClient> configure)
    {
        ProxyConfigRegistry.TypedClientConfigs[typeof(T)] = configure;
        return services;
    }

    /// <summary>
    /// 注册命名客户端的业务配置
    /// </summary>
    /// <param name="services"></param>
    /// <param name="name"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddNamedHttpClient(this IServiceCollection services, string name,
        Action<HttpClient> configure)
    {
        ProxyConfigRegistry.NamedClientConfigs[name] = configure;
        return services;
    }
}