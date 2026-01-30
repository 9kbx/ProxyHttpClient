using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

public static class ProxyLibraryExtensions
{
    public static IServiceCollection AddUniversalProxySupport(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<ProxyHttpClientFactory>();
        
        // 注册一个全局的动态配置，处理所有带代理标识的请求
        services.AddSingleton<IPostConfigureOptions<HttpClientFactoryOptions>, UniversalProxyPostConfigure>();
        
        return services;
    }
}