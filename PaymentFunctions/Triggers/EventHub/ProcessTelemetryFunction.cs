using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PaymentFunctions.Triggers.EventHub
{
    public class ProcessTelemetryFunction
    {
        private readonly ILogger<ProcessTelemetryFunction> _logger;
        public ProcessTelemetryFunction(ILogger<ProcessTelemetryFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessTelemetry")]
        public void Run([EventHubTrigger("telemetry-hub", Connection = "EventHubConnection")] EventData[] events, FunctionContext context)
        {
            var partitionContextJson = context.BindingContext.BindingData["PartitionContext"]?.ToString();
            string? partitionId = null;
            if (!string.IsNullOrEmpty(partitionContextJson))
            {
                using var doc = JsonDocument.Parse(partitionContextJson);
                partitionId = doc.RootElement.GetProperty("PartitionId").GetString();
            }

            _logger.LogInformation("Received {Count} event(s) from Partition {Partition}.", events.Length, partitionId);
            foreach (var eventData in events)
            {
                var body = Encoding.UTF8.GetString(eventData.EventBody.ToArray());

                _logger.LogInformation("--------------------------------");
                _logger.LogInformation("Partition        : {Partition}", partitionId);
                _logger.LogInformation("Partition Key    : {PartitionKey}", eventData.PartitionKey);
                _logger.LogInformation("Sequence Number  : {SequenceNumber}", eventData.SequenceNumber);
                _logger.LogInformation("Offset           : {Offset}", eventData.OffsetString);
                _logger.LogInformation("Enqueued Time    : {EnqueuedTime}", eventData.EnqueuedTime);
                _logger.LogInformation("Telemetry Event  : {Body}", body);
            }

            _logger.LogInformation("--------------------------------");
        }
    }
}
