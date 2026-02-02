# ProxyHttpClient 通用代理客户端

ProxyHttpClient 是一个专为 .NET 10 设计的高性能、轻量级动态代理客户端工厂。它巧妙地解决了在大批量账号/高并发场景下，如何精准控制每个请求的代理
IP 且不造成套接字耗尽（Socket Exhaustion）的难题。

在请求时动态指定代理，适合处理**一个账号单独使用一个代理**请求的场景（也适合类似场景使用）。

## 核心特性

- 物理隔离：基于 Composite Key (复合键) 分区，确保不同代理配置之间的物理连接完全隔离，绝不串号。

- 零预注册：无需预先在容器中配置成千上万个客户端，代理配置随用随传，系统自动动态装配。

- 完美继承：支持原生 ConfigurePrimaryHttpMessageHandler 和 AddResilienceHandler。你的业务配置（SSL、超时、重试）会被自动“克隆”到每一个动态代理客户端上。

- 自动生命周期管理：集成 IHttpClientFactory 缓存机制，空闲代理连接自动回收，节省内存。

- 身份验证支持：完美支持 User:Pass 格式的私密代理。

- 高性能实例化：内部使用 ActivatorUtilities 预编译工厂，在高并发创建强类型客户端时保持极低开销。

## 快速上手

1. 安装与注册

在您的 Program.cs 中注册基础服务。即使使用动态代理，您依然可以像使用原生 HttpClient 一样定义全局策略：

```csharp
using ProxyHttpClient;

builder.Services.AddProxyHttpClient(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(20);
})
// 原生支持：所有的代理客户端都会继承此 SSL 配置
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        RemoteCertificateValidationCallback = (_, _, _, _) => true
    }
});
```

2. 定义代理配置

支持 `http/https` 及 `socks5` 协议。

```csharp
// 匿名代理
var proxy = new ProxyConfig("123.123.123.123", 8080);

// 用户认证代理
var authProxy = new ProxyConfig("123.123.123.123", 8080, "user", "pass");
```

> 默认使用 `http` 协议，如需其它协议可以这样设置 `socks5://138.186.139.137`
>
> 请注意，.NET 的 WebProxy 对 SOCKS 代理的支持取决于操作系统（Windows 10+ / Linux 较新内核支持良好）。如果遇到连接失败，请确认底层环境支持
> SOCKS 协议头

3. 在业务中使用

注入 `ProxyHttpClientFactory`，它将成为你获取客户端的唯一入口。

```csharp
public class MyService(ProxyHttpClientFactory clientFactory)
{
    public async Task FetchData()
    {
        // 1. 获取一个绑定了指定代理的客户端
        var client = clientFactory.CreateClient(proxy);
        // 2. 获取强类型客户端（自动注入 HttpClient）
        var weatherClient = clientFactory.CreateClient<WeatherClient>(authProxy);
        // 3. 获取无代理的直连客户端
        var directClient = clientFactory.CreateClient();
        
        var result = await client.GetStringAsync("https://api.ipify.org");
        Console.WriteLine($"当前出口 IP: {result}");
    }
}
```

## 场景示例

- 强类型客户端配置

当您需要为特定 API 定义 BaseAddress 或特定的 Resilience 策略时：

安装包 `Microsoft.Extensions.Http.Resilience`

```csharp
builder.Services.AddProxyHttpClient<MyIpClient>(client =>
{
    client.BaseAddress = new Uri("https://httpbin.org/");
})
.AddStandardResilienceHandler(); // 所有的动态代理请求都会自动应用此弹性策略
```

- 动态更换代理并重试 (Polly v8)

```csharp
var pipeline = new ResiliencePipelineBuilder<string>().AddRetry(new RetryStrategyOptions<string> {
    MaxRetryAttempts = 3,
    ShouldHandle = new PredicateBuilder<string>().Handle<Exception>()
}).Build();

await pipeline.ExecuteAsync(async ct => {
    var nextProxy = GetProxyFromPool(); 
    // 每次重试都会通过新的 Composite Key 获取/创建一个全新的连接池
    var client = clientFactory.CreateClient(nextProxy);
    return await client.GetStringAsync("https://api.ipify.org", ct);
});
```

