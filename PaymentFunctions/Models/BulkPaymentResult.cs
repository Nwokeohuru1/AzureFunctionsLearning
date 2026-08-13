namespace PaymentFunctions.Models
{
    public class BulkPaymentResult
    {
        public int Total { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<BulkPaymentError> Errors { get; set; } = new();

    }
    public class BulkPaymentError
    {
        public int RowNumber { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
