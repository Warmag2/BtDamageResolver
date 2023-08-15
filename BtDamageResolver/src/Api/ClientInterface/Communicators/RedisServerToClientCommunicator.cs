using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;

/// <summary>
/// Redis implementation of BtDamageResolver server-to-client communicator.
/// </summary>
public abstract class RedisServerToClientCommunicator : RedisCommunicator, IServerToClientCommunicator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedisServerToClientCommunicator"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="connectionString">The connection string for Redis.</param>
    protected RedisServerToClientCommunicator(ILogger logger, string connectionString) : base(logger, connectionString, ServerStreamAddress)
    {
    }

    /// <inheritdoc />
    public void Send<TType>(string clientName, string envelopeType, TType data)
    {
        SendEnvelope(clientName, new Envelope(envelopeType, data));
    }

    /// <inheritdoc />
    public void SendToAll<TType>(string envelopeType, TType data)
    {
        SendEnvelope(ClientStreamAddress, new Envelope(envelopeType, data));
    }

    /// <inheritdoc />
    public void SendToMany<TType>(List<string> clientNames, string envelopeType, TType data)
    {
        var envelope = new Envelope(envelopeType, data);
        foreach (var clientName in clientNames)
        {
            SendEnvelope(clientName, envelope);
        }
    }

    /// <inheritdoc />
    public abstract Task<bool> HandleConnectRequest(byte[] connectRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleDisconnectRequest(byte[] disconnectRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleGetDamageReportsRequest(byte[] getDamageReportsRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleGetGameOptionsRequest(byte[] getGameOptionsRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleGetGameStateRequest(byte[] getGameStateRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleGetPlayerOptionsRequest(byte[] getPlayerOptionsRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleForceReadyRequest(byte[] forceReadyRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleJoinGameRequest(byte[] joinGameRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleKickPlayerRequest(byte[] kickPlayerRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleLeaveGameRequest(byte[] leaveGameRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleMoveUnitRequest(byte[] moveUnitRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleSendDamageRequest(byte[] sendDamageInstanceRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleSendGameOptionsRequest(byte[] sendGameOptionsRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleSendPlayerOptionsRequest(byte[] sendPlayerOptionsRequest, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleSendPlayerStateRequest(byte[] sendPlayerStateRequest, Guid correlationId);

    /// <inheritdoc />
    protected override async Task RunProcessorMethod(Envelope envelope)
    {
        switch (envelope.Type)
        {
            case RequestNames.Connect:
                await HandleConnectRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.Disconnect:
                await HandleDisconnectRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.GetDamageReports:
                await HandleGetDamageReportsRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.GetGameOptions:
                await HandleGetGameOptionsRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.GetGameState:
                await HandleGetGameStateRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.GetPlayerOptions:
                await HandleGetGameOptionsRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.ForceReady:
                await HandleForceReadyRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.JoinGame:
                await HandleJoinGameRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.KickPlayer:
                await HandleKickPlayerRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.LeaveGame:
                await HandleLeaveGameRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.MoveUnit:
                await HandleMoveUnitRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.SendDamageInstanceRequest:
                await HandleSendDamageRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.SendGameOptions:
                await HandleSendGameOptionsRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.SendPlayerOptions:
                await HandleSendPlayerOptionsRequest(envelope.Data, envelope.CorrelationId);
                break;
            case RequestNames.SendPlayerState:
                await HandleSendPlayerStateRequest(envelope.Data, envelope.CorrelationId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(envelope), $"No handler defined for request type {envelope.Type}");
        }
    }
}