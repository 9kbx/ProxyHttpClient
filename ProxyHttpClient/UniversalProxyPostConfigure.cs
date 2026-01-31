using System.Net;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

internal class UniversalProxyPostConfigure : IPostConfigureOptions<HttpClientFactoryOptions>
{
    public void PostConfigure(string? compositeKey, HttpClientFactoryOptions options)
    {
        // 匹配前缀
        if (string.IsNullOrEmpty(compositeKey) || !compositeKey.StartsWith(Consts.ProxyCachePrefixKey)) 
            return;

        options.HttpMessageHandlerBuilderActions.Add(builder =>
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectTimeout = TimeSpan.FromSeconds(10),
            };

            // 1. 获取对应的业务配置名称 (e.g. MyIpClient)
            if (ProxyConfigRegistry.HandlerMapping.TryGetValue(compositeKey, out var businessName))
            {
                if (ProxyConfigRegistry.SocketsHttpHandlers.TryGetValue(businessName, out var action))
                    action?.Invoke(handler);
            }
            // 2. 如果没找到具体业务配置，尝试应用全局默认 Handler 配置
            else if (ProxyConfigRegistry.SocketsHttpHandlers.TryGetValue(Consts.DefaultClientName, out var defaultAction))
            {
                defaultAction?.Invoke(handler);
            }

            // 3. 应用代理配置
            if (ProxyConfigRegistry.ProxyConfigs.TryGetValue(compositeKey, out var config))
            {
                handler.Proxy = config.ToWebProxy();
                handler.UseProxy = true;
            }

            builder.PrimaryHandler = handler;
        });
    }
}