using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators
{
    /// <summary>
    /// Redis implementation of BtDamageResolver server-to-client communicator.
    /// </summary>
    public abstract class RedisServerToClientCommunicator : RedisCommunicator, IServerToClientCommunicator
    {
        /// <summary>
        /// Constructor for the Redis implementation of BtDamageResolver server-to-client communicator.
        /// </summary>
        protected RedisServerToClientCommunicator(ILogger logger, string connectionString) : base(logger, connectionString, ServerStreamAddress)
        {
        }

        protected override async Task RunProcessorMethod(Envelope incomingEnvelope)
        {
            switch (incomingEnvelope.Type)
            {
                case RequestNames.Connect:
                    await HandleConnectRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.Disconnect:
                    await HandleDisconnectRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetDamageReports:
                    await HandleGetDamageReportsRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetGameOptions:
                    await HandleGetGameOptionsRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetGameState:
                    await HandleGetGameStateRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetPlayerOptions:
                    await HandleGetGameOptionsRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.ForceReady:
                    await HandleForceReadyRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.JoinGame:
                    await HandleJoinGameRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.KickPlayer:
                    await HandleKickPlayerRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.LeaveGame:
                    await HandleLeaveGameRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.MoveUnit:
                    await HandleMoveUnitRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendDamageInstanceRequest:
                    await HandleSendDamageRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendGameOptions:
                    await HandleSendGameOptionsRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendPlayerOptions:
                    await HandleSendPlayerOptionsRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendPlayerState:
                    await HandleSendPlayerStateRequest(incomingEnvelope.Data, incomingEnvelope.CorrelationId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(incomingEnvelope.Type), $"No handler defined for request type {incomingEnvelope.Type}");
            }
        }

        /// <inheritdoc />
        public void Send<TType>(string clientName, string envelopeType, TType data)
        {
            SendEnvelope(clientName, new Envelope(envelopeType, data));
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
    }
}