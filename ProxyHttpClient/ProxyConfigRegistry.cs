using System.Collections.Concurrent;

namespace ProxyHttpClient;

internal static class ProxyConfigRegistry
{
    /// <summary>
    /// 代理列表缓存
    /// </summary>
    public static readonly ConcurrentDictionary<string, ProxyConfig> ProxyConfigs = new();

    /// <summary>
    /// 命名客户端配置缓存
    /// </summary>
    public static readonly ConcurrentDictionary<string, Action<HttpClient>> NamedClientConfigs = new();

    /// <summary>
    /// SocketsHttpHandler 的配置缓存 (如SSL, Timeouts, etc.)
    /// </summary>
    public static readonly ConcurrentDictionary<string, Action<SocketsHttpHandler>> SocketsHttpHandlers = new();
}