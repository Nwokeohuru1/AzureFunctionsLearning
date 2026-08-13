using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using PaymentFunctions.Models;
using PaymentFunctions.Services;

namespace PaymentFunctions.Triggers.BlobTrigger
{
    public class ProcessUploadedFileFunction
    {
        private readonly ILogger<ProcessUploadedFileFunction> _logger;
        private readonly IPaymentService _paymentService;
        public ProcessUploadedFileFunction(ILogger<ProcessUploadedFileFunction> logger, IPaymentService paymentService)
        {
            _logger = logger;
            _paymentService = paymentService;
        }

        //[Function("ProcessUploadedFile")]
        public async Task Run([BlobTrigger("uploads/{name}", Connection = "StorageConnection")] Stream blob, string name)
        {
            var payments = new List<CreatePaymentRequest>();

            ExcelPackage.License.SetNonCommercialPersonal("Dikachi");

            using var package = new ExcelPackage(blob);
            var worksheet = package.Workbook.Worksheets[0];
            int rows = worksheet.Dimension.Rows;

            var result = new BulkPaymentResult
            {
                Total = rows - 1
            };

            for (int row = 2; row <= rows; row++)
            {
                var transactionIdText = worksheet.Cells[row, 1].Text?.Trim();
                var fromAccount = worksheet.Cells[row, 2].Text?.Trim();
                var toAccount = worksheet.Cells[row, 3].Text?.Trim();
                var amountText = worksheet.Cells[row, 4].Text?.Trim();

                if (string.IsNullOrWhiteSpace(fromAccount))
                {
                    result.Failed++;

                    result.Errors.Add(new BulkPaymentError
                    {
                        RowNumber = row,
                        Error = "FromAccount is required."
                    });
                    continue;
                }
                if (string.IsNullOrWhiteSpace(toAccount))
                {
                    result.Failed++;

                    result.Errors.Add(new BulkPaymentError
                    {
                        RowNumber = row,
                        Error = "ToAccount is required."
                    });
                    continue;
                }
                if (!decimal.TryParse(amountText, out var amount))
                {
                    result.Failed++;

                    result.Errors.Add(new BulkPaymentError
                    {
                        RowNumber = row,
                        Error = "Amount is not a valid number."
                    });

                    continue;
                }
                if (amount <= 0)
                {
                    result.Failed++;

                    result.Errors.Add(new BulkPaymentError
                    {
                        RowNumber = row,
                        Error = "Amount must be greater than zero."
                    });

                    continue;
                }
                if (!Guid.TryParse(transactionIdText, out var transactionId))
                {
                    result.Failed++;

                    result.Errors.Add(new BulkPaymentError
                    {
                        RowNumber = row,
                        Error = "TransactionId is not a valid GUID."
                    });

                    continue;
                }

                var payment = new CreatePaymentRequest
                {
                    FromAccount = fromAccount,
                    ToAccount = toAccount,
                    Amount = amount,
                    TransactionId = transactionId,
                    CreatedAt = DateTime.Now
                };

                try
                {
                    var paymentResult = await _paymentService.CreatePaymentAsync(payment);
                    if (paymentResult.Success)
                    {
                        result.Successful++;

                        _logger.LogInformation("Row {Row}: Payment {TransactionId} processed successfully.", row, payment.TransactionId);
                    }
                    else
                    {
                        result.Failed++;

                        result.Errors.Add(new BulkPaymentError { RowNumber = row, Error = paymentResult.Message });
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add(new BulkPaymentError { RowNumber = row, Error = ex.Message });
                    _logger.LogError(ex, "Row {Row}: Failed to process payment.", row);
                }
            }

            _logger.LogInformation(
                "Bulk payment processing completed. Total: {Total}, Successful: {Successful}, Failed: {Failed}",
                result.Total,
                result.Successful,
                result.Failed);

            foreach (var error in result.Errors)
            {
                _logger.LogWarning("Row {Row}: {Error}", error.RowNumber, error.Error);
            }        
        }
    }
}
