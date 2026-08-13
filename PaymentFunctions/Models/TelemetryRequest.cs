namespace PaymentFunctions.Models
{
    public class TelemetryRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public decimal Temperature { get; set; }
    }
}
