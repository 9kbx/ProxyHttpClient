using System.Net;
using System.Net.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using ProxyHttpClient;
using ProxyHttpClient.Test;

var builder = Host.CreateApplicationBuilder(args);

// 注册通用代理模块
// builder.Services.AddProxyHttpClient();
builder.Services.AddProxyHttpClient(client =>
    {
        // 设置默认客户端
        client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddHttpMessageHandler<MockProxyHttpMessageHandler>()
    .ConfigurePrimaryHttpMessageHandler((s) =>
    {
        return new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            },
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(10)
        };
    })
    // 重试策略
    .AddResilienceHandler("my-strategy", builder =>
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

// 注册强类型客户端
builder.Services.AddProxyHttpClient<AviationWeatherClient>(client =>
{
    client.BaseAddress = new Uri("https://aviationweather.gov/");
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(20);
}).AddHttpMessageHandler<MockProxyHttpMessageHandler>();
builder.Services.AddProxyHttpClient<MyIpClient>(client => client.BaseAddress = new Uri("https://httpbin.org/"))
    .AddHttpMessageHandler<MockProxyHttpMessageHandler>();
// 注册命名客户端的业务配置
builder.Services.AddProxyHttpClient("IpClient", client => client.BaseAddress = new Uri("https://httpbin.org/"))
    .AddHttpMessageHandler<MockProxyHttpMessageHandler>();
builder.Services.AddProxyHttpClient("MockClient", client => client.BaseAddress = new Uri("https://api.mock-test.com/"))
    .AddHttpMessageHandler<MockProxyHttpMessageHandler>();
builder.Services.AddProxyHttpClient("ProxyChecker", client => client.BaseAddress = new Uri("https://api.ipify.org/"));

builder.Services.AddTransient<MockProxyHttpMessageHandler>();

builder.Services.AddScoped<MyDbContext>(); // 模拟db上下文
builder.Services.AddSingleton<DefaultSample>();
builder.Services.AddSingleton<RetryPolicySample>();
builder.Services.AddSingleton<SpecificProxySample>();
builder.Services.AddSingleton<MockHandlerSample>();
builder.Services.AddSingleton<ProxyChecker>();

var app = builder.Build();

// var test = app.Services.GetRequiredService<DefaultSample>();
// var test = app.Services.GetRequiredService<RetryPolicySample>();
// var test = app.Services.GetRequiredService<SpecificProxySample>();
// var test = app.Services.GetRequiredService<MockHandlerSample>();
var test = app.Services.GetRequiredService<ProxyChecker>();
await test.RunAsync();