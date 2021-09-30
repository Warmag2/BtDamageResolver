using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Microsoft.AspNetCore.SignalR;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Hubs
{
    public class ClientHub : Hub
    {
        public async Task ConnectionResponse(string connectionId, byte[] connectionResponse)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.ConnectionResponse, connectionResponse);
        }

        public async Task DamageReports(string connectionId, byte[] damageReports)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.DamageReports, damageReports);
        }

        public async Task ErrorMessage(string connectionId, byte[] errorMessage)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.ErrorMessage, errorMessage);
        }

        public async Task GameEntries(string connectionId, byte[] gameEntries)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.GameEntries, gameEntries);
        }

        public async Task GameOptions(string connectionId, byte[] gameOptions)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.GameOptions, gameOptions);
        }

        public async Task GameState(string connectionId, byte[] gameState)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.GameState, gameState);
        }

        public async Task PlayerOptions(string connectionId, byte[] playerOptions)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.PlayerOptions, playerOptions);
        }

        public async Task TargetNumbers(string connectionId, byte[] targetNumberUpdates)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.TargetNumbers, targetNumberUpdates);
        }
    }
}