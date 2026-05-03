using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Domain.Entities;

namespace LogLens.Application.Interfaces
{
    public interface ILogAnalyticsService
    {
        Task<List<LogEntry>> GetErrorLogsForWindowAsync(DateTime since, CancellationToken cancellationToken = default);
        IReadOnlyList<ServiceWindowMetric> BuildServiceWindowMetrics(IReadOnlyList<LogEntry> logs, DateTime previousWindowStart, DateTime recentWindowStart);
        List<BucketErrorPoint> BuildErrorBuckets(IReadOnlyList<LogEntry> logs, int bucketMinutes, int bucketCount, DateTime nowUtc);
    }

    public class ServiceWindowMetric
    {
        public string ServiceName { get; init; } = string.Empty;
        public int RecentErrors { get; init; }
        public int PreviousErrors { get; init; }
        public double TrendPercent { get; init; }
    }

    public class BucketErrorPoint
    {
        public DateTime BucketStartUtc { get; init; }
        public int ErrorCount { get; init; }
    }
}