namespace ProxyHttpClient.Test;

public static class Helper
{
    public static IEnumerable<ProxyConfig> GetProxies() =>
        Enumerable.Range(0, 10).Select(i =>
            new ProxyConfig($"10.0.{(i >> 8) & 0xFF}.{i & 0xFF}", 123, "usr", "pwd"));

    public static List<ProxyConfig> LoadProxies()
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