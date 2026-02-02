using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

internal class UniversalProxyPostConfigure(IServiceProvider serviceProvider)
    : IPostConfigureOptions<HttpClientFactoryOptions>
{
    public void PostConfigure(string? compositeKey, HttpClientFactoryOptions options)
    {
        // 匹配前缀
        if (string.IsNullOrEmpty(compositeKey) || !compositeKey.StartsWith(Consts.ProxyCachePrefixKey))
            return;

        // 从复合 Key 中提取出原始的业务 ClientName
        // 格式: ProxyHttpClient:1.1.1.1:80:anon:config:MyClientName
        var lastIndex = compositeKey.LastIndexOf(":config:", StringComparison.Ordinal);
        if (lastIndex == -1) return;
        var businessClientName = compositeKey.Substring(lastIndex + 8);

        // 获取业务客户端的原始配置
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
        var businessOptions = monitor.Get(businessClientName);

        // 将业务客户端的 Handler 配置（SSL, Resilience 等）克隆到复合 Key 客户端
        foreach (var action in businessOptions.HttpMessageHandlerBuilderActions)
        {
            options.HttpMessageHandlerBuilderActions.Add(action);
        }

        // 向配置链路中添加一个自定义 Action
        // 动态覆盖代理配置，保留用户设置的 SSL、Timeout、Cookie 等
        options.HttpMessageHandlerBuilderActions.Add(builder =>
        {
            // 获取代理配置
            if (!ProxyConfigRegistry.ProxyConfigs.TryGetValue(compositeKey, out var proxyConfig)) return;
            // 执行顺序调为第一个
            builder.AdditionalHandlers.Insert(0, new ProxyPropertyHandler(proxyConfig));

            var webProxy = proxyConfig.ToWebProxy();
            switch (builder.PrimaryHandler)
            {
                case SocketsHttpHandler socketsHttpHandler:
                    socketsHttpHandler.Proxy = webProxy;
                    socketsHttpHandler.UseProxy = true;
                    break;
                case HttpClientHandler httpClientHandler:
                    httpClientHandler.Proxy = webProxy;
                    httpClientHandler.UseProxy = true;
                    break;
                default:
                    // 兜底逻辑：如果用户注册了一个完全自定义的 Handler (非 SocketsHttpHandler)
                    // 代理可能无法直接注入，这里可以抛出异常或记录日志
                    throw new NotSupportedException(
                        "The PrimaryHttpMessageHandler is not SocketsHttpHandler or HttpClientHandler.");
            }

            // 选做：由于 Handler 已经被创建并绑定了 Proxy，可以考虑从 Dictionary 中移除以节省内存
            // 但要注意：如果同一个 Key 被多次触发构建（极罕见），移除会导致后续失败
            // ProxyConfigRegistry.ProxyConfigs.TryRemove(name, out _);
        });

        // 将业务客户端的 HttpClient 配置（BaseAddress, Headers 等）也克隆过去
        foreach (var action in businessOptions.HttpClientActions)
        {
            options.HttpClientActions.Add(action);
        }
    }
}