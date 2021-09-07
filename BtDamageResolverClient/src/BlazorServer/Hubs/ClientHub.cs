using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Hubs
{
    public class ClientHub : Hub
    {
        public async Task ReceiveDamageReport(string connectionId, byte[] damageReports)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveDamageReports", damageReports);
        }

        public async Task ReceiveErrorMessage(string connectionId, string errorMessage)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveErrorMessage", errorMessage);
        }

        public async Task ReceiveGameOptions(string connectionId, byte[] gameOptions)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveGameOptions", gameOptions);
        }

        public async Task ReceiveGameState(string connectionId, byte[] gameState)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveGameState", gameState);
        }

        public async Task ReceiveTargetNumberUpdates(string connectionId, byte[] targetNumberUpdates)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveTargetNumberUpdates", targetNumberUpdates);
        }
    }
}