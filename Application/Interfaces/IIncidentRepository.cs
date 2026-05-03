using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Domain.Entities;

namespace LogLens.Application.Interfaces
{
    public interface IIncidentRepository
    {
        Task AddAsync(Incident incident, CancellationToken cancellationToken = default);
        Task<IEnumerable<Incident>> GetRecentAsync(DateTime since, CancellationToken cancellationToken = default);
        Task<Incident?> FindActiveAsync(string template, string serviceName, DateTime minLastSeenUtc, CancellationToken cancellationToken = default);
        Task<Incident?> FindRecentByServiceAsync(string serviceName, DateTime minStartTimeUtc, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}