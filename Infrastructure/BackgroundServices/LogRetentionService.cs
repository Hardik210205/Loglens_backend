using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LogLens.Infrastructure.Data;

namespace LogLens.Infrastructure.BackgroundServices
{
    public class LogRetentionService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogRetentionService> _logger;
        // Keep logs for 7 days on the free tier to save DB space
        private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7);
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

        public LogRetentionService(IServiceProvider serviceProvider, ILogger<LogRetentionService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LogRetentionService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldLogsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing log retention cleanup.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("LogRetentionService is stopping.");
        }

        private async Task CleanupOldLogsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LogLensDbContext>();

            var cutoffDate = DateTime.UtcNow.Subtract(_retentionPeriod);

            _logger.LogInformation($"Starting log cleanup. Deleting logs older than {cutoffDate}");

            // EF Core 7+ ExecuteDeleteAsync for massive performance gain
            var deletedCount = await dbContext.Logs
                .Where(l => l.Timestamp < cutoffDate)
                .ExecuteDeleteAsync(stoppingToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation($"Successfully deleted {deletedCount} old logs.");
            }
            else
            {
                _logger.LogInformation("No old logs found to delete.");
            }
        }
    }
}
