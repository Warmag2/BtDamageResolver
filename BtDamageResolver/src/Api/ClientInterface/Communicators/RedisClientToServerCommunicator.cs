using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using static SevenZip.Compression.LZMA.DataHelper;
using static System.Text.Json.JsonSerializer;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators
{
    /// <summary>
    /// Redis implementation of BtDamageResolver client-to-server communicator.
    /// </summary>
    public abstract class RedisClientToServerCommunicator : RedisCommunicator, IClientToServerCommunicator
    {
        private readonly string _playerId;
        private ChannelMessageQueue _clientMessageQueue;

        /// <summary>
        /// Constructor for the Redis implementation of BtDamageResolver client-to-server communicator.
        /// </summary>
        protected RedisClientToServerCommunicator(ILogger logger, string connectionString, string playerId) : base(logger, connectionString)
        {
            _playerId = playerId;
        }

        protected override void Subscribe()
        {
            _clientMessageQueue = RedisSubscriber.Subscribe(_playerId);
            _clientMessageQueue.OnMessage(async channelMessage => await RunProcessorMethod(Deserialize<Envelope>(channelMessage.Message)));
        }

        private async Task RunProcessorMethod(Envelope incomingEnvelope)
        {
            switch (incomingEnvelope.Type)
            {
                case EventNames.ConnectionResponse:
                    await HandleConnectionResponse(Unpack<ConnectionResponse>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case EventNames.DamageReports:
                    await HandleDamageReports(Unpack<List<DamageReport>>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case EventNames.ErrorMessage:
                    await HandleErrorMessage(Unpack<string>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case EventNames.GameOptions:
                    await HandleGameOptions(Unpack<GameOptions>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case EventNames.GameState:
                    await HandleGameState(Unpack<GameState>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case EventNames.PlayerOptions:
                    await HandlePlayerOptions(Unpack<PlayerOptions>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case EventNames.TargetNumbers:
                    await HandleTargetNumberUpdates(Unpack<List<TargetNumberUpdate>>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                default:
                    Logger.LogWarning("A client has sent data with unknown handling type {handlingType}.", incomingEnvelope.Type);
                    break;
            }
        }

        /// <inheritdoc />
        public async Task Send<TType>(string envelopeType, TType data)
        {
            await SendEnvelope(ServerStreamAddress, new Envelope(envelopeType, data));
        }

        /// <inheritdoc />
        public abstract Task<bool> HandleConnectionResponse(ConnectionResponse connectionResponse, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleDamageReports(List<DamageReport> damageReports, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleErrorMessage(string errorMessage, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGameOptions(GameOptions gameOptions, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGameState(GameState gameState, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandlePlayerOptions(PlayerOptions playerOptions, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleTargetNumberUpdates(List<TargetNumberUpdate> targetNumbers, Guid correlationId);
    }
}