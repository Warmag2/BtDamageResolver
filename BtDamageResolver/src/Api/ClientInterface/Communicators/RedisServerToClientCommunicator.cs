using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using static SevenZip.Compression.LZMA.DataHelper;
using static System.Text.Json.JsonSerializer;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators
{
    /// <summary>
    /// Redis implementation of BtDamageResolver server-to-client communicator.
    /// </summary>
    public abstract class RedisServerToClientCommunicator : RedisCommunicator, IServerToClientCommunicator
    {
        private ChannelMessageQueue _serverMessageQueue;

        /// <summary>
        /// Constructor for the Redis implementation of BtDamageResolver server-to-client communicator.
        /// </summary>
        protected RedisServerToClientCommunicator(ILogger logger, string connectionString) : base(logger, connectionString)
        {
        }

        protected override void Subscribe()
        {
            _serverMessageQueue = RedisSubscriber.Subscribe(ServerStreamAddress);
            _serverMessageQueue.OnMessage(async channelMessage => await RunProcessorMethod(Deserialize<Envelope>(channelMessage.Message)));
        }

        private async Task RunProcessorMethod(Envelope incomingEnvelope)
        {
            switch (incomingEnvelope.Type)
            {
                case RequestNames.Connect:
                    var connectRequest = Unpack<ConnectRequest>(incomingEnvelope.Data);
                    var (connectResult, connectValidationResult) = ValidateObject(connectRequest);

                    if (!connectResult)
                    {
                        await SendErrorMessage(connectRequest.PlayerName, $"Errors: {string.Join(", ", connectValidationResult.Where(v => v.ErrorMessage != null).Select(v => v.ErrorMessage))}");
                    }
                    else
                    {
                        await HandleConnectRequest(connectRequest, incomingEnvelope.CorrelationId);
                    }

                    break;
                case RequestNames.Disconnect:
                    await HandleDisconnectRequest(Unpack<DisconnectRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetDamageReports:
                    await HandleGetDamageReportsRequest(Unpack<GetDamageReportsRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetGameOptions:
                    await HandleGetGameOptionsRequest(Unpack<GetGameOptionsRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetGameState:
                    await HandleGetGameStateRequest(Unpack<GetGameStateRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.GetPlayerOptions:
                    await HandleGetGameOptionsRequest(Unpack<GetGameOptionsRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.ForceReady:
                    await HandleForceReadyRequest(Unpack<ForceReadyRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.JoinGame:
                    var joinGameRequest = Unpack<JoinGameRequest>(incomingEnvelope.Data);
                    var (joinGameRequestResult, joinGameRequestValidationResult) = ValidateObject(joinGameRequest);

                    if (!joinGameRequestResult)
                    {
                        await SendErrorMessage(joinGameRequest.PlayerName, $"Errors: {string.Join(", ", joinGameRequestValidationResult.Where(v => v.ErrorMessage != null).Select(v => v.ErrorMessage))}");
                    }
                    else
                    {
                        await HandleJoinGameRequest(joinGameRequest, incomingEnvelope.CorrelationId);
                    }

                    break;
                case RequestNames.KickPlayer:
                    await HandleKickPlayerRequest(Unpack<KickPlayerRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.LeaveGame:
                    await HandleLeaveGameRequest(Unpack<LeaveGameRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.MoveUnit:
                    await HandleMoveUnitRequest(Unpack<MoveUnitRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendDamageInstanceRequest:
                    await HandleSendDamageRequest(Unpack<SendDamageInstanceRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendGameOptions:
                    await HandleSendGameOptionsRequest(Unpack<SendGameOptionsRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendPlayerOptions:
                    await HandleSendPlayerOptionsRequest(Unpack<SendPlayerOptionsRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                case RequestNames.SendPlayerState:
                    await HandleSendPlayerStateRequest(Unpack<SendPlayerStateRequest>(incomingEnvelope.Data), incomingEnvelope.CorrelationId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(incomingEnvelope.Type), $"No handler defined for request type {incomingEnvelope.Type}");
            }
        }

        private static (bool, List<ValidationResult>) ValidateObject(object validatedObject)
        {
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(validatedObject, new ValidationContext(validatedObject), validationResults, true);
            
            return (result, validationResults);
        }

        /// <inheritdoc />
        public async Task Send<TType>(string clientName, string envelopeType, TType data)
        {
            await SendEnvelope(clientName, new Envelope(envelopeType, data));
        }

        /// <inheritdoc />
        public abstract Task<bool> HandleConnectRequest(ConnectRequest connectRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleDisconnectRequest(DisconnectRequest disconnectRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGetDamageReportsRequest(GetDamageReportsRequest getDamageReportsRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGetGameOptionsRequest(GetGameOptionsRequest getGameOptionsRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGetGameStateRequest(GetGameStateRequest getGameStateRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleGetPlayerOptionsRequest(GetPlayerOptionsRequest getPlayerOptionsRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleForceReadyRequest(ForceReadyRequest forceReadyRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleJoinGameRequest(JoinGameRequest joinGameRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleKickPlayerRequest(KickPlayerRequest kickPlayerRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleLeaveGameRequest(LeaveGameRequest leaveGameRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleMoveUnitRequest(MoveUnitRequest moveUnitRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleSendDamageRequest(SendDamageInstanceRequest sendDamageInstanceRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleSendGameOptionsRequest(SendGameOptionsRequest sendGameOptionsRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleSendPlayerOptionsRequest(SendPlayerOptionsRequest sendPlayerOptionsRequest, Guid correlationId);

        /// <inheritdoc />
        public abstract Task<bool> HandleSendPlayerStateRequest(SendPlayerStateRequest sendPlayerStateRequest, Guid correlationId);
    }
}