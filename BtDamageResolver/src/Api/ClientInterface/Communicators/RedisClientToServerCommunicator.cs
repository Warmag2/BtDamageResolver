using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;

/// <summary>
/// Redis implementation of BtDamageResolver client-to-server communicator.
/// </summary>
public abstract class RedisClientToServerCommunicator : RedisCommunicator, IClientToServerCommunicator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedisClientToServerCommunicator"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="playerId">The player ID to listen for events from.</param>
    protected RedisClientToServerCommunicator(ILogger logger, string connectionString, string playerId) : base(logger, connectionString, playerId)
    {
    }

    /// <inheritdoc />
    public void Send<TType>(string envelopeType, TType data)
    {
        SendEnvelope(ServerStreamAddress, new Envelope(envelopeType, data));
    }

    /// <inheritdoc />
    public abstract Task<bool> HandleConnectionResponse(byte[] connectionResponse, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleDamageReports(byte[] damageReports, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleErrorMessage(byte[] errorMessage, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleGameEntries(byte[] gameEntries, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleGameOptions(byte[] gameOptions, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleGameState(byte[] gameState, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandlePlayerOptions(byte[] playerOptions, Guid correlationId);

    /// <inheritdoc />
    public abstract Task<bool> HandleTargetNumberUpdates(byte[] targetNumbers, Guid correlationId);

    /// <inheritdoc />
    protected override async Task RunProcessorMethod(Envelope envelope)
    {
        switch (envelope.Type)
        {
            case EventNames.ConnectionResponse:
                await HandleConnectionResponse(envelope.Data, envelope.CorrelationId);
                break;
            case EventNames.DamageReports:
                await HandleDamageReports(envelope.Data, envelope.CorrelationId);
                break;
            case EventNames.ErrorMessage:
                await HandleErrorMessage(envelope.Data, envelope.CorrelationId);
                break;
            case EventNames.GameEntries:
                await HandleGameEntries(envelope.Data, envelope.CorrelationId);
                break;
            case EventNames.GameOptions:
                await HandleGameOptions(envelope.Data, envelope.CorrelationId);
                break;
            case EventNames.GameState:
                await HandleGameState(envelope.Data, envelope.CorrelationId);
                break;
            case EventNames.PlayerOptions:
                await HandlePlayerOptions(envelope.Data, envelope.CorrelationId);
                break;
            case EventNames.TargetNumbers:
                await HandleTargetNumberUpdates(envelope.Data, envelope.CorrelationId);
                break;
            default:
                Logger.LogWarning("A client has sent data with unknown handling type {handlingType}.", envelope.Type);
                break;
        }
    }

    /// <inheritdoc />
    protected override void SubscribeAdditional()
    {
        var listenedClientQueue = RedisSubscriber.Subscribe(ClientStreamAddress);
        listenedClientQueue.OnMessage(async channelMessage => await RunProcessorMethod(JsonConvert.DeserializeObject<Envelope>(channelMessage.Message)).ConfigureAwait(false));

        base.SubscribeAdditional();
    }
}