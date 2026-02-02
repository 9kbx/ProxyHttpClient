namespace ProxyHttpClient.Test;

public class SpecificProxySample(ProxyHttpClientFactory clientFactory)
{
    public async Task RunAsync()
    {
        var proxy1 = new ProxyConfig("socks5://111.123.123.52", 12312, "aaaa", "bbbb");
        var proxy2 = new ProxyConfig("222.123.123.115", 12312, "aaaa", "bbbb");
        var proxy3 = new ProxyConfig("123.123.123.35", 12312, "aaaa", "bbbb");
        
        var defaultClient = clientFactory.CreateClient(proxy1);
        var defRes = await defaultClient.GetAsync("https://api.ipify.org");
        var defResIp = await defRes.Content.ReadAsStringAsync();
        Console.WriteLine($"defResIp = {defResIp}");
        
        var defaultClient2 = clientFactory.CreateClient(proxy2);
        var defRes2 = await defaultClient2.GetAsync("https://api.ipify.org");
        var defResIp2 = await defRes2.Content.ReadAsStringAsync();
        Console.WriteLine($"defResIp2 = {defResIp2}");
        
        defRes = await defaultClient.GetAsync("https://api.ipify.org");
        defResIp = await defRes.Content.ReadAsStringAsync();
        Console.WriteLine($"defResIp = {defResIp}");
        
        var defaultClient3 = clientFactory.CreateClient();
        var defRes3 = await defaultClient3.GetAsync("https://api.ipify.org");
        var defResIp3 = await defRes3.Content.ReadAsStringAsync();
        Console.WriteLine($"defResIp3 = {defResIp3}");
        
        Console.WriteLine();
    }
}