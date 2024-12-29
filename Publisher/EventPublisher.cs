using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Azure;
using Shared;

namespace Publisher;

public class EventPublisher(
    IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
    IAzureClientFactory<BlobServiceClient> blobServiceClientFactory,
    IConfiguration configuration)
{
    public async Task SendEventAsync(
        string data)
    {
        LargeData largeData = new(data);
        string sasUri = await UploadAndGenerateSasUriAsync(largeData);

        ServiceBusClient? client = serviceBusClientFactory.CreateClient("Default");
        ServiceBusSender? sender = client.CreateSender(configuration["AzureServiceBus:TopicName"]);

        CloudEvent cloudEvent = new(
            source: "https://mhx8.com/events",
            type: "com.mhx8.event",
            jsonSerializableData: sasUri
        );

        ServiceBusMessage message = new(new BinaryData(cloudEvent))
        {
            ContentType = "application/cloudevents+json"
        };

        await sender.SendMessageAsync(message);
    }

    private async Task<string> UploadAndGenerateSasUriAsync(
        LargeData largeData)
    {
        BlobServiceClient? blobServiceClient = blobServiceClientFactory.CreateClient("Default");
        BlobContainerClient? containerClient = blobServiceClient.GetBlobContainerClient("largedata");
        await containerClient.CreateIfNotExistsAsync();

        BlobClient? blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString());
        await blobClient.UploadAsync(
            new BinaryData(largeData),
            overwrite: true);

        return GenerateSasUri(blobClient);
    }

    private string GenerateSasUri(
        BlobClient blobClient)
    {
        BlobSasBuilder sasBuilder = new()
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(10)
        };
        sasBuilder.SetPermissions(
            BlobSasPermissions.Read | BlobSasPermissions.Delete);

        BlobSasQueryParameters? sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(
            configuration["AzureStorage:AccountName"],
            configuration["AzureStorage:AccountKey"]));

        BlobUriBuilder blobUriBuilder = new(blobClient.Uri)
        {
            Sas = sasToken
        };

        return blobUriBuilder.ToUri().AbsoluteUri;

    }
}