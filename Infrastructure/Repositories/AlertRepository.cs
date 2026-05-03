using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LogLens.Infrastructure.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly LogLensDbContext _context;

        public AlertRepository(LogLensDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
        {
            await _context.AddAsync(alert, cancellationToken);
        }

        public async Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Alerts
                .Where(a => a.Timestamp >= since)
                .CountAsync(cancellationToken);
        }
    }
}