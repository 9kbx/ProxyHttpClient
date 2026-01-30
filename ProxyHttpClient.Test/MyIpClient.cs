namespace ProxyHttpClient.Test;

public class MyIpClient(HttpClient httpClient)
{
    public Task<string> GetIpAsync(CancellationToken stoppingToken) =>
        httpClient.GetStringAsync("ip", stoppingToken);
}