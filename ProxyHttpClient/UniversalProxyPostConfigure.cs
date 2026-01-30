using System.Net;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

internal class UniversalProxyPostConfigure : IPostConfigureOptions<HttpClientFactoryOptions>
{
    public void PostConfigure(string? name, HttpClientFactoryOptions options)
    {
        // 只处理由我们类库发起的请求
        if (string.IsNullOrEmpty(name) || !name.StartsWith("universal_proxy:")) return;

        options.HttpMessageHandlerBuilderActions.Add(builder =>
        {
            ConfigureWebProxy(builder, name, options);
        });
    }

    private static void ConfigureWebProxy(HttpMessageHandlerBuilder builder, string name, HttpClientFactoryOptions options)
    {
        // 从注册表中找回完整的配置信息
        if (!ProxyConfigRegistry.Configs.TryGetValue(name, out var config)) return;

        var webProxy = new WebProxy($"{config.Host}:{config.Port}")
        {
            BypassProxyOnLocal = true
        };

        // 注入身份验证信息
        if (!string.IsNullOrEmpty(config.UserName))
        {
            webProxy.Credentials = new NetworkCredential(config.UserName, config.Password);
        }

        builder.PrimaryHandler = new SocketsHttpHandler
        {
            Proxy = webProxy,
            UseProxy = true,
            // 性能优化：根据大批量场景调整
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            // 重要：.NET 9 默认开启空闲连接扫描，有助于及时释放不用的代理连接
        };
    }
}