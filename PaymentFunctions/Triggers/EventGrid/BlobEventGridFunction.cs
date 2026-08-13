using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PaymentFunctions.Triggers.EventGrid
{
    public class BlobEventGridFunction
    {
        private readonly ILogger<BlobEventGridFunction> _logger;
        public BlobEventGridFunction(ILogger<BlobEventGridFunction> logger)
        {
            _logger = logger;
        }

        [Function("BlobEventGridFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("============================================");
            _logger.LogInformation("Event Grid event received");

            _logger.LogInformation("Event Type: {EventType}", eventGridEvent.EventType);

            _logger.LogInformation("Subject: {Subject}", eventGridEvent.Subject);

            var data = eventGridEvent.Data.ToObjectFromJson<JsonElement>();

            if (!data.TryGetProperty("url", out JsonElement urlElement))
            {
                _logger.LogError("Blob URL was not found in the Event Grid event.");
                return;
            }

            var blobUrl = urlElement.GetString();

            if (string.IsNullOrWhiteSpace(blobUrl))
            {
                _logger.LogError("Blob URL is empty.");
                return;
            }

            _logger.LogInformation("Blob URL: {BlobUrl}", blobUrl);

            var blobClient = new BlobClient(new Uri(blobUrl), new DefaultAzureCredential());

            var properties = await blobClient.GetPropertiesAsync();

            _logger.LogInformation("Blob Content Type: {ContentType}", properties.Value.ContentType);
            _logger.LogInformation("Blob Size: {Size} bytes", properties.Value.ContentLength);
            _logger.LogInformation("Blob ETag: {ETag}", properties.Value.ETag);

            _logger.LogInformation("Successfully accessed the actual blob.");

            _logger.LogInformation("============================================");
        }
    }
}
