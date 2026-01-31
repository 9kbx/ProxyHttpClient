using System.Net;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

internal class UniversalProxyPostConfigure : IPostConfigureOptions<HttpClientFactoryOptions>
{
    public void PostConfigure(string? clientName, HttpClientFactoryOptions options)
    {
        // 只处理由我们类库发起的请求
        if (string.IsNullOrEmpty(clientName) || !clientName.StartsWith(Consts.ProxyCachePrefixKey)) return;

        options.HttpMessageHandlerBuilderActions.Add(builder => { ConfigureWebProxy(builder, clientName, options); });
    }

    private static void ConfigureWebProxy(HttpMessageHandlerBuilder builder, string clientName,
        HttpClientFactoryOptions options)
    {
        // 配置 SocketsHttpHandler
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2), // 性能优化：根据大批量场景调整
            ConnectTimeout = TimeSpan.FromSeconds(10),
        };

        if (ProxyConfigRegistry.SocketsHttpHandlers.TryGetValue(clientName, out var configureHandler))
            configureHandler?.Invoke(handler);
        else if (ProxyConfigRegistry.SocketsHttpHandlers.TryGetValue(Consts.DefaultClientName,
                     out var defaultConfigureHandler))
            defaultConfigureHandler?.Invoke(handler);

        // 配置代理
        if (ProxyConfigRegistry.ProxyConfigs.TryGetValue(clientName, out var config))
        {
            var webProxy = new WebProxy($"{config.Host}:{config.Port}")
            {
                BypassProxyOnLocal = true
            };

            if (!string.IsNullOrEmpty(config.UserName))
                webProxy.Credentials = new NetworkCredential(config.UserName, config.Password);

            handler.Proxy = webProxy;
            handler.UseProxy = true;
        }

        builder.PrimaryHandler = handler;
    }
}