namespace ProxyHttpClient;

internal class ProxyPropertyHandler(ProxyConfig proxyConfig) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // 将代理地址存入请求上下文
        request.Options.Set(HttpRequestOptionsExtensions.CurrentProxy, proxyConfig);
        return base.SendAsync(request, ct);
    }
}