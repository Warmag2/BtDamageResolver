using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces
{
    /// <summary>
    /// Interface for the Unit actor.
    /// </summary>
    public partial interface IPlayerActor : IGrainWithStringKey
    {
        /// <summary>
        /// Get the state of this player.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="markStateAsNew">Should the state be marked as a new state.</param>
        /// <returns>The <see cref="PlayerState"/> object containing the properties of this unit actor.</returns>
        public Task<PlayerState> GetPlayerState(Guid authenticationToken, bool markStateAsNew);

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