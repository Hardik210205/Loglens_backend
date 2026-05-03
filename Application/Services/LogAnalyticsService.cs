using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;

namespace LogLens.Application.Services
{
    public class LogAnalyticsService : ILogAnalyticsService
    {
        private readonly ILogRepository _logRepository;

        public LogAnalyticsService(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<List<LogEntry>> GetErrorLogsForWindowAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            var normalizedSince = AnalyticsTime.NormalizeUtc(since);
            var logs = await _logRepository.GetLogsSinceAsync(normalizedSince, cancellationToken);
            return logs
                .Select(l =>
                {
                    l.Timestamp = AnalyticsTime.NormalizeUtc(l.Timestamp);
                    return l;
                })
                .Where(IsError)
                .ToList();
        }

        public IReadOnlyList<ServiceWindowMetric> BuildServiceWindowMetrics(IReadOnlyList<LogEntry> logs, DateTime previousWindowStart, DateTime recentWindowStart)
        {
            var normalizedPreviousStart = AnalyticsTime.FloorToBucket(previousWindowStart);
            var normalizedRecentStart = AnalyticsTime.FloorToBucket(recentWindowStart);
            var normalizedRecentEnd = normalizedRecentStart.Add(AnalyticsTime.BucketSize);

            var metrics = logs
                .GroupBy(l => NormalizeServiceName(l.ServiceName))
                .Select(g =>
                {
                    var recentErrors = g.Count(l => l.Timestamp >= normalizedRecentStart && l.Timestamp < normalizedRecentEnd);
                    var previousErrors = g.Count(l => l.Timestamp >= normalizedPreviousStart && l.Timestamp < normalizedRecentStart);

                    return new ServiceWindowMetric
                    {
                        ServiceName = g.Key,
                        RecentErrors = recentErrors,
                        PreviousErrors = previousErrors,
                        TrendPercent = CalculateTrendPercent(recentErrors, previousErrors)
                    };
                })
                .Where(m => m.RecentErrors > 0 || m.PreviousErrors > 0)
                .OrderByDescending(m => m.TrendPercent)
                .ThenByDescending(m => m.RecentErrors)
                .ToList();

            return metrics;
        }

        public List<BucketErrorPoint> BuildErrorBuckets(IReadOnlyList<LogEntry> logs, int bucketMinutes, int bucketCount, DateTime nowUtc)
        {
            var effectiveBucketMinutes = AnalyticsTime.BucketMinutes;
            var effectiveBucketCount = Math.Max(2, bucketCount);
            var bucketSize = TimeSpan.FromMinutes(effectiveBucketMinutes);
            var latestBucket = FloorToBucket(nowUtc, bucketSize);
            var firstBucket = latestBucket.AddMinutes(-(effectiveBucketMinutes * (effectiveBucketCount - 1)));

            var countByBucket = logs
                .GroupBy(l => FloorToBucket(AnalyticsTime.NormalizeUtc(l.Timestamp), bucketSize))
                .ToDictionary(g => g.Key, g => g.Count());

            var points = new List<BucketErrorPoint>(effectiveBucketCount);
            for (var i = 0; i < effectiveBucketCount; i++)
            {
                var bucketStart = firstBucket.AddMinutes(i * effectiveBucketMinutes);
                countByBucket.TryGetValue(bucketStart, out var count);
                points.Add(new BucketErrorPoint
                {
                    BucketStartUtc = bucketStart,
                    ErrorCount = count
                });
            }

            return points;
        }

        private static bool IsError(LogEntry log) =>
            log.Level == LogLevel.Error || log.Level == LogLevel.Critical;

        private static string NormalizeServiceName(string? serviceName) =>
            string.IsNullOrWhiteSpace(serviceName) ? "UnknownService" : serviceName.Trim();

        private static double CalculateTrendPercent(int recent, int previous)
        {
            if (previous == 0)
            {
                return recent == 0 ? 0 : 100;
            }

            return ((double)(recent - previous) / previous) * 100;
        }

        private static DateTime FloorToBucket(DateTime value, TimeSpan bucket)
        {
            var utc = AnalyticsTime.NormalizeUtc(value);
            var ticks = utc.Ticks - (utc.Ticks % bucket.Ticks);
            return new DateTime(ticks, DateTimeKind.Utc);
        }
    }
}