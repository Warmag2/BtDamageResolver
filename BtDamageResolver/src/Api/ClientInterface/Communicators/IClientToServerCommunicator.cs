﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators
{
    public interface IClientToServerCommunicator
    {
        /// <summary>
        /// Sends data to the server.
        /// </summary>
        /// <param name="envelopeType">The type of the envelope.</param>
        /// <param name="data">The data to send.</param>
        /// <remarks>
        /// Envelope type is a processing hint for the recipient.
        /// </remarks>
        public Task Send<TType>(string envelopeType, TType data);

        /// <summary>
        /// Handle an incoming <see cref="ConnectionResponse"/>.
        /// </summary>
        /// <param name="connectionResponse">The connection response.</param>
        /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
        /// <returns><b>True</b> if the connection response was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleConnectionResponse(ConnectionResponse connectionResponse, Guid correlationId);

        /// <summary>
        /// Handle incoming <see cref="DamageReport"/>s.
        /// </summary>
        /// <param name="damageReports">The damage reports.</param>
        /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
        /// <returns><b>True</b> if the damage reports were successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleDamageReports(List<DamageReport> damageReports, Guid correlationId);

        /// <summary>
        /// Handle an incoming error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
        /// <returns><b>True</b> if the error message was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleErrorMessage(string errorMessage, Guid correlationId);

        /// <summary>
        /// Handle incoming <see cref="GameOptions"/>.
        /// </summary>
        /// <param name="gameOptions">The game options.</param>
        /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
        /// <returns><b>True</b> if the game options were successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleGameOptions(GameOptions gameOptions, Guid correlationId);

        /// <summary>
        /// Handle an incoming <see cref="GameState"/>.
        /// </summary>
        /// <param name="gameState">The game state.</param>
        /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
        /// <returns><b>True</b> if the game state was successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleGameState(GameState gameState, Guid correlationId);

        /// <summary>
        /// Handle incoming <see cref="PlayerOptions"/>.
        /// </summary>
        /// <param name="playerOptions">The player options.</param>
        /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
        /// <returns><b>True</b> if the player options were successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandlePlayerOptions(PlayerOptions playerOptions, Guid correlationId);

        /// <summary>
        /// Handle incoming <see cref="TargetNumberUpdate"/>s.
        /// </summary>
        /// <param name="targetNumbers">The target numbers.</param>
        /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
        /// <returns><b>True</b> if the target numbers were successfully handled, <b>false</b> otherwise.</returns>
        public Task<bool> HandleTargetNumberUpdates(List<TargetNumberUpdate> targetNumbers, Guid correlationId);
    }
}