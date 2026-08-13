using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentFunctions.Data;
using PaymentFunctions.Services;


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddDbContext<PaymentContext>(options =>
        {
            options.UseSqlServer(context.Configuration["DefaultConnection"]);
        });
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            return new ServiceBusClient(configuration["ServiceBus"]);
        });
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            return new EventHubProducerClient(configuration["EventHubConnection"], configuration["EventHubName"]);
        });
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<RetryPolicyService>();
    })
    .Build();

host.Run();