using Microsoft.Extensions.Logging.Console;
using Publisher;
using Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddControllers();
builder.Services.AddAzureServiceBusClients(builder.Configuration);
builder.Services.AddScoped<EventPublisher>();

WebApplication app = builder.Build();
app.MapControllers();
app.Run();