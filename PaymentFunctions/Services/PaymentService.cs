using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentFunctions.Data;
using PaymentFunctions.Models;
using PaymentFunctions.Response;

namespace PaymentFunctions.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentContext _dbContext;
        private readonly ServiceBusSender _sender;
        private readonly ILogger<PaymentService> _logger;
        public PaymentService(ILogger<PaymentService> logger, PaymentContext context, ServiceBusClient client)
        {
            _logger = logger;
            _dbContext = context;
            _sender = client.CreateSender("transfer-queue");
        }
        public async Task<ApiResponse<CreatePaymentResponse>> CreatePaymentAsync(CreatePaymentRequest payment)
        {
            //var hhh = Guid.NewGuid().ToString();

            if (payment == null)
                return new ApiResponse<CreatePaymentResponse> { Success = false, Message = "Payment request is required." };

            if (string.IsNullOrWhiteSpace(payment.FromAccount))
                return new ApiResponse<CreatePaymentResponse> { Success = false, Message = "From account is required." };

            if (string.IsNullOrWhiteSpace(payment.ToAccount))
                return new ApiResponse<CreatePaymentResponse> { Success = false, Message = "To account is required." };

            if (payment.Amount <= 0)
                return new ApiResponse<CreatePaymentResponse> { Success = false, Message = "Amount must be greater than zero." };

            var existingTransfer = await _dbContext.Transfers.FirstOrDefaultAsync(x => x.TransactionId == payment.TransactionId);
            if (existingTransfer != null)
            {
                _logger.LogWarning("Transaction {TransactionId} already exists. Skipping duplicate.", payment.TransactionId);

                return new ApiResponse<CreatePaymentResponse>
                {
                    Success = false,
                    Message = "Payment has already been processed.",
                    Data = new CreatePaymentResponse
                    {
                        TransactionId = payment.TransactionId.ToString()
                    }
                };
            }

            _logger.LogInformation("Processing transaction {TransactionId} for {Amount}", payment.TransactionId, payment.Amount);

            var transfer = new Transfer
            {
                Id = Guid.NewGuid(),
                TransactionId = payment.TransactionId,
                FromAccount = payment.FromAccount,
                ToAccount = payment.ToAccount,
                Amount = payment.Amount,
                Status = "Pending",
                CreatedAt = payment.CreatedAt
            };

            _dbContext.Transfers.Add(transfer);

            await _dbContext.SaveChangesAsync();
            var json = JsonSerializer.Serialize(payment);
            var message = new ServiceBusMessage(json)
            {
                MessageId = payment.TransactionId.ToString()
            };
            await _sender.SendMessageAsync(message);

            return new ApiResponse<CreatePaymentResponse>
            {
                Success = true,
                Message = "Payment received for processing.",
                Data = new CreatePaymentResponse
                {
                    TransactionId = payment.TransactionId.ToString()
                }
            };
        }
    }
}
