using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PaymentFunctions.Models;

namespace PaymentFunctions.Triggers.EventHub
{
    public class SendTelemetryFunction
    {
        private readonly ILogger<SendTelemetryFunction> _logger;
        private readonly EventHubProducerClient _producer;
        public SendTelemetryFunction(ILogger<SendTelemetryFunction> logger, EventHubProducerClient producerClient)
        {
            _logger = logger;
            _producer = producerClient;
        }

        [Function("SendTelemetry")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            TelemetryRequest? request;
            
            try
            {
                request = await JsonSerializer.DeserializeAsync<TelemetryRequest>(req.Body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult("Invalid JSON.");
            }

            if (request == null)
                return new BadRequestObjectResult("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return new BadRequestObjectResult("DeviceId is required.");

            var telemetry = new
            {
                request.DeviceId,
                request.Temperature,
                Time = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(telemetry);

            using EventDataBatch batch = await _producer.CreateBatchAsync(new CreateBatchOptions { PartitionKey = request.DeviceId});

            if (!batch.TryAdd(new EventData(json)))
            {
                _logger.LogError("Telemetry event is too large for the Event Hub batch.");
                return new BadRequestObjectResult("Telemetry event is too large.");
            }

            await _producer.SendAsync(batch);

            _logger.LogInformation("Telemetry sent to Event Hub. Device: {DeviceId}, Temperature: {Temperature}", telemetry.DeviceId, telemetry.Temperature);
            
            return new OkObjectResult(new
            {
                message = "Telemetry event sent successfully.",
                telemetry.DeviceId,
                telemetry.Temperature,
                telemetry.Time
            });
        }
    }
}
