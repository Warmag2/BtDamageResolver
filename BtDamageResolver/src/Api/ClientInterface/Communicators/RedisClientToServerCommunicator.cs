using System;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

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
    /// <param name="jsonSerializerOptions">JSON serializer options.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="dataHelper">The data compression helper.</param>
    /// <param name="playerId">The player ID to listen for events from.</param>
    protected RedisClientToServerCommunicator(ILogger logger, IOptions<JsonSerializerOptions> jsonSerializerOptions, string connectionString, DataHelper dataHelper, string playerId) : base(logger, jsonSerializerOptions, connectionString, dataHelper, playerId)
    {
    }

    /// <inheritdoc />
    public void Send<TType>(string envelopeType, TType data)
        where TType : class
    {
        SendSingle(ServerStreamAddress, new Envelope(envelopeType, DataHelper.Pack(data)));
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
                Logger.LogWarning("A client has sent data with unknown handling type {HandlingType}.", envelope.Type);
                break;
        }
    }

    /// <inheritdoc />
    protected override void SubscribeAdditional()
    {
        var listenedClientQueue = RedisSubscriber.Subscribe(new RedisChannel(ClientStreamAddress, RedisChannel.PatternMode.Literal));
        listenedClientQueue.OnMessage(async channelMessage => await RunProcessorMethod(JsonSerializer.Deserialize<Envelope>(channelMessage.Message, JsonSerializerOptions)).ConfigureAwait(false));

        base.SubscribeAdditional();
    }
}