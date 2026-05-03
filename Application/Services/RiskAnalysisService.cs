using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace LogLens.Application.Services
{
    public class RiskAnalysisService : IRiskAnalysisService
    {
        private readonly ILogAnalyticsService _logAnalyticsService;
        private readonly IIncidentRepository _incidentRepository;
        private readonly IIncidentService _incidentService;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan MetricsCacheTtl = TimeSpan.FromSeconds(5);

        public RiskAnalysisService(
            ILogAnalyticsService logAnalyticsService,
            IIncidentRepository incidentRepository,
            IIncidentService incidentService,
            IMemoryCache cache)
        {
            _logAnalyticsService = logAnalyticsService;
            _incidentRepository = incidentRepository;
            _incidentService = incidentService;
            _cache = cache;
        }

        public async Task<RiskAnalysisResultDto> AnalyzeSystemRiskAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var currentWindowStart = AnalyticsTime.FloorToBucket(now);
            var cacheKey = $"risk:{currentWindowStart.Ticks}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = MetricsCacheTtl;

                var firstWindowStart = currentWindowStart.AddMinutes(-(AnalyticsTime.BucketMinutes * 2));
                var since = currentWindowStart.AddMinutes(-(AnalyticsTime.BucketMinutes * 3));
                var errorLogs = await _logAnalyticsService.GetErrorLogsForWindowAsync(since, cancellationToken);

                if (errorLogs == null || errorLogs.Count == 0)
                {
                    return new RiskAnalysisResultDto
                    {
                        Score = 0,
                        Reason = "No recent errors. System healthy.",
                        AffectedService = "None",
                        PredictedWindow = "No immediate risk"
                    };
                }

                var perWindowScores = new List<double>(3);
                for (var i = 0; i < 3; i++)
                {
                    var recentWindowStart = firstWindowStart.AddMinutes(i * AnalyticsTime.BucketMinutes);
                    var previousWindowStart = recentWindowStart.AddMinutes(-AnalyticsTime.BucketMinutes);
                    var metrics = _logAnalyticsService.BuildServiceWindowMetrics(errorLogs, previousWindowStart, recentWindowStart);
                    var components = BuildRiskComponents(metrics);
                    perWindowScores.Add(CalculateWeightedRiskScore(components.RecentErrors, components.ErrorRateIncrease, components.ServiceImpact));
                }

                var smoothedRiskScore = perWindowScores.Count == 0
                    ? 0
                    : perWindowScores.Average();
                smoothedRiskScore = Sanitize(smoothedRiskScore);

                var previousStartForCurrent = currentWindowStart.AddMinutes(-AnalyticsTime.BucketMinutes);
                var currentMetrics = _logAnalyticsService.BuildServiceWindowMetrics(errorLogs, previousStartForCurrent, currentWindowStart);
                var currentComponents = BuildRiskComponents(currentMetrics);
                var finalScore = (int)Math.Round(Math.Clamp(smoothedRiskScore, 0, 100));

                if (finalScore > 70)
                {
                    await _incidentService.CreateRiskIncidentIfNeededAsync(
                        currentComponents.AffectedService,
                        currentWindowStart,
                        currentComponents.RecentErrors,
                        finalScore,
                        cancellationToken);
                }

                var reason = string.Join("\n", new[]
                {
                    $"Current window (5m): {currentComponents.RecentErrors} errors",
                    $"Previous window (5m): {currentComponents.PreviousErrors} errors",
                    $"Weighted factors => recentErrors={Math.Min(100, currentComponents.RecentErrors)}, errorRateIncrease={currentComponents.ErrorRateIncrease:0.##}, serviceImpact={currentComponents.ServiceImpact:0.##}",
                    $"Smoothed score (last 3 windows): {smoothedRiskScore:0.##}",
                    $"Top impacted service: {currentComponents.AffectedService}"
                });

                return new RiskAnalysisResultDto
                {
                    Score = finalScore,
                    Reason = reason,
                    AffectedService = currentComponents.AffectedService,
                    PredictedWindow = finalScore >= 85 ? "15 minutes" : finalScore >= 70 ? "30 minutes" : "No immediate risk"
                };
            }) ?? new RiskAnalysisResultDto();
        }

        public async Task<List<ServiceRiskInsightDto>> GetTopFailingServicesAsync(int top = 5, CancellationToken cancellationToken = default)
        {
            var safeTop = Math.Clamp(top, 1, 5);
            var currentWindowStart = AnalyticsTime.FloorToBucket(DateTime.UtcNow);
            var cacheKey = $"top-services:{currentWindowStart.Ticks}:{safeTop}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = MetricsCacheTtl;

                var previousWindowStart = currentWindowStart.AddMinutes(-AnalyticsTime.BucketMinutes);
                var errorLogs = await _logAnalyticsService.GetErrorLogsForWindowAsync(previousWindowStart, cancellationToken);
                var incidents24h = (await _incidentRepository.GetRecentAsync(DateTime.UtcNow.AddHours(-24), cancellationToken)).ToList();
                var metrics = _logAnalyticsService.BuildServiceWindowMetrics(errorLogs, previousWindowStart, currentWindowStart);

                return metrics
                    .Select(m => new ServiceRiskInsightDto
                    {
                        ServiceName = m.ServiceName,
                        ErrorRate = m.RecentErrors,
                        Trend = $"{m.TrendPercent:+0.##;-0.##;0}%",
                        IncidentCount = incidents24h.Count(i => string.Equals(i.ServiceName, m.ServiceName, StringComparison.OrdinalIgnoreCase))
                    })
                    .Where(i => i.ErrorRate > 0 || i.IncidentCount > 0)
                    .OrderByDescending(i => ParsePercent(i.Trend))
                    .ThenByDescending(i => i.ErrorRate)
                    .Take(safeTop)
                    .ToList();
            }) ?? new List<ServiceRiskInsightDto>();
        }

        public async Task<List<ErrorTrendPointDto>> GetErrorTrendPredictionAsync(
            int bucketMinutes = 5,
            int historyBuckets = 12,
            int projectionBuckets = 3,
            CancellationToken cancellationToken = default)
        {
            var safeBucketMinutes = bucketMinutes > 0 ? bucketMinutes : AnalyticsTime.BucketMinutes;
            var safeHistoryBuckets = historyBuckets > 0 ? historyBuckets : 5;
            var safeProjectionBuckets = Math.Clamp(projectionBuckets, 1, 12);
            var totalBuckets = safeHistoryBuckets + safeProjectionBuckets;
            var currentWindowStart = FloorToBucket(DateTime.UtcNow, safeBucketMinutes);
            var cacheKey = $"prediction:{currentWindowStart.Ticks}:{safeProjectionBuckets}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = MetricsCacheTtl;

                var since = currentWindowStart.AddMinutes(-(safeBucketMinutes * (safeHistoryBuckets - 1)));
                var errorLogs = await _logAnalyticsService.GetErrorLogsForWindowAsync(since, cancellationToken);
                var buckets = _logAnalyticsService.BuildErrorBuckets(
                    errorLogs,
                    safeBucketMinutes,
                    safeHistoryBuckets,
                    currentWindowStart);

                var results = new List<ErrorTrendPointDto>(totalBuckets);
                foreach (var bucket in buckets)
                {
                    results.Add(new ErrorTrendPointDto
                    {
                        BucketStartUtc = bucket.BucketStartUtc,
                        Label = bucket.BucketStartUtc.ToString("HH:mm"),
                        CurrentErrors = bucket.ErrorCount,
                        PredictedErrors = bucket.ErrorCount
                    });
                }

                var previous = buckets.Count >= 2 ? buckets[^2].ErrorCount : 0;
                var current = buckets.Count >= 1 ? buckets[^1].ErrorCount : 0;
                var projectionStart = (buckets.LastOrDefault()?.BucketStartUtc ?? currentWindowStart)
                    .AddMinutes(safeBucketMinutes);

                for (var i = 1; i <= safeProjectionBuckets; i++)
                {
                    var projectedBase = current + (current - previous);
                    var noise = 1 + ((Random.Shared.NextDouble() * 0.10) - 0.05);
                    var predicted = Math.Max(0, (int)Math.Round(projectedBase * noise));
                    var bucketStart = projectionStart.AddMinutes((i - 1) * safeBucketMinutes);

                    results.Add(new ErrorTrendPointDto
                    {
                        BucketStartUtc = bucketStart,
                        Label = bucketStart.ToString("HH:mm"),
                        CurrentErrors = 0,
                        PredictedErrors = predicted
                    });

                    previous = current;
                    current = predicted;
                }

                return results;
            }) ?? new List<ErrorTrendPointDto>();
        }

        private static DateTime FloorToBucket(DateTime value, int bucketMinutes)
        {
            var utc = AnalyticsTime.NormalizeUtc(value);
            var bucketTicks = TimeSpan.FromMinutes(bucketMinutes).Ticks;
            var ticks = utc.Ticks - (utc.Ticks % bucketTicks);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static (int RecentErrors, int PreviousErrors, double ErrorRateIncrease, double ServiceImpact, string AffectedService)
            BuildRiskComponents(IReadOnlyList<ServiceWindowMetric> metrics)
        {
            var previousWindowErrors = metrics.Sum(m => m.PreviousErrors);
            var recentWindowErrors = metrics.Sum(m => m.RecentErrors);
            var errorRateIncrease = CalculateTrendPercent(recentWindowErrors, previousWindowErrors);
            var topService = metrics
                .OrderByDescending(m => m.RecentErrors)
                .ThenByDescending(m => m.TrendPercent)
                .FirstOrDefault();

            var serviceImpact = topService == null || recentWindowErrors == 0
                ? 0
                : ((double)topService.RecentErrors / recentWindowErrors) * 100;

            var sanitizedIncrease = Sanitize(Math.Max(0, errorRateIncrease));
            var sanitizedImpact = Sanitize(Math.Clamp(serviceImpact, 0, 100));

            return (
                recentWindowErrors,
                previousWindowErrors,
                sanitizedIncrease,
                sanitizedImpact,
                topService?.ServiceName ?? "UnknownService");
        }

        private static double CalculateWeightedRiskScore(int recentErrors, double errorRateIncrease, double serviceImpact)
        {
            var boundedRecentErrors = Math.Clamp(recentErrors, 0, 100);
            var boundedErrorRateIncrease = Math.Clamp(errorRateIncrease, 0, 100);
            var boundedServiceImpact = Math.Clamp(serviceImpact, 0, 100);

            var score =
                (boundedRecentErrors * 0.6) +
                (boundedErrorRateIncrease * 0.3) +
                (boundedServiceImpact * 0.1);

            return Math.Clamp(score, 0, 100);
        }

        private static double CalculateTrendPercent(int recent, int previous)
        {
            if (previous == 0)
            {
                return recent == 0 ? 0 : 100;
            }

            return ((double)(recent - previous) / previous) * 100;
        }

        private static double ParsePercent(string percentText)
        {
            if (string.IsNullOrWhiteSpace(percentText))
            {
                return 0;
            }

            var cleaned = percentText.Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase);
            return double.TryParse(cleaned, out var value) ? value : 0;
        }

        private static double Sanitize(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            return value;
        }
    }
}