using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PaymentFunctions.Models;
using PaymentFunctions.Response;
using PaymentFunctions.Services;

namespace PaymentFunctions.Triggers.HttpTrigger
{
    public class CreatePaymentFunction
    {
        private readonly ILogger<CreatePaymentFunction> _logger;
        private readonly IPaymentService _paymentService;
        public CreatePaymentFunction(ILogger<CreatePaymentFunction> logger, IPaymentService paymentService)
        {
            _logger = logger;
            _paymentService = paymentService;
        }

        [Function("CreatePayment")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            //var hhh = Guid.NewGuid().ToString();
            CreatePaymentRequest? payment;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                payment = JsonSerializer.Deserialize<CreatePaymentRequest>(requestBody);
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(new ApiResponse<object> { Success = false, Message = "Invalid JSON." });
            }
            if (payment == null)
            {
                return new BadRequestObjectResult(new ApiResponse<object> { Success = false, Message = "Invalid request body." });
            }
            if (string.IsNullOrWhiteSpace(payment.ToAccount))
            {
                return new BadRequestObjectResult(new ApiResponse<object> { Success = false, Message = "Account Number Is required." });
            }
            if (payment.Amount <= 0)
            {
                return new BadRequestObjectResult(new ApiResponse<object> { Success = false, Message = "Amount must be greater than zero." });
            }

            var result = await _paymentService.CreatePaymentAsync(payment);

            return new OkObjectResult(result);
        }
    }
}
