using Azure;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Consumer;

public class EventConsumer : BackgroundService
{
    private readonly ILogger _logger;
    private readonly ServiceBusProcessor _processor;

    public EventConsumer(
        IAzureClientFactory<ServiceBusClient> clientFactory,
        ILogger<EventConsumer> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        ServiceBusClient? client = clientFactory.CreateClient("Default");
        _processor = client.CreateProcessor(
            configuration["AzureServiceBus:TopicName"],
            "ConsumerSubscription",
            new ServiceBusProcessorOptions());
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        CloudEvent cloudEvent = CloudEvent.Parse(args.Message.Body)!;
        _logger.LogInformation(
            "Received event: {0}",
            cloudEvent.Id);

        BlobClient blobClient = new(new Uri(cloudEvent.Data!.ToObjectFromJson<string>()!));
        Response<BlobDownloadResult>? result = await blobClient.DownloadContentAsync();

        await blobClient.DeleteIfExistsAsync();

        _logger.LogInformation(result.Value.Content.ToString());

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ProcessErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError($"Error: {args.Exception.Message}");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        await _processor.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}