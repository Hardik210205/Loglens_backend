using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LogLens.API.Hubs
{
    public class LogHub : Hub
    {
        // clients will receive logs via ReceiveLogs method invoked by server
        public const string ReceiveLogsMethod = "ReceiveLogs";
        public const string ReceiveAlertsMethod = "ReceiveAlerts";
        public const string ReceiveIncidentsMethod = "ReceiveIncidents";

        public override Task OnConnectedAsync()
        {
            // clients could send subscribe messages if needed
            return base.OnConnectedAsync();
        }
    }
}
