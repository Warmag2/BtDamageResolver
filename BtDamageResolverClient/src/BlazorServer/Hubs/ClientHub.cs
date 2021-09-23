using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.AspNetCore.SignalR;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Hubs
{
    public class ClientHub : Hub
    {
        public async Task ReceiveConnectionResponse(string connectionId, ConnectionResponse connectionResponse)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.ConnectionResponse, connectionResponse);
        }

        public async Task ReceiveDamageReport(string connectionId, List<DamageReport> damageReports)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.DamageReports, damageReports);
        }

        public async Task ReceiveErrorMessage(string connectionId, string errorMessage)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.ErrorMessage, errorMessage);
        }

        public async Task ReceiveGameOptions(string connectionId, GameOptions gameOptions)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.GameOptions, gameOptions);
        }

        public async Task ReceiveGameState(string connectionId, GameState gameState)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.GameState, gameState);
        }

        public async Task ReceivePlayerOptions(string connectionId, PlayerOptions playerOptions)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.PlayerOptions, playerOptions);
        }

        public async Task ReceiveTargetNumberUpdates(string connectionId, List<TargetNumberUpdate> targetNumberUpdates)
        {
            await Clients.Client(connectionId).SendAsync(EventNames.TargetNumbers, targetNumberUpdates);
        }
    }
}