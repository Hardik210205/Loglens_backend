namespace LogLens.Infrastructure.Hubs
{
    public interface ILogsHub
    {
        // Marker interface for LogHub
        // Allows Infrastructure layer to reference SignalR hub without circular dependency
        Task ReceiveLogAsync(object logDto);
    }
}
