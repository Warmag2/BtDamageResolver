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
    /// <summary>
    /// A Redis-based server-to-client communicator.
    /// </summary>
    public class ServerToClientCommunicator : RedisServerToClientCommunicator
    {
        private readonly ILogger<ServerToClientCommunicator> _logger;
        private readonly IGrainFactory _grainFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerToClientCommunicator"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="connectionString">The Redis connection string.</param>
        /// <param name="grainFactory">The grain factory.</param>
        public ServerToClientCommunicator(ILogger<ServerToClientCommunicator> logger, string connectionString, IGrainFactory grainFactory) : base(logger, connectionString)
        {
            _logger = logger;
            _grainFactory = grainFactory;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleConnectRequest(byte[] connectRequest, Guid correlationId)
        {
            var unpackedConnectRequest = Unpack<ConnectRequest>(connectRequest);
            var (connectResult, connectValidationResult) = ValidateObject(unpackedConnectRequest);

            if (!connectResult)
            {
                SendErrorMessage(unpackedConnectRequest.PlayerName, $"Errors: {string.Join(", ", connectValidationResult.Where(v => v.ErrorMessage != null).Select(v => v.ErrorMessage))}");

                return false;
            }

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedConnectRequest.Credentials.Name).Connect(unpackedConnectRequest.Credentials.Password))
            {
                LogWarning(unpackedConnectRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleDisconnectRequest(byte[] disconnectRequest, Guid correlationId)
        {
            var unpackedDisconnectRequest = Unpack<DisconnectRequest>(disconnectRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedDisconnectRequest.PlayerName).Disconnect(unpackedDisconnectRequest.AuthenticationToken))
            {
                LogWarning(unpackedDisconnectRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetDamageReportsRequest(byte[] getDamageReportsRequest, Guid correlationId)
        {
            var unpackedGetDamageReportsRequest = Unpack<GetDamageReportsRequest>(getDamageReportsRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetDamageReportsRequest.PlayerName).RequestDamageReports(unpackedGetDamageReportsRequest.AuthenticationToken))
            {
                LogWarning(unpackedGetDamageReportsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetGameOptionsRequest(byte[] getGameOptionsRequest, Guid correlationId)
        {
            var unpackedGetGameOptionsRequest = Unpack<GetGameOptionsRequest>(getGameOptionsRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetGameOptionsRequest.PlayerName).RequestGameOptions(unpackedGetGameOptionsRequest.AuthenticationToken))
            {
                LogWarning(unpackedGetGameOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetGameStateRequest(byte[] getGameStateRequest, Guid correlationId)
        {
            var unpackedGetGameStateRequest = Unpack<GetGameStateRequest>(getGameStateRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetGameStateRequest.PlayerName).RequestGameState(unpackedGetGameStateRequest.AuthenticationToken))
            {
                LogWarning(unpackedGetGameStateRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleGetPlayerOptionsRequest(byte[] getPlayerOptionsRequest, Guid correlationId)
        {
            var unpackedGetPlayerOptionsRequest = Unpack<GetPlayerOptionsRequest>(getPlayerOptionsRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetPlayerOptionsRequest.PlayerName).RequestPlayerOptions(unpackedGetPlayerOptionsRequest.AuthenticationToken))
            {
                LogWarning(unpackedGetPlayerOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleForceReadyRequest(byte[] forceReadyRequest, Guid correlationId)
        {
            var unpackedForceReadyRequest = Unpack<ForceReadyRequest>(forceReadyRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedForceReadyRequest.PlayerName).ForceReady(unpackedForceReadyRequest.AuthenticationToken))
            {
                LogWarning(unpackedForceReadyRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleJoinGameRequest(byte[] joinGameRequest, Guid correlationId)
        {
            var unpackedJoinGameRequest = Unpack<JoinGameRequest>(joinGameRequest);
            var (joinGameRequestResult, joinGameRequestValidationResult) = ValidateObject(unpackedJoinGameRequest);

            if (!joinGameRequestResult)
            {
                SendErrorMessage(unpackedJoinGameRequest.PlayerName, $"Errors: {string.Join(", ", joinGameRequestValidationResult.Where(v => v.ErrorMessage != null).Select(v => v.ErrorMessage))}");

                return false;
            }

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedJoinGameRequest.PlayerName).JoinGame(unpackedJoinGameRequest.AuthenticationToken, unpackedJoinGameRequest.Credentials.Name, unpackedJoinGameRequest.Credentials.Password))
            {
                LogWarning(unpackedJoinGameRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleKickPlayerRequest(byte[] kickPlayerRequest, Guid correlationId)
        {
            var unpackedKickPlayerRequest = Unpack<KickPlayerRequest>(kickPlayerRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedKickPlayerRequest.PlayerName).KickPlayer(unpackedKickPlayerRequest.AuthenticationToken, unpackedKickPlayerRequest.PlayerToKickName))
            {
                LogWarning(unpackedKickPlayerRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleLeaveGameRequest(byte[] leaveGameRequest, Guid correlationId)
        {
            var unpackedLeaveGameRequest = Unpack<LeaveGameRequest>(leaveGameRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedLeaveGameRequest.PlayerName).LeaveGame(unpackedLeaveGameRequest.AuthenticationToken))
            {
                LogWarning(unpackedLeaveGameRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleMoveUnitRequest(byte[] moveUnitRequest, Guid correlationId)
        {
            var unpackedMoveUnitRequest = Unpack<MoveUnitRequest>(moveUnitRequest);

            var gameName = await _grainFactory.GetGrain<IPlayerActor>(unpackedMoveUnitRequest.PlayerName).GetGameId(unpackedMoveUnitRequest.AuthenticationToken);

            if (!await _grainFactory.GetGrain<IGameActor>(gameName).MoveUnit(unpackedMoveUnitRequest.PlayerName, unpackedMoveUnitRequest.UnitId, unpackedMoveUnitRequest.ReceivingPlayer))
            {
                LogWarning(unpackedMoveUnitRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendDamageRequest(byte[] sendDamageInstanceRequest, Guid correlationId)
        {
            var unpackedSendDamageInstanceRequest = Unpack<SendDamageInstanceRequest>(sendDamageInstanceRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendDamageInstanceRequest.PlayerName).SendDamageInstance(unpackedSendDamageInstanceRequest.AuthenticationToken, unpackedSendDamageInstanceRequest.DamageInstance))
            {
                LogWarning(unpackedSendDamageInstanceRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendGameOptionsRequest(byte[] sendGameOptionsRequest, Guid correlationId)
        {
            var unpackedSendGameOptionsRequest = Unpack<SendGameOptionsRequest>(sendGameOptionsRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendGameOptionsRequest.PlayerName).SendGameOptions(unpackedSendGameOptionsRequest.AuthenticationToken, unpackedSendGameOptionsRequest.GameOptions))
            {
                LogWarning(unpackedSendGameOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendPlayerOptionsRequest(byte[] sendPlayerOptionsRequest, Guid correlationId)
        {
            var unpackedSendPlayerOptionsRequest = Unpack<SendPlayerOptionsRequest>(sendPlayerOptionsRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendPlayerOptionsRequest.PlayerName).SendPlayerOptions(unpackedSendPlayerOptionsRequest.AuthenticationToken, unpackedSendPlayerOptionsRequest.PlayerOptions))
            {
                LogWarning(unpackedSendPlayerOptionsRequest);
            }

            return true;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleSendPlayerStateRequest(byte[] sendPlayerStateRequest, Guid correlationId)
        {
            var unpackedSendPlayerStateRequest = Unpack<SendPlayerStateRequest>(sendPlayerStateRequest);

            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendPlayerStateRequest.PlayerName).SendPlayerState(unpackedSendPlayerStateRequest.AuthenticationToken, unpackedSendPlayerStateRequest.PlayerState))
            {
                LogWarning(unpackedSendPlayerStateRequest);
            }

            return true;
        }

        private static (bool Success, List<ValidationResult> ValidationResults) ValidateObject(object validatedObject)
        {
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(validatedObject, new ValidationContext(validatedObject), validationResults, true);

            return (result, validationResults);
        }

        private void LogWarning(RequestBase request)
        {
            _logger.LogWarning("Failed to handle a {type} for player {playerId}", request.GetType(), request.PlayerName);
            Send(request.PlayerName, EventNames.ErrorMessage, $"Failed to handle a {request.GetType()} for player {request.PlayerName}.");
        }
    }
}