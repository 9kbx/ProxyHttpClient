using System.Net;

namespace ProxyHttpClient;

/// <summary>
/// 代理配置
/// </summary>
/// <param name="Host"></param>
/// <param name="Port"></param>
/// <param name="UserName"></param>
/// <param name="Password"></param>
public record ProxyConfig(
    string Host,
    int Port,
    string? UserName = null,
    string? Password = null)
{
    /// <summary>
    /// 生成唯一的 Key，用于缓存和连接池分区
    /// </summary>
    /// <returns></returns>
    public string GetCacheKey() =>
        $"{Consts.ProxyCachePrefixKey}:{Host}:{Port}:{UserName ?? "anon"}";

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public WebProxy ToWebProxy() => new($"{Host}:{Port}")
    {
        Credentials = UserName != null ? new NetworkCredential(UserName, Password) : null
    };
}