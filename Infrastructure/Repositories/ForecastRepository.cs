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
    public class ForecastRepository : IForecastRepository
    {
        private readonly LogLensDbContext _context;

        public ForecastRepository(LogLensDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Forecast forecast, CancellationToken cancellationToken = default)
        {
            await _context.AddAsync(forecast, cancellationToken);
        }

        public async Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Forecasts
                .Where(f => f.ForecastTime >= since)
                .CountAsync(cancellationToken);
        }
    }
}