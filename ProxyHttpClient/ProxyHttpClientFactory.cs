using Microsoft.Extensions.DependencyInjection;

namespace ProxyHttpClient;

public class ProxyHttpClientFactory(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
{
    /// <summary>
    /// 获取一个绑定了特定代理的 HttpClient
    /// </summary>
    /// <param name="proxy"></param>
    /// <returns></returns>
    public HttpClient CreateClient(ProxyConfig? proxy = null)
    {
        var clientName = proxy?.GetCacheKey() ?? Consts.DefaultClientName;
        return CreateClient(clientName, proxy);
    }

    /// <summary>
    /// 获取强类型客户端：融合动态代理与业务配置
    /// </summary>
    /// <param name="proxy"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T CreateClient<T>(ProxyConfig? proxy = null) where T : class
    {
        var clientName = typeof(T).FullName ?? typeof(T).Name;
        var client = CreateClient(clientName, proxy);
        // 通过 DI 容器实例化强类型客户端，自动注入上述 HttpClient
        return ActivatorUtilities.CreateInstance<T>(serviceProvider, client);
    }

    /// <summary>
    /// 获取一个绑定了特定代理的 HttpClient
    /// </summary>
    /// <param name="clientName"></param>
    /// <param name="proxy"></param>
    /// <param name="handlerAction"></param>
    /// <returns></returns>
    public HttpClient CreateClient(string clientName, 
        ProxyConfig? proxy = null,
        Action<SocketsHttpHandler>? handlerAction = null)
    {
        if (proxy != null)
        {
            var proxyName = proxy.GetCacheKey();
            ProxyConfigRegistry.ProxyConfigs.TryAdd(proxyName, proxy);
            // ProxyConfigRegistry.HandlerMapping[proxyName] = clientName;
        }

        if (handlerAction != null)
        {
            ProxyConfigRegistry.SocketsHttpHandlers[clientName] = handlerAction;
        }

        var client = httpClientFactory.CreateClient(clientName);

        // 配置HttpClient
        if (!string.IsNullOrEmpty(clientName) &&
            ProxyConfigRegistry.NamedClientConfigs.TryGetValue(clientName, out var configureHttpClient))
        {
            configureHttpClient(client);
        }

        return client;
    }
}