using System.Threading.Tasks;
using LogLens.Application.DTOs;

namespace LogLens.Application.Interfaces
{
    public interface ILogService
    {
        Task EnqueueAsync(IngestLogRequest log);
    }
}
