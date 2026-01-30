using System.Collections.Concurrent;

namespace ProxyHttpClient;

internal static class ProxyConfigRegistry
{
    public static readonly ConcurrentDictionary<string, ProxyConfig> ProxyConfigs = new();
    public static readonly ConcurrentDictionary<string, Action<HttpClient>> NamedClientConfigs = new();
    public static readonly ConcurrentDictionary<Type, Action<HttpClient>> TypedClientConfigs = new();
}