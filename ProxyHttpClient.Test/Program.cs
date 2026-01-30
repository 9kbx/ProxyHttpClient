using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProxyHttpClient;
using ProxyHttpClient.Test;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddUniversalProxySupport();

builder.Services.AddScoped<MyDbContext>();
builder.Services.AddSingleton<MyApp>();

var app = builder.Build();
var test = app.Services.GetRequiredService<MyApp>();
await test.RunAsync();