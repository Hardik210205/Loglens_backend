using System;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Domain.Entities;

namespace LogLens.Application.Interfaces
{
    public interface IAlertRepository
    {
        Task AddAsync(Alert alert, CancellationToken cancellationToken = default);
        Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default);
    }
}