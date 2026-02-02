namespace ProxyHttpClient;

/// <summary>
/// 模拟代理请求结果
/// </summary>
public class MockProxyHttpMessageHandler : DelegatingHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 在请求上下文元数据提取代理配置
        if (!request.Options.TryGetValue(HttpRequestOptionsExtensions.CurrentProxy, out var proxyConfig))
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("no proxy")
            };

        // 返回测试数据
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(proxyConfig.Host)
        };
    }
}