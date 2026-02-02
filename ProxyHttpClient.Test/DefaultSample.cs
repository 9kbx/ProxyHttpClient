using System.Net;
using Polly;

namespace ProxyHttpClient.Test;

public class DefaultSample(ProxyHttpClientFactory clientFactory, MyDbContext dbContext)
{
    public async Task RunAsync()
    {
        var proxy = new ProxyConfig("111.222.123.123", 33321, "aaa", "bbb");

        await MyAppClientTestAsync(); // 强类型客户端测试
        await DefaultHttpClientTestAsync(); // 不使用代理
        await ProxyTestAsync(proxy); // 代理测试
        await BatchTestAsync(); // 批量代理测试
    }

    private async Task BatchTestAsync()
    {
        var proxies = Helper.GetProxies();
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

    private async Task DefaultHttpClientTestAsync(CancellationToken stoppingToken = default)
    {
        var client = clientFactory.CreateClient();
        var response = await client.GetAsync("https://httpbin.org/ip", stoppingToken);
        var rawText = await response.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine(rawText);
    }

    private async Task MyAppClientTestAsync(CancellationToken stoppingToken = default)
    {
        var proxy = new ProxyConfig("socks5://111.123.123.52", 12312, "aaaa", "bbbb");
        var proxy2 = new ProxyConfig("222.123.123.115", 12312, "aaaa", "bbbb");
        var proxy3 = new ProxyConfig("123.123.123.35", 12312, "aaaa", "bbbb");


        var weatherClient = clientFactory.CreateClient<AviationWeatherClient>(proxy);
        var data = await weatherClient.GetMetarAsync("KMCI", stoppingToken);
        Console.WriteLine($"Weather = {data}");

        var defaultClient = clientFactory.CreateClient(proxy);
        var defRes = await defaultClient.GetAsync("https://api.ipify.org", stoppingToken);
        var defResIp = await defRes.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine($"defResIp = {defResIp}");

        var defaultClient2 = clientFactory.CreateClient(proxy2);
        var defRes2 = await defaultClient2.GetAsync("https://httpbin.org/ip", stoppingToken);
        var defResIp2 = await defRes2.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine($"defResIp2 = {defResIp2}");

        defRes = await defaultClient.GetAsync("https://api.ipify.org", stoppingToken);
        defResIp = await defRes.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine($"defResIp = {defResIp}");


        var defaultClient3 = clientFactory.CreateClient();
        var defRes3 = await defaultClient3.GetAsync("https://api.ipify.org", stoppingToken);
        var defResIp3 = await defRes3.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine($"defResIp3 = {defResIp3}");

        var myIpClient = clientFactory.CreateClient<MyIpClient>(proxy2);
        var myIp1 = await myIpClient.GetIpAsync(stoppingToken);
        Console.WriteLine($"myIp = {myIp1}");

        var myIpClient2 = clientFactory.CreateClient("IpClient", proxy3);
        // var myIp2 = await myIpClient2.GetStringAsync("ip", stoppingToken);
        var response = await myIpClient2.GetAsync("ip", stoppingToken);
        var myIp2 = await response.Content.ReadAsStringAsync(stoppingToken);
        Console.WriteLine($"myIp2 = {myIp2}");
        Console.WriteLine();
    }
}