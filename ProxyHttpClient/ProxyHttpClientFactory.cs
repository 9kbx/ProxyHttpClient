using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyHttpClient;

public class ProxyHttpClientFactory(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<Type, ObjectFactory> TypeFactoryCache = new();

    /// <summary>
    /// 获取客户端
    /// </summary>
    /// <param name="clientName"></param>
    /// <param name="proxy"></param>
    /// <param name="handlerAction"></param>
    /// <returns></returns>
    public HttpClient CreateClient(string clientName, ProxyConfig? proxy = null,
        Action<SocketsHttpHandler>? handlerAction = null)
    {
        // 1. 生成复合 Key: "proxy_http_client:1.1.1.1:80:config:MyIpClient"
        // 这样既能触发拦截器(前缀匹配)，又能区分不同的业务 Handler 配置
        var compositeKey = proxy != null
            ? $"{proxy.GetCacheKey()}:config:{clientName}"
            : $"{Consts.ProxyCachePrefixKey}:config:{clientName}";

        if (proxy != null)
        {
            // 存储代理配置，Key 必须是复合 Key
            ProxyConfigRegistry.ProxyConfigs.TryAdd(compositeKey, proxy);

            // 建立 复合Key -> 业务配置名 的映射，方便拦截器查找 SocketsHttpHandler 配置
            ProxyConfigRegistry.HandlerMapping[compositeKey] = clientName;
        }

        if (handlerAction != null)
        {
            // 允许运行时临时覆盖或增加 Handler 配置
            ProxyConfigRegistry.SocketsHttpHandlers[clientName] = handlerAction;
        }

        // 2. 触发 IHttpClientFactory。如果是第一次见这个复合 Key，会执行 PostConfigure
        var client = httpClientFactory.CreateClient(compositeKey);

        // 3. 应用 HttpClient 业务配置 (BaseAddress, Headers 等)
        if (ProxyConfigRegistry.NamedClientConfigs.TryGetValue(clientName, out var configureHttpClient))
        {
            configureHttpClient(client);
        }

        return client;
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

        // 优化：使用预编译工厂提升高并发下的实例化性能
        var factory = TypeFactoryCache.GetOrAdd(typeof(T), _ =>
            ActivatorUtilities.CreateFactory(typeof(T), [typeof(HttpClient)]));

        return (T)factory(serviceProvider, [client]);
    }

    /// <summary>
    /// 获取一个绑定了特定代理的 HttpClient
    /// </summary>
    /// <param name="proxy"></param>
    /// <returns></returns>
    public HttpClient CreateClient(ProxyConfig? proxy = null) 
        => CreateClient(Consts.DefaultClientName, proxy);
}