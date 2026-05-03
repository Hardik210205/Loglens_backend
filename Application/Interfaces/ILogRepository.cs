using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Domain.Entities;

namespace LogLens.Application.Interfaces
{
    public interface ILogRepository
    {
        Task AddRangeAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);
        Task AddAsync(LogEntry entry, CancellationToken cancellationToken = default);
        Task<IEnumerable<LogEntry>> GetLogsSinceAsync(DateTime since, CancellationToken cancellationToken = default);
        Task<IEnumerable<LogEntry>> GetAllAsync(int? limit = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<LogEntry>> GetUnclusteredSinceAsync(DateTime since, CancellationToken cancellationToken = default);
        Task<IEnumerable<(int Hour, int Errors, int Warnings, int Info)>> GetLogCountsByHourAsync(
            DateTime since,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default);
    }
}