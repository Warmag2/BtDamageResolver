using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators
{
    /// <summary>
    /// Redis implementation of BtDamageResolver client-to-server communicator.
    /// </summary>
    public abstract class RedisClientToServerCommunicator : RedisCommunicator, IClientToServerCommunicator
    {
        /// <summary>
        /// Constructor for the Redis implementation of BtDamageResolver client-to-server communicator.
        /// </summary>
        protected RedisClientToServerCommunicator(ILogger logger, string connectionString, string playerId) : base(logger, connectionString, playerId)
        {
        }

        protected override async Task RunProcessorMethod(Envelope incomingEnvelope)
        {
            switch (incomingEnvelope.Type)
            {
                case EventNames.ConnectionResponse:
                    await HandleConnectionResponse(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case EventNames.DamageReports:
                    await HandleDamageReports(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case EventNames.ErrorMessage:
                    await HandleErrorMessage(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case EventNames.GameOptions:
                    await HandleGameOptions(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case EventNames.GameState:
                    await HandleGameState(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case EventNames.PlayerOptions:
                    await HandlePlayerOptions(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case EventNames.TargetNumbers:
                    await HandleTargetNumberUpdates(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
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
        public abstract Task<bool> HandleConnectionResponse(byte[] connectionResponse, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleDamageReports(byte[] damageReports, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleErrorMessage(byte[] errorMessage, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGameOptions(byte[] gameOptions, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGameState(byte[] gameState, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandlePlayerOptions(byte[] playerOptions, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleTargetNumberUpdates(byte[] targetNumbers, Guid correlationId);
    }
}