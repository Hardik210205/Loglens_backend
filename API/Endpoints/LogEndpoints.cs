using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using DomainLogLevel = LogLens.Domain.Enums.LogLevel;

namespace LogLens.API.Endpoints
{
    public static class LogEndpoints
    {
        private sealed class LogEndpointLoggerCategory { }

        public static void MapLogEndpoints(this WebApplication app)
        {
            static Task<IResult> IngestAsync(
                IngestLogRequest log,
                HttpContext context,
                ILogQueueService logQueueService,
                ILogger<LogEndpointLoggerCategory> logger)
            {
                return ExecuteAsync();

                async Task<IResult> ExecuteAsync()
                {
                    try
                    {
                        if (!context.Items.TryGetValue("ServiceId", out var serviceIdValue) || serviceIdValue is not Guid serviceId)
                        {
                            return Results.BadRequest(new { error = "Missing service context from middleware." });
                        }

                        if (!context.Items.TryGetValue("ServiceName", out var serviceNameValue) || serviceNameValue is not string serviceName || string.IsNullOrWhiteSpace(serviceName))
                        {
                            return Results.BadRequest(new { error = "Missing service context from middleware." });
                        }

                        var entry = new LogEntry
                        {
                            Timestamp = log.Timestamp == default ? DateTime.UtcNow : log.Timestamp,
                            Level = Enum.TryParse<DomainLogLevel>(log.LogLevel, ignoreCase: true, out var parsedLevel)
                                ? parsedLevel
                                : DomainLogLevel.Information,
                            Message = string.IsNullOrWhiteSpace(log.Message) ? "Log received" : log.Message.Trim(),
                            ServiceName = serviceName,
                            ServiceId = serviceId,
                            TraceId = log.TraceId
                        };

                        await logQueueService.EnqueueAsync(entry);
                        return Results.Accepted();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to enqueue log for service {ServiceName}", log.ServiceName);
                        return Results.Problem(
                            detail: "Failed to ingest log entry.",
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                }
            }

            // Ingestion endpoint used by external services
            app.MapPost("/api/logs", IngestAsync)
            .WithName("IngestLog")
            .WithTags("Logs");

            app.MapPost("/logs", IngestAsync)
            .WithName("IngestLogAlias")
            .WithTags("Logs");

            app.MapGet("/api/logs", async (ILogRepository logRepo, int? limit) =>
            {
                try
                {
                    var logs = (await logRepo.GetAllAsync(limit ?? 100)) ?? Enumerable.Empty<LogLens.Domain.Entities.LogEntry>();
                    var dtos = logs.Select(l => new LogResponseDto(
                        l.Id,
                        l.Timestamp,
                        l.Level.ToString(),
                        l.Message,
                        l.Metadata
                    )).ToList();

                    return Results.Ok(dtos);
                }
                catch
                {
                    return Results.Ok(new List<LogResponseDto>());
                }
            })
            .WithName("GetLogs")
            .WithTags("Logs");

            app.MapGet("/logs", async (ILogRepository logRepo, int? limit) =>
            {
                try
                {
                    var logs = (await logRepo.GetAllAsync(limit ?? 100)) ?? Enumerable.Empty<LogLens.Domain.Entities.LogEntry>();
                    var dtos = logs.Select(l => new LogResponseDto(
                        l.Id,
                        l.Timestamp,
                        l.Level.ToString(),
                        l.Message,
                        l.Metadata
                    )).ToList();

                    return Results.Ok(dtos);
                }
                catch
                {
                    return Results.Ok(new List<LogResponseDto>());
                }
            })
            .WithName("GetLogsAlias")
            .WithTags("Logs");

            app.MapGet("/api/incidents", async (IIncidentService incidentService) =>
            {
                try
                {
                    var incidents = await incidentService.GetIncidentsForLast24HoursAsync();
                    var dtos = incidents.Select(i => new IncidentResponseDto(
                        i.Id,
                        i.StartTimeUtc,
                        i.Severity.ToString().ToLowerInvariant(),
                        i.Title,
                        i.Template,
                        i.ServiceName,
                        i.ErrorCount,
                        i.WarningCount,
                        i.FirstSeen,
                        i.LastSeen,
                        i.SuggestedCause,
                        i.Status
                    )).ToList();

                    return Results.Ok(dtos);
                }
                catch
                {
                    return Results.Ok(new List<IncidentResponseDto>());
                }
            })
            .WithName("GetIncidents")
            .WithTags("Incidents");

            app.MapGet("/incidents", async (IIncidentService incidentService) =>
            {
                try
                {
                    var incidents = await incidentService.GetIncidentsForLast24HoursAsync();
                    var dtos = incidents.Select(i => new IncidentResponseDto(
                        i.Id,
                        i.StartTimeUtc,
                        i.Severity.ToString().ToLowerInvariant(),
                        i.Title,
                        i.Template,
                        i.ServiceName,
                        i.ErrorCount,
                        i.WarningCount,
                        i.FirstSeen,
                        i.LastSeen,
                        i.SuggestedCause,
                        i.Status
                    )).ToList();

                    return Results.Ok(dtos);
                }
                catch
                {
                    return Results.Ok(new List<IncidentResponseDto>());
                }
            })
            .WithName("GetIncidentsLast24h")
            .WithTags("Incidents");

            app.MapGet("/api/insights/risk", async (IRiskAnalysisService riskAnalysisService) =>
            {
                try
                {
                    var result = await riskAnalysisService.AnalyzeSystemRiskAsync();
                    return Results.Ok(result);
                }
                catch
                {
                    return Results.Ok(new RiskAnalysisResultDto
                    {
                        Score = 0,
                        Reason = "No data available",
                        AffectedService = "UnknownService",
                        PredictedWindow = "No immediate risk"
                    });
                }
            })
            .WithName("GetSystemRisk")
            .WithTags("Insights");

            app.MapGet("/api/insights/services", async (IRiskAnalysisService riskAnalysisService) =>
            {
                try
                {
                    var result = await riskAnalysisService.GetTopFailingServicesAsync();
                    return Results.Ok(result ?? new List<ServiceRiskInsightDto>());
                }
                catch
                {
                    return Results.Ok(new List<ServiceRiskInsightDto>());
                }
            })
            .WithName("GetTopFailingServices")
            .WithTags("Insights");

            app.MapGet("/api/services/top-failing", async (IRiskAnalysisService riskAnalysisService) =>
            {
                try
                {
                    var result = await riskAnalysisService.GetTopFailingServicesAsync();
                    return Results.Ok(result ?? new List<ServiceRiskInsightDto>());
                }
                catch
                {
                    return Results.Ok(new List<ServiceRiskInsightDto>());
                }
            })
            .WithName("GetTopFailingServicesAlias")
            .WithTags("Insights");

            app.MapGet("/api/insights/prediction", async (
                IRiskAnalysisService riskAnalysisService,
                int? bucketMinutes,
                int? historyBuckets,
                int? projectionBuckets) =>
            {
                try
                {
                    var result = await riskAnalysisService.GetErrorTrendPredictionAsync(
                        bucketMinutes ?? 5,
                        historyBuckets ?? 12,
                        projectionBuckets ?? 3);

                    return Results.Ok(result ?? new List<ErrorTrendPointDto>());
                }
                catch
                {
                    return Results.Ok(new List<ErrorTrendPointDto>());
                }
            })
            .WithName("GetErrorTrendPrediction")
            .WithTags("Insights");

            app.MapGet("/api/stats/heatmap", async (ILogRepository logRepo, HttpContext httpContext) =>
            {
                try
                {
                    var since = DateTime.UtcNow.AddHours(-24);
                    var requestedTimeZone = httpContext.Request.Headers["X-Timezone"].FirstOrDefault();
                    var counts = (await logRepo.GetLogCountsByHourAsync(since, requestedTimeZone))
                        ?? Enumerable.Empty<(int Hour, int Errors, int Warnings, int Info)>();
                    var dtos = counts.Select(c => new HeatmapResponseDto(
                        $"{c.Hour:D2}:00",
                        c.Errors,
                        c.Warnings,
                        c.Info
                    )).ToList();

                    return Results.Ok(dtos);
                }
                catch
                {
                    return Results.Ok(new List<HeatmapResponseDto>());
                }
            })
            .WithName("GetHeatmap")
            .WithTags("Stats");

            app.MapGet("/api/stats/dashboard", async (
                ILogRepository logRepo,
                IIncidentRepository incidentRepo,
                IAlertRepository alertRepo,
                IForecastRepository forecastRepo) =>
            {
                try
                {
                    var since = DateTime.UtcNow.AddHours(-24);
                    var logs24h = ((await logRepo.GetLogsSinceAsync(since)) ?? Enumerable.Empty<LogLens.Domain.Entities.LogEntry>()).Count();
                    var incidents = ((await incidentRepo.GetRecentAsync(since)) ?? Enumerable.Empty<LogLens.Domain.Entities.Incident>()).ToList();
                    var activeIncidents = incidents.Count(i => string.Equals(i.Status, "Active", StringComparison.OrdinalIgnoreCase));
                    var alerts24h = Math.Max(0, await alertRepo.GetCountSinceAsync(since));
                    var healthPercent = logs24h > 0
                        ? Math.Max(0, 100 - (int)((double)(incidents.Count * 5) / logs24h * 100))
                        : 100;
                    var dto = new DashboardStatsDto(logs24h, activeIncidents, alerts24h, Math.Min(100, healthPercent));
                    return Results.Ok(dto);
                }
                catch
                {
                    return Results.Ok(new DashboardStatsDto(
                        TotalLogs24h: 0,
                        ActiveIncidents: 0,
                        PendingAlerts24h: 0,
                        SystemHealthPercent: 100));
                }
            })
            .WithName("GetDashboardStats")
            .WithTags("Stats");

            app.MapGet("/api/dashboard/stats", async (
                ILogRepository logRepo,
                IIncidentRepository incidentRepo,
                IAlertRepository alertRepo,
                IForecastRepository forecastRepo) =>
            {
                try
                {
                    var since = DateTime.UtcNow.AddHours(-24);
                    var logs24h = ((await logRepo.GetLogsSinceAsync(since)) ?? Enumerable.Empty<LogLens.Domain.Entities.LogEntry>()).Count();
                    var incidents = ((await incidentRepo.GetRecentAsync(since)) ?? Enumerable.Empty<LogLens.Domain.Entities.Incident>()).ToList();
                    var activeIncidents = incidents.Count(i => string.Equals(i.Status, "Active", StringComparison.OrdinalIgnoreCase));
                    var alerts24h = Math.Max(0, await alertRepo.GetCountSinceAsync(since));
                    var healthPercent = logs24h > 0
                        ? Math.Max(0, 100 - (int)((double)(incidents.Count * 5) / logs24h * 100))
                        : 100;

                    return Results.Ok(new DashboardStatsDto(logs24h, activeIncidents, alerts24h, Math.Min(100, healthPercent)));
                }
                catch
                {
                    return Results.Ok(new DashboardStatsDto(
                        TotalLogs24h: 0,
                        ActiveIncidents: 0,
                        PendingAlerts24h: 0,
                        SystemHealthPercent: 100));
                }
            })
            .WithName("GetDashboardStatsAlias")
            .WithTags("Stats");

            app.MapGet("/api/stats/ai", async (
                IIncidentRepository incidentRepo,
                IForecastRepository forecastRepo,
                IIncidentClusteringService clusteringService,
                IForecastService forecastService) =>
            {
                try
                {
                    var since = DateTime.UtcNow.AddHours(-24);
                    var incidents = (await incidentRepo.GetRecentAsync(since)) ?? Enumerable.Empty<LogLens.Domain.Entities.Incident>();
                    var incidentCount = incidents.Count();
                    var forecastCount = Math.Max(0, await forecastRepo.GetCountSinceAsync(since));
                    var clusteringAccuracy = (int)(await clusteringService.GetClusteringAccuracyAsync() * 100);
                    var forecastingAccuracy = (int)(await forecastService.GetForecastAccuracyAsync() * 100);
                    var dto = new AIStatusDto(
                        ClusteringAccuracy: clusteringAccuracy,
                        ForecastingAccuracy: forecastingAccuracy,
                        IncidentsDetected: incidentCount,
                        ForecastsGenerated: forecastCount,
                        LastUpdate: DateTime.UtcNow
                    );
                    return Results.Ok(dto);
                }
                catch
                {
                    return Results.Ok(new AIStatusDto(
                        ClusteringAccuracy: 0,
                        ForecastingAccuracy: 0,
                        IncidentsDetected: 0,
                        ForecastsGenerated: 0,
                        LastUpdate: DateTime.UtcNow));
                }
            })
            .WithName("GetAIStatus")
            .WithTags("Stats");
        }
    }
}
