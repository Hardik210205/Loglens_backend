using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;
using LogLens.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace LogLens.Infrastructure.Repositories
{
    public class LogRepository : ILogRepository
    {
        private sealed class LogProjection
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
        }

        private readonly LogLensDbContext _context;

        public LogRepository(LogLensDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
        {
            await _context.Logs.AddRangeAsync(entries, cancellationToken);
        }

        public async Task AddAsync(LogEntry entry, CancellationToken cancellationToken = default)
        {
            await _context.Logs.AddAsync(entry, cancellationToken);
        }

        public async Task<IEnumerable<LogEntry>> GetLogsSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Logs
                .AsNoTracking()
                .Where(l => l.Timestamp >= since)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<LogEntry>> GetAllAsync(int? limit = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Logs
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .AsQueryable();
            if (limit.HasValue)
                query = query.Take(limit.Value);
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<LogEntry>> GetUnclusteredSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Logs
                .Where(l => l.Timestamp >= since && string.IsNullOrEmpty(l.ClusterId))
                .OrderBy(l => l.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<(int Hour, int Errors, int Warnings, int Info)>> GetLogCountsByHourAsync(
            DateTime since,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default)
        {
            var logs = await _context.Logs
                .Where(l => l.Timestamp >= since)
                .Select(l => new LogProjection
                {
                    Timestamp = l.Timestamp,
                    Level = l.Level
                })
                .ToListAsync(cancellationToken);

            var timeZone = ResolveTimeZone(timeZoneId);

            var groupedByHour = logs
                .GroupBy(log =>
                {
                    var utcTimestamp = NormalizeUtc(log.Timestamp);
                    var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, timeZone);
                    return localTime.Hour;
                })
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<(int Hour, int Errors, int Warnings, int Info)>();
            for (var h = 0; h < 24; h++)
            {
                var hourLogs = groupedByHour.TryGetValue(h, out var items)
                    ? items
                    : new List<LogProjection>();
                var errors = hourLogs.Count(l => l.Level == LogLevel.Error || l.Level == LogLevel.Critical);
                var warnings = hourLogs.Count(l => l.Level == LogLevel.Warning);
                var info = hourLogs.Count(l => l.Level == LogLevel.Information || l.Level == LogLevel.Debug || l.Level == LogLevel.Trace);
                result.Add((h, errors, warnings, info));
            }

            return result.OrderBy(x => x.Hour);
        }

        private static TimeZoneInfo ResolveTimeZone(string? requestedTimeZoneId)
        {
            if (!string.IsNullOrWhiteSpace(requestedTimeZoneId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(requestedTimeZoneId.Trim());
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            var tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "India Standard Time"
                : "Asia/Kolkata";

            return TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }

        private static DateTime NormalizeUtc(DateTime timestamp)
        {
            if (timestamp.Kind == DateTimeKind.Utc)
            {
                return timestamp;
            }

            if (timestamp.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
            }

            return timestamp.ToUniversalTime();
        }
    }
}
