using System.Collections.Concurrent;

namespace ProxyHttpClient.Test;

public class ProxyChecker(ProxyHttpClientFactory factory)
{
    private readonly ConcurrentBag<string> Successed = new();
    private readonly ConcurrentBag<string> Faild = new();
    private readonly ConcurrentBag<string> Log = new();

    public async Task RunAsync()
    {
        var proxies = Helper.LoadProxies();
        await Parallel.ForEachAsync(proxies, new ParallelOptions()
        {
            MaxDegreeOfParallelism = 10,
        }, async (proxy, ct) =>
        {
            try
            {
                var client = factory.CreateClient("ProxyChecker", proxy);
                var ip = await client.GetStringAsync("", ct);
                await Console.Out.WriteLineAsync($"true - {proxy.Host} -> {ip}");
                Log.Add($"true - {proxy.Host} -> {ip}");
                Successed.Add(proxy.Host);
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync($"false - {proxy.Host}");
                Log.Add($"false - {proxy.Host}");
                Faild.Add(proxy.Host);
            }
        });
        await Console.Out.WriteLineAsync("----- done ------");
        
        await File.WriteAllLinesAsync("0-successed.txt", Successed);
        await File.WriteAllLinesAsync("0-faild.txt", Faild);
        await File.WriteAllLinesAsync("0-log.txt", Log);
    }
}