using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Plus.Options;
using Microsoft.FeatureManagement.Plus.Services;

namespace Microsoft.FeatureManagement.Plus
{
    public class TimedHostedService : BackgroundService
    {
        private readonly ILogger<TimedHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private int _executionCount;

        private readonly TimeSpan _interval;
        public TimedHostedService(ILogger<TimedHostedService> logger, IServiceProvider serviceProvider, IOptions<TimeHostedServiceOptions> options)
        {
            _logger = logger;
            this._serviceProvider = serviceProvider;
            _interval = TimeSpan.FromSeconds(options.Value.IntervalInSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            await DoWorkAsync();

            using var timer = new PeriodicTimer(_interval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    await DoWorkAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Timed Hosted Service is stopping.");
            }
        }

        private async Task DoWorkAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var myService = scope.ServiceProvider.GetRequiredService<IFeatureService>();
                // ... actual work
                int count = Interlocked.Increment(ref _executionCount);
                await Task.Delay(TimeSpan.FromSeconds(2));
                _logger.LogInformation("Timed Hosted Service is working. Count: {Count}", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DoWork.");
                // Optionally: implement retry/backoff logic here
            }
        }
    }
}