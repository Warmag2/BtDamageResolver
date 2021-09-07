using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Events;
using Faemiyah.BtDamageResolver.Api.Interfaces;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces
{
    /// <summary>
    /// Interface for the Unit actor.
    /// </summary>
    public interface IPlayerActor : IClientInterface
    {
        /// <summary>
        /// Get the state of this player.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="markStateAsNew">Should the state be marked as a new state.</param>
        /// <returns>The <see cref="PlayerState"/> object containing the properties of this unit actor.</returns>
        public Task<PlayerState> GetPlayerState(Guid authenticationToken, bool markStateAsNew);

        /// <summary>
        /// Sends a damage report to the client.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="damageReport">The damage report to send.</param>
        /// <remarks>
        /// For internal server use. Do not use in the client.
        /// </remarks>
        /// <returns><b>True</b> if sending the damage reports was successful, <b>false</b> otherwise.</returns>
        public Task<bool> SendDamageReportsToClient(Guid authenticationToken, List<DamageReport> damageReport);

        /// <summary>
        /// Sends a collection of target number updates to the client.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="gameOptions">The game options to send.</param>
        /// <remarks>
        /// For internal server use. Do not use in the client.
        /// </remarks>
        /// <returns><b>True</b> if sending the options was successful, <b>false</b> otherwise.</returns>
        public Task<bool> SendGameOptionsToClient(Guid authenticationToken, GameOptions gameOptions);

        /// <summary>
        /// Sends the game state to the currently subscribed client.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="gameState">The game state to send.</param>
        /// <remarks>
        /// For internal server use. Do not use in the client.
        /// </remarks>
        /// <returns><b>True</b> if sending the state was successful, <b>false</b> otherwise.</returns>
        public Task<bool> SendGameStateToClient(Guid authenticationToken, GameState gameState);

        /// <summary>
        /// Sends a collection of target number updates to the client.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="targetNumberUpdates">The target number updates to send.</param>
        /// <remarks>
        /// For internal server use. Do not use in the client.
        /// </remarks>
        /// <returns><b>True</b> if sending the target numbers was successful, <b>false</b> otherwise.</returns>
        public Task<bool> SendTargetNumberUpdatesToClient(Guid authenticationToken, List<TargetNumberUpdate> targetNumberUpdates);

        /// <summary>
        /// Sends a ping to the client.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <remarks>
        /// For internal server use. Do not use in the client.
        /// </remarks>
        /// <returns><b>True</b> if sending the was successful, <b>false</b> otherwise.</returns>
        public Task<bool> SendPingToClient(Guid authenticationToken);

        /// <summary>
        /// Tries to receive an unit and mark it as the property of this player, provided you have the authority.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="unitId">The unit id to receive.</param>
        /// <param name="owningPlayerId">The player id of the player who owns the unit.</param>
        /// <param name="ownerAuthenticationToken">The authentication token of the owner of this unit.</param>
        /// <returns><b>True</b> if the unit was successfully received, <b>false</b> otherwise.</returns>
        public Task<bool> ReceiveUnit(Guid authenticationToken, Guid unitId, string owningPlayerId, Guid ownerAuthenticationToken);

        /// <summary>
        /// Asks the player to remove an unit.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="unitId">The unit to remove.</param>
        /// <returns><b>True</b> if the removal is authorized and successful, <b>false</b> otherwise.</returns>
        public Task<bool> RemoveUnit(Guid authenticationToken, Guid unitId);

        /// <summary>
        /// Removes the ready status from this player.
        /// </summary>
        public Task UnReady();
    }
}