using System.Collections.Concurrent;

namespace ProxyHttpClient;

internal static class ProxyConfigRegistry
{
    /// <summary>
    /// 代理列表缓存
    /// </summary>
    public static readonly ConcurrentDictionary<string, ProxyConfig> ProxyConfigs = new();
}