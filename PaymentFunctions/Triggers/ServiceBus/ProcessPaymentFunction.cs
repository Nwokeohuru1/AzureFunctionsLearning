using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentFunctions.Data;
using PaymentFunctions.Exceptions;
using PaymentFunctions.Models;
using PaymentFunctions.Services;

namespace PaymentFunctions.Triggers.ServiceBusTrigger
{
    public class ProcessPaymentFunction
    {
        private readonly ILogger<ProcessPaymentFunction> _logger;
        private readonly PaymentContext _context;
        private readonly RetryPolicyService _retryPolicy;

        public ProcessPaymentFunction(ILogger<ProcessPaymentFunction> logger, PaymentContext context, RetryPolicyService retryPolicy)
        {
            _logger = logger;
            _context = context;
            _retryPolicy = retryPolicy;
        }

        [Function("ProcessPayment")]
        public async Task Run([ServiceBusTrigger("transfer-queue", Connection = "ServiceBus", AutoCompleteMessages = false)] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        {
            var attempt = 0;
            CreatePaymentRequest? payment;

            try
            {
                try
                {
                    payment = JsonSerializer.Deserialize<CreatePaymentRequest>(message.Body.ToString());
                }
                catch (JsonException)
                {
                    _logger.LogError("Invalid json request");
                    throw new PermanentPaymentException("Invalid json request.");
                }

                if (payment == null)
                    throw new PermanentPaymentException("Invalid request body.");

                if (string.IsNullOrWhiteSpace(payment.FromAccount))
                    throw new PermanentPaymentException("FromAccount is required.");

                if (string.IsNullOrWhiteSpace(payment.ToAccount))
                    throw new PermanentPaymentException("ToAccount is required.");

                if (payment.Amount <= 0)
                    throw new PermanentPaymentException("Amount must be greater than zero.");

                _logger.LogInformation(
                    "Processing transaction {TransactionId} for account {AccountNumber} with amount {Amount}",
                    payment.TransactionId,
                    payment.FromAccount,
                    payment.Amount);

                _logger.LogInformation("--------------------------------");
                _logger.LogInformation("Starting transfer processing...");

                var transfer = await _retryPolicy.CreateRetryPolicy().ExecuteAsync(async () =>
                {
                    attempt++;

                    _logger.LogInformation("Attempt {Attempt} for transaction {TransactionId}", attempt, payment.TransactionId);

                    var result = await _context.Transfers.FirstOrDefaultAsync(x => x.TransactionId == payment.TransactionId);

                    if (result == null)
                        throw new PermanentPaymentException($"Transfer {payment.TransactionId} not found.");

                    if (attempt < 3)
                        throw new TimeoutException("Simulated temporary database timeout.");

                    _logger.LogInformation("Database operation succeeded on attempt {Attempt}", attempt);

                    return result;
                });

                if (string.Equals(transfer.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Transaction {TransactionId} has already been processed.", transfer.TransactionId);

                    await messageActions.CompleteMessageAsync(message);

                    return;
                }

                transfer.Status = "Processing";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction : {TransactionId}", transfer.TransactionId);
                _logger.LogInformation("Amount      : {Amount}", transfer.Amount);
                _logger.LogInformation("Status      : {Status}", transfer.Status);

                transfer.Status = "Completed";
                transfer.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Transfer processed successfully.");
                _logger.LogInformation("Status      : {Status}", transfer.Status);
                _logger.LogInformation("--------------------------------");

                await messageActions.CompleteMessageAsync(message);

            }
            catch (PermanentPaymentException ex)
            {
                _logger.LogError("Permanent payment failure for message {MessageId}: {Reason}", message.MessageId, ex.Message);

                await messageActions.DeadLetterMessageAsync(
                    message, deadLetterReason:
                    "PermanentPaymentFailure", deadLetterErrorDescription:
                    ex.Message);
            }
        }
    }
}
