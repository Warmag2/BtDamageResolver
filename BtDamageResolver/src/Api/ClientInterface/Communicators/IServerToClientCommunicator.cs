﻿using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators
{
    public interface IServerToClientCommunicator
    {
        /// <summary>
        /// Sends data to the selected communication endpoint.
        /// </summary>
        /// <param name="clientName">The target client where to send data.</param>
        /// <param name="envelopeType">The envelope type.</param>
        /// <param name="data">The data to send.</param>
        /// <remarks>
        /// Envelope type is a processing hint for the recipient.
        /// </remarks>
        public Task Send<TType>(string clientName, string envelopeType, TType data);

        /// <summary>
        /// Handle an incoming <see cref="ConnectRequest"/>.
        /// </summary>
        /// <param name="connectRequest">The connect request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the connect request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleConnectRequest(ConnectRequest connectRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="DisconnectRequest"/>.
        /// </summary>
        /// <param name="disconnectRequest">The connect request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the connect request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleDisconnectRequest(DisconnectRequest disconnectRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="GetDamageReportsRequest"/>.
        /// </summary>
        /// <param name="getDamageReportsRequest">The get damage reports request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the get damage reports request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleGetDamageReportsRequest(GetDamageReportsRequest getDamageReportsRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="GetGameOptionsRequest"/>.
        /// </summary>
        /// <param name="getGameOptionsRequest">The get game options request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the get game options request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleGetGameOptionsRequest(GetGameOptionsRequest getGameOptionsRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="GetGameStateRequest"/>.
        /// </summary>
        /// <param name="getGameStateRequest">The get game state request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the get game state request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleGetGameStateRequest(GetGameStateRequest getGameStateRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="GetPlayerOptionsRequest"/>.
        /// </summary>
        /// <param name="getPlayerOptionsRequest">The get player options request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the get player options request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleGetPlayerOptionsRequest(GetPlayerOptionsRequest getPlayerOptionsRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="ForceReadyRequest"/>.
        /// </summary>
        /// <param name="forceReadyRequest">The force ready request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the force ready request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleForceReadyRequest(ForceReadyRequest forceReadyRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="JoinGameRequest"/>.
        /// </summary>
        /// <param name="joinGameRequest">The join game request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the join game request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleJoinGameRequest(JoinGameRequest joinGameRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="KickPlayerRequest"/>.
        /// </summary>
        /// <param name="kickPlayerRequest">The kick player request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the kick player request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleKickPlayerRequest(KickPlayerRequest kickPlayerRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="LeaveGameRequest"/>.
        /// </summary>
        /// <param name="leaveGameRequest">The leave game request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the leave game request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleLeaveGameRequest(LeaveGameRequest leaveGameRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="MoveUnitRequest"/>.
        /// </summary>
        /// <param name="moveUnitRequest">The move unit request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the move unit request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleMoveUnitRequest(MoveUnitRequest moveUnitRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="SendDamageInstanceRequest"/>.
        /// </summary>
        /// <param name="sendDamageInstanceRequest">The send damage request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the send damage request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleSendDamageRequest(SendDamageInstanceRequest sendDamageInstanceRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="SendGameOptionsRequest"/>.
        /// </summary>
        /// <param name="sendGameOptionsRequest">The send game options request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the send game options request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleSendGameOptionsRequest(SendGameOptionsRequest sendGameOptionsRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="SendPlayerOptionsRequest"/>.
        /// </summary>
        /// <param name="sendPlayerOptionsRequest">The send player options request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the send player options request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleSendPlayerOptionsRequest(SendPlayerOptionsRequest sendPlayerOptionsRequest, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="SendPlayerStateRequest"/>.
        /// </summary>
        /// <param name="sendPlayerStateRequest">The send player state request.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns><b>True</b> if the send player state request was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleSendPlayerStateRequest(SendPlayerStateRequest sendPlayerStateRequest, Guid correlationId);
    }
}