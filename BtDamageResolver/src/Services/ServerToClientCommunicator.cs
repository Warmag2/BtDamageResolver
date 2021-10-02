using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Microsoft.Extensions.Logging;
using Orleans;

using static SevenZip.Compression.LZMA.DataHelper;

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
        public override async Task<bool> HandleConnectRequest(byte[] connectRequestData, Guid correlationId)
        {
            var connectRequest = Unpack<ConnectRequest>(connectRequestData);
            var (connectResult, connectValidationResult) = ValidateObject(connectRequest);

            if (!connectResult)
            {
                SendErrorMessage(connectRequest.PlayerName, $"Errors: {string.Join(", ", connectValidationResult.Where(v => v.ErrorMessage != null).Select(v => v.ErrorMessage))}");

                return false;
            }

            if (!await _grainFactory.GetGrain<IPlayerActor>(connectRequest.Credentials.Name).Connect(connectRequest.Credentials.Password))
            {
                LogWarning(connectRequest);
            }
          
            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleDisconnectRequest(byte[] disconnectRequestData, Guid correlationId)
        {
            var disconnectRequest = Unpack<DisconnectRequest>(disconnectRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(disconnectRequest.PlayerName).Disconnect(disconnectRequest.AuthenticationToken))
            {
                LogWarning(disconnectRequest);
            }
            
            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetDamageReportsRequest(byte[] getDamageReportsRequestData, Guid correlationId)
        {
            var getDamageReportsRequest = Unpack<GetDamageReportsRequest>(getDamageReportsRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(getDamageReportsRequest.PlayerName).RequestDamageReports(getDamageReportsRequest.AuthenticationToken))
            {
                LogWarning(getDamageReportsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetGameOptionsRequest(byte[] getGameOptionsRequestData, Guid correlationId)
        {
            var getGameOptionsRequest = Unpack<GetGameOptionsRequest>(getGameOptionsRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(getGameOptionsRequest.PlayerName).RequestGameOptions(getGameOptionsRequest.AuthenticationToken))
            {
                LogWarning(getGameOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetGameStateRequest(byte[] getGameStateRequestData, Guid correlationId)
        {
            var getGameStateRequest = Unpack<GetGameStateRequest>(getGameStateRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(getGameStateRequest.PlayerName).RequestGameState(getGameStateRequest.AuthenticationToken))
            {
                LogWarning(getGameStateRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetPlayerOptionsRequest(byte[] getPlayerOptionsRequestData, Guid correlationId)
        {
            var getPlayerOptionsRequest = Unpack<GetPlayerOptionsRequest>(getPlayerOptionsRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(getPlayerOptionsRequest.PlayerName).RequestPlayerOptions(getPlayerOptionsRequest.AuthenticationToken))
            {
                LogWarning(getPlayerOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleForceReadyRequest(byte[] forceReadyRequestData, Guid correlationId)
        {
            var forceReadyRequest = Unpack<ForceReadyRequest>(forceReadyRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(forceReadyRequest.PlayerName).ForceReady(forceReadyRequest.AuthenticationToken))
            {
                LogWarning(forceReadyRequest);
            }
            
            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleJoinGameRequest(byte[] joinGameRequestData, Guid correlationId)
        {
            var joinGameRequest = Unpack<JoinGameRequest>(joinGameRequestData);
            var (joinGameRequestResult, joinGameRequestValidationResult) = ValidateObject(joinGameRequest);

            if (!joinGameRequestResult)
            {
                SendErrorMessage(joinGameRequest.PlayerName, $"Errors: {string.Join(", ", joinGameRequestValidationResult.Where(v => v.ErrorMessage != null).Select(v => v.ErrorMessage))}");

                return false;
            }

            if (!await _grainFactory.GetGrain<IPlayerActor>(joinGameRequest.PlayerName).JoinGame(joinGameRequest.AuthenticationToken, joinGameRequest.Credentials.Name, joinGameRequest.Credentials.Password))
            {
                LogWarning(joinGameRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleKickPlayerRequest(byte[] kickPlayerRequestData, Guid correlationId)
        {
            var kickPlayerRequest = Unpack<KickPlayerRequest>(kickPlayerRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(kickPlayerRequest.PlayerName).KickPlayer(kickPlayerRequest.AuthenticationToken, kickPlayerRequest.PlayerToKickName))
            {
                LogWarning(kickPlayerRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleLeaveGameRequest(byte[] leaveGameRequestData, Guid correlationId)
        {
            var leaveGameRequest = Unpack<LeaveGameRequest>(leaveGameRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(leaveGameRequest.PlayerName).LeaveGame(leaveGameRequest.AuthenticationToken))
            {
                LogWarning(leaveGameRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleMoveUnitRequest(byte[] moveUnitRequestData, Guid correlationId)
        {
            var moveUnitRequest = Unpack<MoveUnitRequest>(moveUnitRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(moveUnitRequest.PlayerName).MoveUnit(moveUnitRequest.AuthenticationToken, moveUnitRequest.UnitId, moveUnitRequest.ReceivingPlayer))
            {
                LogWarning(moveUnitRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendDamageRequest(byte[] sendDamageInstanceRequestData, Guid correlationId)
        {
            var sendDamageInstanceRequest = Unpack<SendDamageInstanceRequest>(sendDamageInstanceRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(sendDamageInstanceRequest.PlayerName).SendDamageInstance(sendDamageInstanceRequest.AuthenticationToken, sendDamageInstanceRequest.DamageInstance))
            {
                LogWarning(sendDamageInstanceRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendGameOptionsRequest(byte[] sendGameOptionsRequestData, Guid correlationId)
        {
            var sendGameOptionsRequest = Unpack<SendGameOptionsRequest>(sendGameOptionsRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(sendGameOptionsRequest.PlayerName).SendGameOptions(sendGameOptionsRequest.AuthenticationToken, sendGameOptionsRequest.GameOptions))
            {
                LogWarning(sendGameOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendPlayerOptionsRequest(byte[] sendPlayerOptionsRequestData, Guid correlationId)
        {
            var sendPlayerOptionsRequest = Unpack<SendPlayerOptionsRequest>(sendPlayerOptionsRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(sendPlayerOptionsRequest.PlayerName).SendPlayerOptions(sendPlayerOptionsRequest.AuthenticationToken, sendPlayerOptionsRequest.PlayerOptions))
            {
                LogWarning(sendPlayerOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendPlayerStateRequest(byte[] sendPlayerStateRequestData, Guid correlationId)
        {
            var sendPlayerStateRequest = Unpack<SendPlayerStateRequest>(sendPlayerStateRequestData);

            if (!await _grainFactory.GetGrain<IPlayerActor>(sendPlayerStateRequest.PlayerName).SendPlayerState(sendPlayerStateRequest.AuthenticationToken, sendPlayerStateRequest.PlayerState))
            {
                LogWarning(sendPlayerStateRequest);
            }

            return true;
        }

        private void LogWarning(RequestBase request)
        {
            _logger.LogWarning("Failed to handle a {type} for player {playerId}", request.GetType(), request.PlayerName);
            Send(request.PlayerName, EventNames.ErrorMessage, $"Failed to handle a {request.GetType()} for player {request.PlayerName}.");
        }

        private static (bool, List<ValidationResult>) ValidateObject(object validatedObject)
        {
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(validatedObject, new ValidationContext(validatedObject), validationResults, true);

            return (result, validationResults);
        }
    }
}