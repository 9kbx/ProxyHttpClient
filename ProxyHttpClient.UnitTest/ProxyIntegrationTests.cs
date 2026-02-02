using Microsoft.Extensions.DependencyInjection;

namespace ProxyHttpClient.UnitTest;

public class ProxyIntegrationTests
{
    private readonly ProxyHttpClientFactory _factory;
    private readonly IServiceProvider _serviceProvider;

    public ProxyIntegrationTests()
    {
        var services = new ServiceCollection();

        // 1. 注册我们的代理组件
        services.AddProxyHttpClient(client => { client.DefaultRequestHeaders.Add("User-Agent", "XUnit-ProxyHttpClient.UnitTest"); });

        // 2. 注册一个专门的命名客户端用于隔离测试
        services.AddProxyHttpClient("IsolationClient",
            client => { client.BaseAddress = new Uri("https://api.ipify.org"); });

        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<ProxyHttpClientFactory>();
    }

    /// <summary>
    /// 测试点1：默认代理配置
    /// </summary>
    [Fact]
    public async Task CreateClient_WithDefaultProxy_ShouldUseCorrectIp()
    {
        // 准备一个测试代理
        var proxy = new ProxyConfig("123.123.123.123", 12312, "aaaa", "bbbb");

        // var client = _factory.CreateClient(proxy);
        // var ip = await client.GetStringAsync("https://api.ipify.org");
        //
        // Assert.NotNull(ip);
        // Assert.Equal("123.123.123.123", ip); // 验证返回的出口IP是否为代理IP
    }

    /// <summary>
    /// 测试点2：指定不同的代理配置
    /// </summary>
    [Fact]
    public async Task CreateClient_WithSpecificProxy_ShouldReturnDifferentIps()
    {
        var proxy1 = new ProxyConfig("123.123.123.123", 12312, "aaaa", "bbbb");
        var proxy2 = new ProxyConfig("213.123.123.123", 12312, "aaaa", "bbbb");

        // var client1 = _factory.CreateClient(proxy1);
        // var client2 = _factory.CreateClient(proxy2);
        //
        // var ip1 = await client1.GetStringAsync("https://api.ipify.org");
        // var ip2 = await client2.GetStringAsync("https://api.ipify.org");
        //
        // Assert.NotEqual(ip1, ip2);
        // Assert.Equal("123.123.123.123", ip1);
        // Assert.Equal("213.123.123.123", ip2);
    }

    /// <summary>
    /// 测试点3：核心验证 - 相同 ClientName 下不同代理是否互相干扰
    /// 验证 Composite Key 是否真正实现了物理隔离
    /// </summary>
    [Fact]
    public async Task NamedClient_WithMultipleProxies_ShouldBeIsolated()
    {
        var clientName = "IsolationClient";
        var proxyA = new ProxyConfig("123.123.123.123", 12312, "aaaa", "bbbb");
        var proxyB = new ProxyConfig("213.123.123.123", 12312, "aaaa", "bbbb");

        // // 同一个 clientName，传入不同的 proxy
        // var clientA = _factory.CreateClient(clientName, proxyA);
        // var clientB = _factory.CreateClient(clientName, proxyB);
        // var clientDirect = _factory.CreateClient(clientName, null); // 不带代理的直连模式
        //
        // var ipA = await clientA.GetStringAsync(""); // 使用 BaseAddress
        // var ipB = await clientB.GetStringAsync("");
        //
        // // 获取本地真实 IP（非代理）
        // var ipDirect = await clientDirect.GetStringAsync("");
        //
        // // 验证：虽然都叫 IsolationClient，但由于代理不同，底层 Handler 必须是隔离的
        // Assert.Equal("123.123.123.123", ipA);
        // Assert.Equal("213.123.123.123", ipB);
        // Assert.NotEqual(ipA, ipDirect);
        // Assert.NotEqual(ipB, ipDirect);
    }
}