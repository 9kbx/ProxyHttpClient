using System.Net;

namespace ProxyHttpClient;

public record ProxyConfig(
    string Host, 
    int Port, 
    string? UserName = null, 
    string? Password = null)
{
    // 生成唯一的 Key，用于缓存和连接池分区
    public string GetCacheKey() => 
        $"{Consts.ProxyCachePrefixKey}{Host}:{Port}:{UserName ?? "anon"}";

    public WebProxy ToWebProxy() => new($"{Host}:{Port}")
    {
        Credentials = UserName != null ? new NetworkCredential(UserName, Password) : null
    };
}