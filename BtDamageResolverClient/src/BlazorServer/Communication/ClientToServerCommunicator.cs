using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication
{
    public class ClientToServerCommunicator : RedisClientToServerCommunicator
    {
        private readonly HubConnection _hubConnection;

        public ClientToServerCommunicator(ILogger logger, string connectionString, string playerId, HubConnection hubConnection) : base(logger, connectionString, playerId)
        {
            _hubConnection = hubConnection;
        }

        public override async Task<bool> HandleConnectionResponse(ConnectionResponse connectionResponse, Guid correlationId)
        {
            await _hubConnection.SendAsync(EventNames.ConnectionResponse, _hubConnection.ConnectionId, connectionResponse);

            return true;
        }

        public override async Task<bool> HandleDamageReports(List<DamageReport> damageReports, Guid correlationId)
        {
            await _hubConnection.SendAsync(EventNames.DamageReports, _hubConnection.ConnectionId, damageReports);
            
            return true;
        }

        public override async Task<bool> HandleErrorMessage(string errorMessage, Guid correlationId)
        {
            await _hubConnection.SendAsync(EventNames.ErrorMessage, _hubConnection.ConnectionId, errorMessage);

            return true;
        }

        public override async Task<bool> HandleGameOptions(GameOptions gameOptions, Guid correlationId)
        {
            await _hubConnection.SendAsync(EventNames.GameOptions, _hubConnection.ConnectionId, gameOptions);
            
            return true;
        }

        public override async Task<bool> HandleGameState(GameState gameState, Guid correlationId)
        {
            await _hubConnection.SendAsync(EventNames.GameState, _hubConnection.ConnectionId, gameState);
            
            return true;
        }

        public override async Task<bool> HandlePlayerOptions(PlayerOptions playerOptions, Guid correlationId)
        {
            await _hubConnection.SendAsync(EventNames.PlayerOptions, _hubConnection.ConnectionId, playerOptions);

            return true;

        }

        public override async Task<bool> HandleTargetNumberUpdates(List<TargetNumberUpdate> targetNumbers, Guid correlationId)
        {
            await _hubConnection.SendAsync("ReceiveTargetNumberUpdates", _hubConnection.ConnectionId, targetNumbers);
            
            return true;
        }
    }
}