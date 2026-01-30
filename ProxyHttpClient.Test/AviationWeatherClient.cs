namespace ProxyHttpClient.Test;

public class AviationWeatherClient
{
    private readonly HttpClient _httpClient;

    public AviationWeatherClient(HttpClient httpClient, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetMetarAsync(string city, CancellationToken stoppingToken = default)
    {
        // 这里的请求会自动带上 BaseAddress 和特定的代理
        return await _httpClient.GetStringAsync($"api/data/metar?ids={city}&format=json", stoppingToken);
    }
}