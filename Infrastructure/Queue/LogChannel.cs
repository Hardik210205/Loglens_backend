using System.Threading.Channels;
using LogLens.Domain.Entities;

namespace LogLens.Infrastructure.Queue
{
    public class LogChannel
    {
        // unbounded channel; can be switched to bounded for backpressure
        public Channel<LogEntry> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<LogEntry>();
    }
}
