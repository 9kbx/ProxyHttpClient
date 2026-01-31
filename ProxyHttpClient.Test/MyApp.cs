using System.Net;
using Polly;

namespace ProxyHttpClient.Test;

public class MyApp(ProxyHttpClientFactory clientFactory, MyDbContext dbContext)
{
    public async Task RunAsync()
    {
        // 1. 定义代理（可以是任何来源：数据库、API、配置文件）
        var config = new ProxyConfig("socks5://92.113.218.52", 43321, "7KIzKi0HIka5H6B", "Xfjj92SDiEW93bh");
        // var config = new ProxyConfig("111.222.123.123", 33321, "aaa", "bbb");

        await MyAppClientTestAsync(config); // 强类型客户端测试
        await DefaultHttpClientTestAsync(); // 不使用代理
        await ProxyTestAsync(config);        // 代理测试
        await BatchTestAsync();                  // 批量代理测试
        await PollyTestAsync(config); // 重试策略测试
    }

    private async Task BatchTestAsync()
    {
        var proxies = GetProxies();
        await Parallel.ForEachAsync(proxies, new ParallelOptions()
        {
            MaxDegreeOfParallelism = 10
        }, async (proxyConfig, token) => { await ProxyTestAsync(proxyConfig, token); });
    }

    private async Task ProxyTestAsync(ProxyConfig proxyConfig, CancellationToken stoppingToken = default)
    {
        var client = clientFactory.CreateClient(proxyConfig);
        var response = await client.GetAsync("https://httpbin.org/ip", stoppingToken);
        var rawText = await response.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine(rawText);
    }

    private async Task PollyTestAsync(ProxyConfig proxyConfig, CancellationToken stoppingToken = default)
    {
        var client = clientFactory.CreateClient(proxyConfig);
        client.Timeout = TimeSpan.FromSeconds(3);

        // 在业务层定义特定策略：例如针对下单接口，只重试 1 次
        var localPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.RequestTimeout)
            .Or<TaskCanceledException>() // 捕获 HttpClient.Timeout 抛出的超时异常
            .Or<HttpRequestException>() // 建议同时捕获网络层错误
            // .RetryAsync(1) // 简单的重试
            .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetryAsync: async (outcome, timespan, retryCount, context) =>
                {
                    // 可以在这里判断具体原因
                    var reason = outcome.Exception != null
                        ? outcome.Exception.Message
                        : outcome.Result.StatusCode.ToString();

                    Console.WriteLine($"[第 {retryCount} 次重试] 原因: {reason}");

                    // 模拟从上下文获取数据库对象
                    if (context.TryGetValue("myDb", out var obj2) && obj2 is MyDbContext db)
                        await db.GetOrdersAsync();
                    if (context.TryGetValue("myLog", out var obj))
                        Console.WriteLine($"从上下文获取的对象：{obj}");
                }); // 重试前的处理

        try
        {
            // 执行时传入上下文（比如传入DbContext等）
            // 如果 Policy 以单例模式注册时内部需要调用其他生命周期的服务时必须由上下文传递
            var context = new Context { { "myLog", "123123" }, { "myDb", dbContext } };

            // 使用策略执行请求
            var response = await localPolicy.ExecuteAsync(async (ctx, ct) =>
                    await client.GetAsync("https://httpbin.org/ip", ct)
                , context, stoppingToken);

            // 处理成功逻辑
            if (response.IsSuccessStatusCode)
            {
                var rawText = await response.Content.ReadAsStringAsync(stoppingToken);
                Console.WriteLine(rawText);
            }
        }
        catch (TaskCanceledException ex) when (!stoppingToken.IsCancellationRequested)
        {
            // 如果重试次数用完依然超时，会进到这里
            Console.WriteLine("最终请求还是超时了（客户端定义超时）。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"其他错误: {ex.Message}");
        }
    }

    private async Task DefaultHttpClientTestAsync(CancellationToken stoppingToken = default)
    {
        var client = clientFactory.CreateClient();
        var response = await client.GetAsync("https://httpbin.org/ip", stoppingToken);
        var rawText = await response.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine(rawText);
    }

    private async Task MyAppClientTestAsync(ProxyConfig proxy, CancellationToken stoppingToken = default)
    {
        var defaultClient = clientFactory.CreateClient(proxy);
        var defResponse = await defaultClient.GetAsync("https://api.ipify.org", stoppingToken);
        var myIp = await defResponse.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine($"myIp = {myIp}");

        // 获取一个绑定了特定账号代理、且具备固定 BaseAddress 的强类型客户端
        var myIpClient = clientFactory.CreateClient<MyIpClient>(proxy);
        var myIp1 = await myIpClient.GetIpAsync(stoppingToken);
        Console.WriteLine($"myIp = {myIp1}");

        var myIpClient2 = clientFactory.CreateClient("IpClient", proxy);
        // var myIp2 = await myIpClient2.GetStringAsync("ip", stoppingToken);
        var response = await myIpClient2.GetAsync("ip", stoppingToken);
        var myIp2 = await response.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine($"myIp2 = {myIp2}");
        
        var weatherClient = clientFactory.CreateClient<AviationWeatherClient>(proxy);
        var data = await weatherClient.GetMetarAsync("KMCI", stoppingToken);
        Console.WriteLine($"Weather = {data}");
    }

    private List<ProxyConfig> GetProxies()
    {
        var proxies = new List<ProxyConfig>();

        var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "proxy.txt")).ToList();

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.TrimStart().StartsWith("//") || raw.TrimStart().StartsWith("#"))
                continue;

            var parts = raw.Split(':');
            switch (parts.Length)
            {
                case 2:
                {
                    proxies.Add(new ProxyConfig(parts[0], int.Parse(parts[1])));
                    break;
                }
                case 4:
                {
                    proxies.Add(new ProxyConfig(parts[0], int.Parse(parts[1]), parts[2], parts[3]));
                    break;
                }
            }
        }

        return proxies;
    }
}