using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Infrastructure.Queue;

namespace LogLens.Infrastructure.BackgroundServices
{
    public class LogQueueService : ILogQueueService
    {
        private readonly LogChannel _logChannel;

        public LogQueueService(LogChannel logChannel)
        {
            _logChannel = logChannel;
        }

        public Task EnqueueAsync(LogEntry entry)
        {
            // write directly to the infrastructure channel
            return _logChannel.Channel.Writer.WriteAsync(entry).AsTask();
        }
    }
}
