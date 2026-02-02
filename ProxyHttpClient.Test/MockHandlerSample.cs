namespace ProxyHttpClient.Test;

public class MockHandlerSample(ProxyHttpClientFactory clientFactory)
{
    public async Task RunAsync()
    {
        foreach (var proxy in Helper.GetProxies())
        {
            var defaultClient = clientFactory.CreateClient("MockClient", proxy);
            var defRes = await defaultClient.GetAsync("https://api.mock-test.com/");
            var defResIp = await defRes.Content.ReadAsStringAsync();
            Console.WriteLine($"defResIp = {defResIp}");
        }

        Console.WriteLine();
    }

}