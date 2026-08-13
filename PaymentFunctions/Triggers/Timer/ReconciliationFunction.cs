using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentFunctions.Data;

namespace PaymentFunctions.Triggers.TimerTrigger
{
    public class ReconciliationFunction
    {
        private readonly ILogger<ReconciliationFunction> _logger;
        private readonly PaymentContext _context;
        private readonly IConfiguration _config;
        public ReconciliationFunction(ILogger<ReconciliationFunction> logger, PaymentContext context, IConfiguration config)
        {
            _logger = logger;
            _context = context;
            _config = config;
        }

        //[Function("ReconciliationJob")]
        public async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo timer)
        {
            _logger.LogInformation("================================");
            _logger.LogInformation("Reconciliation Job Started");
            _logger.LogInformation($"Current local Time : {DateTime.Now}");

            var timeout = _config.GetValue<int>("PendingTransferTimeoutMinutes");
            var cutoff = DateTime.Now.AddMinutes(-timeout);

            var pendingTransfers = await _context.Transfers.Where(x => x.Status == "Pending" && x.CreatedAt <= cutoff).ToListAsync();

            if (pendingTransfers.Any())
            {
                foreach (var trnx in pendingTransfers)
                {
                    trnx.Status = "Failed";
                    _logger.LogInformation("Marked {Count} pending transfers as Failed.", pendingTransfers.Count);
                }

                await _context.SaveChangesAsync();

                if (timer.ScheduleStatus != null)
                {
                    _logger.LogInformation($"Next Run : {timer.ScheduleStatus.Next}");
                }

                _logger.LogInformation("Reconciliation Job Finished");
                _logger.LogInformation("================================");
            }

            _logger.LogInformation(".................................No pending record........................................");
        }
    }
}
