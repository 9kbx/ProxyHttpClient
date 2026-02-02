using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using ProxyHttpClient;
using System.Net.Http;

// 运行测试
BenchmarkRunner.Run<ProxyBenchmark>();

[MemoryDiagnoser] // 开启内存分配诊断
[RankColumn]      // 开启性能排名
public class ProxyBenchmark
{
    private ServiceProvider _serviceProvider;
    private ProxyHttpClientFactory _proxyFactory;
    private IHttpClientFactory _nativeFactory;
    private ProxyConfig _reusableConfig;
    private int _counter = 0;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // 注册你的代理客户端
        services.AddProxyHttpClient("TestClient", client => {
            client.BaseAddress = new System.Uri("https://api.test.com");
            client.DefaultRequestHeaders.Add("X-ProxyHttpClient.UnitTest", "Benchmark");
        }).AddStandardResilienceHandler(); // 加上策略，测试克隆开销

        _serviceProvider = services.BuildServiceProvider();
        _proxyFactory = _serviceProvider.GetRequiredService<ProxyHttpClientFactory>();
        _nativeFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        _reusableConfig = new ProxyConfig("127.0.0.1", 8080, "user", "pass");
    }

    // 场景 1：原生工厂（作为基准）
    [Benchmark(Baseline = true)]
    public HttpClient NativeCreate()
    {
        return _nativeFactory.CreateClient("TestClient");
    }

    // 场景 2：代理客户端工厂（命中缓存）
    // 模拟同一个账号持续使用同一个代理 IP 的高频请求
    [Benchmark]
    public HttpClient ProxyCreateCached()
    {
        return _proxyFactory.CreateClient("TestClient", _reusableConfig);
    }

    // 3. 测试完全新建一个代理配置时的速度（模拟新 IP 加入）
    // 模拟海量爬虫 IP，每一个请求都是一个全新的代理 IP
    // 这将完整触发：Key生成 -> 配置克隆 -> PostConfigure执行 -> Handler实例化
    [Benchmark]
    public HttpClient ProxyCreateNew()
    {
        _counter++;
        // 生成唯一的 IP，例如 10.0.0.1, 10.0.0.2 ...
        // 这样每次对应的 compositeKey 都是全新的
        var dynamicIp = $"10.0.{(_counter >> 8) & 0xFF}.{_counter & 0xFF}";
        var dynamicConfig = new ProxyConfig(dynamicIp, 8888);
        
        return _proxyFactory.CreateClient("TestClient", dynamicConfig);
    }
}