- 合理配置过期时间： 如果您希望更积极地回收不再使用的代理连接，可以在注册时调整：

```csharp
builder.Services.AddProxyHttpClient()
    .SetHandlerLifetime(TimeSpan.FromSeconds(60)); // 缩短 Handler 在缓存中的驻留时间
```

- 其它 Polly 使用样例

示例1：注册时设定重试策略

```csharp
builder.Services.AddProxyHttpClient<MyClient>()
    // .AddStandardResilienceHandler(); // 应用默认弹性策略
    .AddResilienceHandler("my-strategy", builder => // 自定义策略
    {
        builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1)
            })
            .AddTimeout(TimeSpan.FromSeconds(5));
    });
```

示例2：动态创建重试策略

```csharp
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

var client = proxyFactory.CreateClient(config);
await pipeline.ExecuteAsync(() => client.GetStringAsync("https://example.com"));
```

## 性能基准测试 (Benchmark)

为了保证并发处理能力，我们对 ProxyHttpClient 进行了严苛的基准测试。

| 场景     | 平均耗时 (Mean) | 内存分配 (Allocated) | 性能说明             |
|--------|------------:|-----------------:|------------------|
| 原生创建   |    64.11 ns |            320 B | NET 原生工厂基准       |                                                                                                                                                                                                         
| 代理缓存命中 |   139.01 ns |            544 B | 相同 IP 再次请求，极速响应  |
| 全新代理注入 |   414.77 ns |            651 B | 核心能力：海量新 IP 动态装配 |

以下是测试详情

```text
BenchmarkDotNet v0.15.8, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M4 Pro, 1 CPU, 14 logical and 14 physical cores                                                                                                                                                                                                                                                                       
.NET SDK 10.0.100                                                                                                                                                                                                                                                                                                           
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a                                                                                                                                                                                                                                                  
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a 
```

| Method            |      Mean |    Error |   StdDev | Ratio | RatioSD | Rank |   Gen0 | Allocated | Alloc Ratio |
|-------------------|----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| NativeCreate      |  64.11 ns | 0.563 ns | 0.499 ns |  1.00 |    0.01 |    1 | 0.0381 |     320 B |        1.00 |                                                                                                                                                                                                         
| ProxyCreateCached | 139.01 ns | 0.538 ns | 0.477 ns |  2.17 |    0.02 |    2 | 0.0648 |     544 B |        1.70 |
| ProxyCreateNew    | 414.77 ns | 8.253 ns | 8.105 ns |  6.47 |    0.13 |    3 | 0.0777 |     651 B |        2.03 |

运行Benchmark

```shell
cd ProxyHttpClient.Benchmarks
dotnet run -c Release
```

## 核心原理：它是如何工作的？

- **复合键 (Composite Key) 生成**： 当你调用 `CreateClient(proxy)` 时，库会根据 `ProxyIP + Port + UserName + ClientName`
  实时计算一个唯一的字符串标识。

- **配置克隆 (Configuration Cloning)**： 由于该标识对 `IHttpClientFactory` 是全新的，库会拦截创建过程，并利用
  `IOptionsMonitor` 自动将您在 `Program.cs` 中为业务客户端（如 `Default`）定义的 `HttpClientActions` 和 `HandlerActions`
  完整克隆到这个新生成的标识下。

- **动态拦截 (Dynamic Injection)**： 在 Handler 链装配的最后一步，通过 `IPostConfigureOptions` 强行修改
  `SocketsHttpHandler.Proxy`，实现代理注入。

- **连接池分区 (Pooling)**： .NET 原生工厂会为每个唯一的标识维护独立的连接池。这意味着不同的代理 IP 拥有物理隔绝的连接，互不干扰。

## 注意事项

- 操作系统限制：在 Linux 下进行超大规模并发（5000+ 并发代理）时，请务必调高句柄限制：ulimit -n 65535。
- SOCKS5 支持：.NET 的 WebProxy 在 Windows 10+ 和较新版本的 Linux 内核上支持良好。
- 内存建议：虽然连接池会自动回收，但过多的命名客户端配置会占用一定内存。建议对于已失效且不再使用的静态代理 Key 进行定期观察。

## 开源协议

MIT License