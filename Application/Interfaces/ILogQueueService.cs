using System.Threading.Tasks;
using LogLens.Domain.Entities;

namespace LogLens.Application.Interfaces
{
    public interface ILogQueueService
    {
        Task EnqueueAsync(LogEntry entry);
    }
}