using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Services
{
    public class ServerToClientCommunicator : RedisServerToClientCommunicator
    {
        private readonly ILogger<ServerToClientCommunicator> _logger;
        private readonly IGrainFactory _grainFactory;

        /// <inheritdoc />
        public ServerToClientCommunicator(ILogger<ServerToClientCommunicator> logger, string connectionString, IGrainFactory grainFactory) : base(logger, connectionString)
        {
            _logger = logger;
            _grainFactory = grainFactory;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleConnectRequest(ConnectRequest connectRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(connectRequest.Credentials.Name).Connect(connectRequest.Credentials.Password))
            {
                await LogWarning(connectRequest);
            }
          
            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleDisconnectRequest(DisconnectRequest disconnectRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(disconnectRequest.PlayerName).Disconnect(disconnectRequest.AuthenticationToken))
            {
                await LogWarning(disconnectRequest);
            }
            
            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetDamageReportsRequest(GetDamageReportsRequest getDamageReportsRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(getDamageReportsRequest.PlayerName).GetDamageReports(getDamageReportsRequest.AuthenticationToken))
            {
                await LogWarning(getDamageReportsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetGameOptionsRequest(GetGameOptionsRequest getGameOptionsRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(getGameOptionsRequest.PlayerName).GetGameOptions(getGameOptionsRequest.AuthenticationToken))
            {
                await LogWarning(getGameOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetGameStateRequest(GetGameStateRequest getGameStateRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(getGameStateRequest.PlayerName).GetGameState(getGameStateRequest.AuthenticationToken))
            {
                await LogWarning(getGameStateRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetPlayerOptionsRequest(GetPlayerOptionsRequest getPlayerOptionsRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(getPlayerOptionsRequest.PlayerName).GetPlayerOptions(getPlayerOptionsRequest.AuthenticationToken))
            {
                await LogWarning(getPlayerOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleForceReadyRequest(ForceReadyRequest forceReadyRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(forceReadyRequest.PlayerName).ForceReady(forceReadyRequest.AuthenticationToken))
            {
                await LogWarning(forceReadyRequest);
            }
            
            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleJoinGameRequest(JoinGameRequest joinGameRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(joinGameRequest.PlayerName).JoinGame(joinGameRequest.AuthenticationToken, joinGameRequest.Credentials.Name, joinGameRequest.Credentials.Password))
            {
                await LogWarning(joinGameRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleKickPlayerRequest(KickPlayerRequest kickPlayerRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(kickPlayerRequest.PlayerName).KickPlayer(kickPlayerRequest.AuthenticationToken, kickPlayerRequest.PlayerToKickName))
            {
                await LogWarning(kickPlayerRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleLeaveGameRequest(LeaveGameRequest leaveGameRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(leaveGameRequest.PlayerName).LeaveGame(leaveGameRequest.AuthenticationToken))
            {
                await LogWarning(leaveGameRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleMoveUnitRequest(MoveUnitRequest moveUnitRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(moveUnitRequest.PlayerName).MoveUnit(moveUnitRequest.AuthenticationToken, moveUnitRequest.UnitId, moveUnitRequest.ReceivingPlayer))
            {
                await LogWarning(moveUnitRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendDamageRequest(SendDamageInstanceRequest sendDamageInstanceRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(sendDamageInstanceRequest.PlayerName).SendDamageInstance(sendDamageInstanceRequest.AuthenticationToken, sendDamageInstanceRequest.DamageInstance))
            {
                await LogWarning(sendDamageInstanceRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendGameOptionsRequest(SendGameOptionsRequest sendGameOptionsRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(sendGameOptionsRequest.PlayerName).SendGameOptions(sendGameOptionsRequest.AuthenticationToken, sendGameOptionsRequest.GameOptions))
            {
                await LogWarning(sendGameOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendPlayerOptionsRequest(SendPlayerOptionsRequest sendPlayerOptionsRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(sendPlayerOptionsRequest.PlayerName).SendPlayerOptions(sendPlayerOptionsRequest.AuthenticationToken, sendPlayerOptionsRequest.PlayerOptions))
            {
                await LogWarning(sendPlayerOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendPlayerStateRequest(SendPlayerStateRequest sendPlayerStateRequest, Guid correlationId)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(sendPlayerStateRequest.PlayerName).SendPlayerState(sendPlayerStateRequest.AuthenticationToken, sendPlayerStateRequest.PlayerState))
            {
                await LogWarning(sendPlayerStateRequest);
            }

            return true;
        }

        private async Task LogWarning(RequestBase request)
        {
            _logger.LogWarning("Failed to handle a {type} for player {playerId}", request.GetType(), request.PlayerName);
            await Send(request.PlayerName, EventNames.ErrorMessage, $"Failed to handle a {request.GetType()} for player {request.PlayerName}.");
        }
    }
}