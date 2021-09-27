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

        /// <inheritdoc />
        public override async Task<bool> HandleConnectionResponse(byte[] connectionResponse, Guid correlationId)
        {
            await _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<ConnectionResponse>(connectionResponse)));

            await _hubConnection.SendAsync(EventNames.ConnectionResponse, _hubConnection.ConnectionId, connectionResponse);

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleDamageReports(byte[] damageReports, Guid correlationId)
        {
            await _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<List<DamageReport>>(damageReports)));

            await _hubConnection.SendAsync(EventNames.DamageReports, _hubConnection.ConnectionId, damageReports);

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleErrorMessage(byte[] errorMessage, Guid correlationId)
        {
            await _hubConnection.SendAsync(EventNames.ErrorMessage, _hubConnection.ConnectionId, errorMessage);

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGameOptions(byte[] gameOptions, Guid correlationId)
        {
            await _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<GameOptions>(gameOptions)));

            await _hubConnection.SendAsync(EventNames.GameOptions, _hubConnection.ConnectionId, gameOptions);

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGameState(byte[] gameState, Guid correlationId)
        {
            await _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<GameState>(gameState)));

            await _hubConnection.SendAsync(EventNames.GameState, _hubConnection.ConnectionId, gameState);

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandlePlayerOptions(byte[] playerOptions, Guid correlationId)
        {
            await _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<PlayerOptions>(playerOptions)));

            await _hubConnection.SendAsync(EventNames.PlayerOptions, _hubConnection.ConnectionId, playerOptions);

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleTargetNumberUpdates(byte[] targetNumbers, Guid correlationId)
        {
            await _hubConnection.SendAsync(
                "DebugMessage",
                _hubConnection.ConnectionId,
                JsonConvert.SerializeObject(SevenZip.Compression.LZMA.DataHelper.Unpack<List<TargetNumberUpdate>>(targetNumbers)));

            await _hubConnection.SendAsync(EventNames.TargetNumbers, _hubConnection.ConnectionId, targetNumbers);

            return true;
        }
    }
}