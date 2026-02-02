using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ProxyHttpClient;

internal class UniversalProxyPostConfigure(IServiceProvider serviceProvider) : IPostConfigureOptions<HttpClientFactoryOptions>
{
    // 拦截器的执行时机：UniversalProxyPostConfigure 里的委托是在 IHttpClientFactory 第一次创建这个特定 compositeKey 的底层 Handler 时执行的
    
    public void PostConfigure(string? compositeKey, HttpClientFactoryOptions options)
    {
        // 匹配前缀
        if (string.IsNullOrEmpty(compositeKey) || !compositeKey.StartsWith(Consts.ProxyCachePrefixKey)) 
            return;
        
        if (ProxyConfigRegistry.HandlerMapping.TryGetValue(compositeKey, out var clientName))
        {
            // 从原生的配置中提取已经注册给 clientName 的中间件（包括 Polly）
            // 这样复合 Key 就能自动继承你在 AddHttpClient 时注册的所有拦截器
            var factoryOptions = serviceProvider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
            var businessOptions = factoryOptions.Get(clientName);
            
            foreach (var action in businessOptions.HttpMessageHandlerBuilderActions)
            {
                // 我们只继承非 PrimaryHandler 的部分 (即中间件链)
                options.HttpMessageHandlerBuilderActions.Add(action);
            }
        }
        
        // 保持原有的 PrimaryHandler (代理/SSL) 配置逻辑
        options.HttpMessageHandlerBuilderActions.Add(builder =>
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectTimeout = TimeSpan.FromSeconds(10),
            };

            // 获取对应的业务配置名称 (e.g. MyIpClient)
            if (ProxyConfigRegistry.HandlerMapping.TryGetValue(compositeKey, out var businessName))
            {
                if (ProxyConfigRegistry.SocketsHttpHandlers.TryGetValue(businessName, out var action))
                    action?.Invoke(handler);
            }
            // 如果没找到具体业务配置，尝试应用全局默认 Handler 配置
            else if (ProxyConfigRegistry.SocketsHttpHandlers.TryGetValue(Consts.DefaultClientName, out var defaultAction))
            {
                defaultAction?.Invoke(handler);
            }

            // 应用代理配置
            if (ProxyConfigRegistry.ProxyConfigs.TryGetValue(compositeKey, out var config))
            {
                handler.Proxy = config.ToWebProxy();
                handler.UseProxy = true;
            }

            builder.PrimaryHandler = handler;
        });
    }
}