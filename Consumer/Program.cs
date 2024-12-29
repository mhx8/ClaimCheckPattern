using Consumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Shared;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json");
builder.Logging.AddSimpleConsole(
        options =>
        {
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.SingleLine = true;
        })
    .AddFilter(
        "Azure",
        LogLevel.None)
    .AddFilter(
        "Microsoft",
        LogLevel.None);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddAzureServiceBusClients(builder.Configuration);
builder.Services.AddHostedService<EventConsumer>();

IHost host = builder.Build();
host.Run();