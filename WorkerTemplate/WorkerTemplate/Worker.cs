using System.Diagnostics;

namespace WorkerTemplate
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var MyActivitySource = new ActivitySource(configuration["ServiceName"]);

                using var activity = MyActivitySource.StartActivity("Execution_Activity");
                activity?.SetTag("Method", "ExecuteAsync");
                activity?.SetTag("Numbers", new int[] { 1, 2, 3, 4, 5, 6 });
                activity?.AddEvent(new ActivityEvent("Returning numbers from method...."));

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}







