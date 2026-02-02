namespace ProxyHttpClient;

/// <summary>
/// 
/// </summary>
public static class HttpRequestOptionsExtensions
{
    /// <summary>
    /// 请求上下文元数据代理配置键
    /// </summary>
    public static readonly HttpRequestOptionsKey<ProxyConfig> CurrentProxy = new("CurrentProxy");
}