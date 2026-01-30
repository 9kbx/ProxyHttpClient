using Microsoft.Extensions.DependencyInjection;

namespace ProxyHttpClient;

public class ProxyHttpClientFactory(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
{
    /// <summary>
    /// 获取强类型客户端：融合动态代理与业务配置
    /// </summary>
    public T GetTypedClient<T>(ProxyConfig? config = null) where T : class
    {
        var client = CreateClient(config);

        // 注入业务层定义的固定配置 (如BaseAddress, Headers)
        if (ProxyConfigRegistry.TypedClientConfigs.TryGetValue(typeof(T), out var configure))
            configure(client);

        // 通过 DI 容器实例化强类型客户端，自动注入上述 HttpClient
        return ActivatorUtilities.CreateInstance<T>(serviceProvider, client);
    }

    /// <summary>
    /// 获取一个绑定了特定代理的 HttpClient
    /// </summary>
    /// <param name="proxy"></param>
    /// <returns></returns>
    public HttpClient CreateClient(ProxyConfig? proxy = null)
    {
        return CreateClient(Consts.DefaultClientConfigName, proxy);
    }

    /// <summary>
    /// 获取一个绑定了特定代理的 HttpClient
    /// </summary>
    /// <param name="clientName"></param>
    /// <param name="proxy"></param>
    /// <returns></returns>
    public HttpClient CreateClient(string clientName, ProxyConfig? proxy = null)
    {
        HttpClient client;
        // var proxyName = proxy?.GetCacheKey() ?? Consts.DefaultProxyConfigName;

        if (proxy == null)
        {
            client = httpClientFactory.CreateClient();
        }
        else
        {
            var proxyName = proxy.GetCacheKey();
            ProxyConfigRegistry.ProxyConfigs.TryAdd(proxyName, proxy);
            client = httpClientFactory.CreateClient(proxyName);
        }

        if (!string.IsNullOrEmpty(clientName) &&
            ProxyConfigRegistry.NamedClientConfigs.TryGetValue(clientName, out var configure))
        {
            configure(client);
        }

        return client;
    }
}