using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProxyHttpClient;
using ProxyHttpClient.Test;

var builder = Host.CreateApplicationBuilder(args);

// 注册通用代理模块
// builder.Services.AddProxyHttpClient();
builder.Services.AddProxyHttpClient(client =>
{
    // 设置默认客户端
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(20);
});
// 注册强类型客户端
builder.Services.AddProxyHttpClient<AviationWeatherClient>(client =>
{
    client.BaseAddress = new Uri("https://aviationweather.gov/");
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddProxyHttpClient<MyIpClient>(client =>
{
    client.BaseAddress = new Uri("https://httpbin.org/");
});
// 注册命名客户端的业务配置
builder.Services.AddProxyHttpClient("IpClient", client =>
{
    client.BaseAddress = new Uri("https://httpbin.org/");
});

builder.Services.AddScoped<MyDbContext>(); // 模拟db上下文
builder.Services.AddSingleton<MyApp>();

var app = builder.Build();
var test = app.Services.GetRequiredService<MyApp>();
await test.RunAsync();