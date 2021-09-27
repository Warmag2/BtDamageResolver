using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication
{
    public class ClientToServerCommunicator : RedisClientToServerCommunicator
    {
        private readonly HubConnection _hubConnection;

        public ClientToServerCommunicator(ILogger logger, string connectionString, string playerId, HubConnection hubConnection) : base(logger, connectionString, playerId)
        {
            _hubConnection = hubConnection;
        }

        public override Task<bool> HandleConnectionResponse(byte[] connectionResponse, Guid correlationId)
        {
            _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<ConnectionResponse>(connectionResponse))).Ignore();

            _hubConnection.SendAsync(EventNames.ConnectionResponse, _hubConnection.ConnectionId, connectionResponse).Ignore();

            return Task.FromResult(true);
        }

        public override Task<bool> HandleDamageReports(byte[] damageReports, Guid correlationId)
        {
            _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<List<DamageReport>>(damageReports))).Ignore();

            _hubConnection.SendAsync(EventNames.DamageReports, _hubConnection.ConnectionId, damageReports).Ignore();

            return Task.FromResult(true);
        }

        public override Task<bool> HandleErrorMessage(byte[] errorMessage, Guid correlationId)
        {
            _hubConnection.SendAsync(EventNames.ErrorMessage, _hubConnection.ConnectionId, errorMessage).Ignore();

            return Task.FromResult(true);
        }

        public override Task<bool> HandleGameOptions(byte[] gameOptions, Guid correlationId)
        {
            _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<GameOptions>(gameOptions))).Ignore();

            _hubConnection.SendAsync(EventNames.GameOptions, _hubConnection.ConnectionId, gameOptions).Ignore();

            return Task.FromResult(true);
        }

        public override Task<bool> HandleGameState(byte[] gameState, Guid correlationId)
        {
            _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<GameState>(gameState))).Ignore();

            _hubConnection.SendAsync(EventNames.GameState, _hubConnection.ConnectionId, gameState).Ignore();

            return Task.FromResult(true);
        }

        public override Task<bool> HandlePlayerOptions(byte[] playerOptions, Guid correlationId)
        {
            _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<PlayerOptions>(playerOptions))).Ignore();

            _hubConnection.SendAsync(EventNames.PlayerOptions, _hubConnection.ConnectionId, playerOptions).Ignore();

            return Task.FromResult(true);

        }

        public override Task<bool> HandleTargetNumberUpdates(byte[] targetNumbers, Guid correlationId)
        {
            _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<List<TargetNumberUpdate>>(targetNumbers))).Ignore();

            _hubConnection.SendAsync(EventNames.TargetNumbers, _hubConnection.ConnectionId, targetNumbers).Ignore();

            return Task.FromResult(true);
        }
    }
}