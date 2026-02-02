using System.Net;
using Polly;
using Polly.Retry;

namespace ProxyHttpClient.Test;

public class RetryPolicySample(ProxyHttpClientFactory clientFactory, IServiceProvider serviceProvider)
{
    private int _nextIdx = 0;

    private readonly List<ProxyConfig> _proxyConfigs =
    [
        new("socks5://111.123.123.52", 12312, "aaaa", "bbbb"),
        new("222.123.123.115", 12312, "aaaa", "bbbb"),
        new("123.123.123.35", 12312, "aaaa", "bbbb")
    ];

    private ProxyConfig GetNextProxyConfig()
    {
        if (_nextIdx >= _proxyConfigs.Count)
            _nextIdx = 0;
        return _proxyConfigs[_nextIdx++];
    }

    public async Task RunAsync()
    {
        await PollyTestAsync();
        // await PollyTest2Async(_proxyConfigs.First());
    }

    private async Task PollyTestAsync(CancellationToken stoppingToken = default)
    {
        // 定义一个支持“换 IP 重试”的策略
        var pipeline = new ResiliencePipelineBuilder<string>()
            .AddRetry(new RetryStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>()
                    .HandleResult(string.IsNullOrEmpty) // 条件1
                    .Handle<Exception>(), // 条件2

                MaxRetryAttempts = 3,

                // 对应 WaitAndRetryAsync 的指数退避逻辑
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // 建议开启，防止高并发下的“惊群效应”
                Delay = TimeSpan.FromSeconds(2),

                OnRetry = outcome =>
                {
                    var reason = outcome.Outcome.Exception?.Message ?? "返回结果为空";
                    Console.WriteLine($"[第 {outcome.AttemptNumber + 1} 次重试] 原因: {reason}");
                    return default;
                }
            })
            .Build();

        try
        {
            var myIp = await pipeline.ExecuteAsync(async (ct) =>
            {
                // 每次重试都会重新进入这段逻辑
                var proxy = GetNextProxyConfig();
                // 获取客户端（如果 proxy 变了，这里拿到的 client 底层连接池就是新的）
                var client = clientFactory.CreateClient(proxy);
                var ip = await client.GetStringAsync("https://httpbin.org/get", ct);
                Console.WriteLine($"request ip: {ip}");
                throw new Exception("模拟错误重试"); // 模拟错误重试
                return ip;
            }, stoppingToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}