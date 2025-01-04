using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.Services;

/// <summary>
/// A Redis-based server-to-client communicator.
/// </summary>
public class ServerToClientCommunicator : RedisServerToClientCommunicator
{
    private readonly ILogger<ServerToClientCommunicator> _logger;
    private readonly DataHelper _dataHelper;
    private readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerToClientCommunicator"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="jsonSerializerOptions">JSON serializer options.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="dataHelper">The data compression helper.</param>
    /// <param name="grainFactory">The grain factory.</param>
    public ServerToClientCommunicator(ILogger<ServerToClientCommunicator> logger, IOptions<JsonSerializerOptions> jsonSerializerOptions, string connectionString, DataHelper dataHelper, IGrainFactory grainFactory) : base(logger, jsonSerializerOptions, connectionString, dataHelper)
    {
        _logger = logger;
        _dataHelper = dataHelper;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleConnectRequest(byte[] connectRequest, Guid correlationId)
    {
        var unpackedConnectRequest = _dataHelper.Unpack<ConnectRequest>(connectRequest);
        var (connectResult, connectValidationResult) = ValidateObject(unpackedConnectRequest);

        if (!connectResult)
        {
            SendErrorMessage(unpackedConnectRequest.PlayerName, $"Errors: {string.Join(", ", connectValidationResult.Where(v => v.ErrorMessage != null).Select(v => v.ErrorMessage))}");

            return false;
        }

        if (unpackedConnectRequest.Credentials.AuthenticationToken.HasValue)
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedConnectRequest.Credentials.Name).Connect(unpackedConnectRequest.Credentials.AuthenticationToken.Value))
            {
                LogWarning(unpackedConnectRequest);
            }
        }
        else
        {
            if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedConnectRequest.Credentials.Name).Connect(unpackedConnectRequest.Credentials.Password))
            {
                LogWarning(unpackedConnectRequest);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleDisconnectRequest(byte[] disconnectRequest, Guid correlationId)
    {
        var unpackedDisconnectRequest = _dataHelper.Unpack<DisconnectRequest>(disconnectRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedDisconnectRequest.PlayerName).Disconnect(unpackedDisconnectRequest.AuthenticationToken))
        {
            LogWarning(unpackedDisconnectRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGetDamageReportsRequest(byte[] getDamageReportsRequest, Guid correlationId)
    {
        var unpackedGetDamageReportsRequest = _dataHelper.Unpack<GetDamageReportsRequest>(getDamageReportsRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetDamageReportsRequest.PlayerName).RequestDamageReports(unpackedGetDamageReportsRequest.AuthenticationToken))
        {
            LogWarning(unpackedGetDamageReportsRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGetGameOptionsRequest(byte[] getGameOptionsRequest, Guid correlationId)
    {
        var unpackedGetGameOptionsRequest = _dataHelper.Unpack<GetGameOptionsRequest>(getGameOptionsRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetGameOptionsRequest.PlayerName).RequestGameOptions(unpackedGetGameOptionsRequest.AuthenticationToken))
        {
            LogWarning(unpackedGetGameOptionsRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGetGameStateRequest(byte[] getGameStateRequest, Guid correlationId)
    {
        var unpackedGetGameStateRequest = _dataHelper.Unpack<GetGameStateRequest>(getGameStateRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetGameStateRequest.PlayerName).RequestGameState(unpackedGetGameStateRequest.AuthenticationToken))
        {
            LogWarning(unpackedGetGameStateRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGetPlayerOptionsRequest(byte[] getPlayerOptionsRequest, Guid correlationId)
    {
        var unpackedGetPlayerOptionsRequest = _dataHelper.Unpack<GetPlayerOptionsRequest>(getPlayerOptionsRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedGetPlayerOptionsRequest.PlayerName).RequestPlayerOptions(unpackedGetPlayerOptionsRequest.AuthenticationToken))
        {
            LogWarning(unpackedGetPlayerOptionsRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleForceReadyRequest(byte[] forceReadyRequest, Guid correlationId)
    {
        var unpackedForceReadyRequest = _dataHelper.Unpack<ForceReadyRequest>(forceReadyRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedForceReadyRequest.PlayerName).ForceReady(unpackedForceReadyRequest.AuthenticationToken))
        {
            LogWarning(unpackedForceReadyRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleJoinGameRequest(byte[] joinGameRequest, Guid correlationId)
    {
        var unpackedJoinGameRequest = _dataHelper.Unpack<JoinGameRequest>(joinGameRequest);
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
        var unpackedKickPlayerRequest = _dataHelper.Unpack<KickPlayerRequest>(kickPlayerRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedKickPlayerRequest.PlayerName).KickPlayer(unpackedKickPlayerRequest.AuthenticationToken, unpackedKickPlayerRequest.PlayerToKickName))
        {
            LogWarning(unpackedKickPlayerRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleLeaveGameRequest(byte[] leaveGameRequest, Guid correlationId)
    {
        var unpackedLeaveGameRequest = _dataHelper.Unpack<LeaveGameRequest>(leaveGameRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedLeaveGameRequest.PlayerName).LeaveGame(unpackedLeaveGameRequest.AuthenticationToken))
        {
            LogWarning(unpackedLeaveGameRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleMoveUnitRequest(byte[] moveUnitRequest, Guid correlationId)
    {
        var unpackedMoveUnitRequest = _dataHelper.Unpack<MoveUnitRequest>(moveUnitRequest);

        var gameName = await _grainFactory.GetGrain<IPlayerActor>(unpackedMoveUnitRequest.PlayerName).GetGameId(unpackedMoveUnitRequest.AuthenticationToken);

        // Early bail if the player is not in a game or the authentication failed
        if (string.IsNullOrWhiteSpace(gameName))
        {
            return false;
        }

        if (!await _grainFactory.GetGrain<IGameActor>(gameName).MoveUnit(unpackedMoveUnitRequest.PlayerName, unpackedMoveUnitRequest.UnitId, unpackedMoveUnitRequest.ReceivingPlayer))
        {
            LogWarning(unpackedMoveUnitRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleSendDamageRequest(byte[] sendDamageInstanceRequest, Guid correlationId)
    {
        var unpackedSendDamageInstanceRequest = _dataHelper.Unpack<SendDamageInstanceRequest>(sendDamageInstanceRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendDamageInstanceRequest.PlayerName).SendDamageInstance(unpackedSendDamageInstanceRequest.AuthenticationToken, unpackedSendDamageInstanceRequest.DamageInstance))
        {
            LogWarning(unpackedSendDamageInstanceRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleSendGameOptionsRequest(byte[] sendGameOptionsRequest, Guid correlationId)
    {
        var unpackedSendGameOptionsRequest = _dataHelper.Unpack<SendGameOptionsRequest>(sendGameOptionsRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendGameOptionsRequest.PlayerName).SendGameOptions(unpackedSendGameOptionsRequest.AuthenticationToken, unpackedSendGameOptionsRequest.GameOptions))
        {
            LogWarning(unpackedSendGameOptionsRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleSendPlayerOptionsRequest(byte[] sendPlayerOptionsRequest, Guid correlationId)
    {
        var unpackedSendPlayerOptionsRequest = _dataHelper.Unpack<SendPlayerOptionsRequest>(sendPlayerOptionsRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendPlayerOptionsRequest.PlayerName).SendPlayerOptions(unpackedSendPlayerOptionsRequest.AuthenticationToken, unpackedSendPlayerOptionsRequest.PlayerOptions))
        {
            LogWarning(unpackedSendPlayerOptionsRequest);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleSendPlayerStateRequest(byte[] sendPlayerStateRequest, Guid correlationId)
    {
        var unpackedSendPlayerStateRequest = _dataHelper.Unpack<SendPlayerStateRequest>(sendPlayerStateRequest);

        if (!await _grainFactory.GetGrain<IPlayerActor>(unpackedSendPlayerStateRequest.PlayerName).SendPlayerState(unpackedSendPlayerStateRequest.AuthenticationToken, unpackedSendPlayerStateRequest.PlayerState))
        {
            // Do not use regular LogWarning here - SendPlayerState has internal error handling and sends its own sensible error messages to the player
            _logger.LogWarning("Failed to handle a message of type {Type} for player {PlayerId}", unpackedSendPlayerStateRequest.GetType(), unpackedSendPlayerStateRequest.PlayerName);
        }
        else
        {
            // In this case, clear error message on success, so that the player knows he is in spec.
            // This is the only error message the player should ever see if the game is working properly, so I won't bother to worry about this discrepancy too much.
            SendErrorMessage(unpackedSendPlayerStateRequest.PlayerName, string.Empty);
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
        _logger.LogWarning("Failed to handle a message of type {Type} for player {PlayerId}", request.GetType(), request.PlayerName);
        SendErrorMessage(request.PlayerName, $"Failed to handle a {request.GetType()} for player {request.PlayerName}.");
    }
}