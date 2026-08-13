using PaymentFunctions.Models;
using PaymentFunctions.Response;

namespace PaymentFunctions.Services
{
    public interface IPaymentService
    {
        Task<ApiResponse<CreatePaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request);
    }
}
