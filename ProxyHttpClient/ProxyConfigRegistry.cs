using System.Collections.Concurrent;

namespace ProxyHttpClient;

internal static class ProxyConfigRegistry
{
    public static readonly ConcurrentDictionary<string, ProxyConfig> Configs = new();
}