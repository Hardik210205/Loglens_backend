using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Infrastructure.Data;
using LogLens.ML.Clustering;
using LogLens.ML.Forecasting;
using Microsoft.AspNetCore.SignalR;

namespace LogLens.Infrastructure.BackgroundServices
{
    public class MlProcessingService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<MlProcessingService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(60);
        private object? _hubContext;
        private Type? _logHubType;

        public MlProcessingService(IServiceProvider services, ILogger<MlProcessingService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ML Processing Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessLogsAsync(stoppingToken);
                    await Task.Delay(_processingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ML processing");
                    // Continue despite errors
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            _logger.LogInformation("ML Processing Service stopped");
        }

        private async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
            var incidentClusteringService = scope.ServiceProvider.GetRequiredService<IIncidentClusteringService>();
            var riskAnalysisService = scope.ServiceProvider.GetRequiredService<IRiskAnalysisService>();
            var forecastRepository = scope.ServiceProvider.GetRequiredService<IForecastRepository>();
            var alertRepository = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
            var mlClusteringService = scope.ServiceProvider.GetRequiredService<IncidentClusteringService>();
            var mlForecastService = scope.ServiceProvider.GetRequiredService<WarningForecastService>();

            var recentLogs = (await logRepository.GetUnclusteredSinceAsync(
                DateTime.UtcNow.AddMinutes(-15), cancellationToken)).ToList();

            if (recentLogs.Count >= 5)
            {
                _logger.LogInformation("Processing {LogCount} unclustered logs", recentLogs.Count);

                var groupedLogs = mlClusteringService.GroupLogsIntoIncidents(recentLogs);
                var incidentCount = 0;

                foreach (var group in groupedLogs)
                {
                    if (group.Count == 0)
                    {
                        continue;
                    }

                    var incidents = await incidentClusteringService.AnalyzeAndCreateIncidentsAsync(group);
                    incidentCount += incidents.Count;
                }

                _logger.LogInformation("Created or updated {IncidentCount} incidents", incidentCount);
            }
            else if (recentLogs.Count > 0)
            {
                _logger.LogInformation("Skipping clustering because only {LogCount} unclustered logs are available", recentLogs.Count);
            }

            var risk = await riskAnalysisService.AnalyzeSystemRiskAsync(cancellationToken);
            if (risk.Score > 80)
            {
                await BroadcastRiskAlertAsync(scope, risk.Score, risk.AffectedService);
            }

            _logger.LogInformation("Running forecast analysis");
            var recentLogsForForecast = (await logRepository.GetLogsSinceAsync(DateTime.UtcNow.AddHours(-24), cancellationToken)).ToList();
            if (recentLogsForForecast.Count >= 10)
            {
                var forecasts = mlForecastService.ForecastWarnings(recentLogsForForecast, 24);
                if (forecasts.Count > 0)
                {
                    foreach (var forecast in forecasts)
                    {
                        await forecastRepository.AddAsync(new Forecast
                        {
                            ForecastTime = forecast.Timestamp,
                            PredictedValue = forecast.Value,
                            Notes = $"Confidence: [{forecast.ConfidenceLower:F2}, {forecast.ConfidenceUpper:F2}]"
                        }, cancellationToken);
                    }

                    var alerts = mlForecastService.GenerateAlertsFromForecast(forecasts);
                    foreach (var alert in alerts)
                    {
                        await alertRepository.AddAsync(alert, cancellationToken);
                    }

                    _logger.LogInformation("Generated {ForecastCount} forecasts and {AlertCount} forecast-based alerts", forecasts.Count, alerts.Count);
                }
            }

            // Save changes
            var dbContext = scope.ServiceProvider.GetRequiredService<LogLensDbContext>();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task BroadcastRiskAlertAsync(IServiceScope scope, int score, string serviceName)
        {
            try
            {
                if (_logHubType == null)
                {
                    _logHubType = Type.GetType("LogLens.API.Hubs.LogHub, LogLens.API");
                    if (_logHubType == null)
                    {
                        _logger.LogWarning("LogHub type not found. Risk alerts will not be broadcast.");
                        return;
                    }
                }

                if (_hubContext == null)
                {
                    var iHubContextBaseType =
                        Type.GetType("Microsoft.AspNetCore.SignalR.IHubContext`1, Microsoft.AspNetCore.SignalR.Core") ??
                        Type.GetType("Microsoft.AspNetCore.SignalR.IHubContext`1, Microsoft.AspNetCore.SignalR");

                    if (iHubContextBaseType == null)
                    {
                        _logger.LogWarning("IHubContext type not found. Risk alerts will not be broadcast.");
                        return;
                    }

                    var iHubContextType = iHubContextBaseType.MakeGenericType(_logHubType);
                    _hubContext = scope.ServiceProvider.GetService(iHubContextType);
                    if (_hubContext == null)
                    {
                        _logger.LogWarning("IHubContext unavailable. Risk alerts will not be broadcast.");
                        return;
                    }
                }

                var payload = new
                {
                    type = "SYSTEM_RISK",
                    message = $"System risk increased to {score}%",
                    service = serviceName
                };

                var clientsProperty = _hubContext.GetType().GetProperty("Clients");
                var clients = clientsProperty?.GetValue(_hubContext);
                var allProperty = clients?.GetType().GetProperty("All");
                var all = allProperty?.GetValue(clients);
                var sendCoreAsyncMethod = all?.GetType().GetMethod(
                    "SendCoreAsync",
                    new[] { typeof(string), typeof(object[]), typeof(CancellationToken) });

                if (sendCoreAsyncMethod == null)
                {
                    return;
                }

                var sendTask = sendCoreAsyncMethod.Invoke(
                    all,
                    new object[] { "ReceiveAlerts", new object[] { payload }, CancellationToken.None }) as Task;

                if (sendTask != null)
                {
                    await sendTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast risk alert.");
            }
        }
    }
}
