namespace ProxyHttpClient;

public class ProxyHttpClientFactory(IHttpClientFactory httpClientFactory)
{
    /// <summary>
    /// 获取一个绑定了特定代理的 HttpClient
    /// </summary>
    public HttpClient CreateClient(ProxyConfig? config = null)
    {
        // 如果配置为空，直接返回 IHttpClientFactory 的默认客户端
        if (config == null) return httpClientFactory.CreateClient();
        
        var key = config.GetCacheKey();
        
        // 暂存配置，供拦截器读取
        ProxyConfigRegistry.Configs.TryAdd(key, config);
        
        // 触发 HttpClient 创建。如果 Handler 已存在且未过期，底层会直接复用
        return httpClientFactory.CreateClient(key);
    }
}