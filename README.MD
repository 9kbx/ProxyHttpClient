# ProxyHttpClient 通用代理客户端

ProxyHttpClient 是一个专为 .NET 8+ 设计的高性能、轻量级动态代理客户端工厂。它解决了在大批量账号/并发场景下，如何精准控制每个请求的代理 IP 且不造成套接字耗尽（Socket Exhaustion）的难题。

在请求时动态指定代理，适合处理**一个账号单独使用一个代理**请求的场景（也适合类似场景使用）。

## 核心特性
- 物理隔离：基于连接池分区技术，确保不同代理配置之间的物理连接完全隔离，绝不串号。

- 零预注册：无需预先配置成千上万个客户端，随用随传，自动动态生成。

- 自动生命周期管理：集成 IHttpClientFactory 缓存机制，空闲代理连接自动回收，节省内存。

- 身份验证支持：完美支持 User:Pass 格式的私密代理。

- Polly 友好：支持在类库层或业务层轻松挂载重试、熔断策略。

## 快速上手

1. 安装与注册

在您的 Program.cs 中注册服务：

```csharp
using ProxyHttpClient;

var builder = WebApplication.CreateBuilder(args);

// 注册通用代理模块
// builder.Services.AddProxyHttpClient();
builder.Services.AddProxyHttpClient(client =>
{
    // 设置默认客户端
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(20);
});
// 注册强类型客户端
builder.Services.AddTypedHttpClient<AviationWeatherClient>(client =>
{
    client.BaseAddress = new Uri("https://aviationweather.gov/");
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddTypedHttpClient<MyIpClient>(client =>
{
    client.BaseAddress = new Uri("https://httpbin.org/");
});
// 注册命名客户端的业务配置
builder.Services.AddNamedHttpClient("IpClient", client =>
{
    client.BaseAddress = new Uri("https://httpbin.org/");
});

var app = builder.Build();
```

2. 定义代理配置

```csharp
var config = new ProxyConfig(
    Host: "138.186.139.137", 
    Port: 36505, 
    UserName: "your_user", 
    Password: "your_password"
);
```
> Host 默认 `http` 协议，也可指定协议，比如： `Host: "socks5://138.186.139.137"`

3. 在业务中使用

```csharp
public class MyService(ProxyHttpClientFactory proxyFactory)
{
    public async Task FetchData()
    {
        // 1. 获取绑定了特定代理的 HttpClient
        // 如果代理配置相同，内部会自动复用高性能连接池
        var client = proxyFactory.CreateClient(config);

        // 2. 发起请求
        var result = await client.GetStringAsync("https://api.ipify.org");
        Console.WriteLine($"当前出口 IP: {result}");
    }
}
```

其它使用示例

```csharp
var myIpClient = clientFactory.CreateClient<MyIpClient>(proxy);
var myIp = await myIpClient.GetIpAsync(stoppingToken);

var myIpClient2 = clientFactory.CreateClient("IpClient", proxy);
var myIp2 = await myIpClient2.GetStringAsync("ip", stoppingToken);
```

## 进阶配置：配合 Polly 使用

安装包

- `Microsoft.Extensions.Http.Polly`
- `Microsoft.Extensions.Http.Resilience`

建议在业务层根据具体接口需求配置 Polly 策略：

```csharp
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2));

var client = proxyFactory.CreateClient(config);

await retryPolicy.ExecuteAsync(() => client.GetAsync("https://example.com"));
```

## 核心原理

本库利用了 .NET 的 IHttpClientFactory 命名客户端特性。当你调用 CreateClient(config) 时：

- 摘要计算：库会根据 ProxyConfig 生成唯一的 CacheKey。

- 动态拦截：通过 IPostConfigureOptions 拦截 HttpClient 创建过程。

- 按需配置：在请求发起瞬间，动态注入 SocketsHttpHandler 并挂载 WebProxy。

- 连接复用：相同的 CacheKey 会共享同一个底层物理连接池，直到该 Client 因空闲被回收。

## 注意事项

- 句柄限制：在大批量并发场景下（如同时使用 5000+ 不同代理），请务必增加操作系统的最大文件句柄数（Linux: ulimit -n）。

- 连接寿命：类库默认 PooledConnectionLifetime 为 2 分钟。如果需要更频繁地切换 IP（如代理池本身在变动），可以调整此参数。

## 开源协议

MIT License