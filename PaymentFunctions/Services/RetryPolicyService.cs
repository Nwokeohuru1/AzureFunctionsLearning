using Polly;

namespace PaymentFunctions.Services
{
    public class RetryPolicyService
    {
        public IAsyncPolicy CreateRetryPolicy()
        {
            return Policy
                .Handle<TimeoutException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
}
