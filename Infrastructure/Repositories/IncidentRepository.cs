using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LogLens.Infrastructure.Repositories
{
    public class IncidentRepository : IIncidentRepository
    {
        private readonly LogLensDbContext _context;

        public IncidentRepository(LogLensDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            await _context.AddAsync(incident, cancellationToken);
        }

        public async Task<IEnumerable<Incident>> GetRecentAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Incidents
                .Include(i => i.LogEntries)
                .Where(i => i.LastSeen >= since)
                .OrderByDescending(i => i.LastSeen)
                .ToListAsync(cancellationToken);
        }

        public async Task<Incident?> FindActiveAsync(string template, string serviceName, DateTime minLastSeenUtc, CancellationToken cancellationToken = default)
        {
            return await _context.Incidents
                .Include(i => i.LogEntries)
                .Where(i => i.Template == template
                            && i.ServiceName == serviceName
                            && i.Status == "Active"
                            && i.LastSeen >= minLastSeenUtc)
                .OrderByDescending(i => i.LastSeen)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Incident?> FindRecentByServiceAsync(string serviceName, DateTime minStartTimeUtc, CancellationToken cancellationToken = default)
        {
            return await _context.Incidents
                .Where(i => i.ServiceName == serviceName
                            && i.Status == "Active"
                            && i.StartTimeUtc >= minStartTimeUtc)
                .OrderByDescending(i => i.StartTimeUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
