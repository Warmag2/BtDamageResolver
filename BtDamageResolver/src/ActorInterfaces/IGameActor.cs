using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces
{
    /// <summary>
    /// Interface for the Unit actor.
    /// </summary>
    public interface IGameActor : IGrainWithStringKey
    {
        /// <summary>
        /// Process a damage instance.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="damageInstance">The damage instance.</param>
        /// <returns><b>True</b> if processing the damage instance succeeded, <b>false</b> otherwise.</returns>
        Task<bool> ProcessDamageInstance(Guid authenticationToken, DamageInstance damageInstance);

        /// <summary>
        /// Ask whether a certain unit is in this game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="unitId">The id of the unit.</param>
        /// <returns><b>True</b> if the unit is in this game, <b>false</b> otherwise.</returns>
        Task<bool> IsUnitInGame(Guid authenticationToken, Guid unitId);

        /// <summary>
        /// Receives a player state and distribute it to other players in the game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerState">A <see cref="PlayerState"/> object containing the player state to be distributed.</param>
        /// <param name="unitIds">List of unit IDs which were actually updated in this update request.</param>
        /// <returns>Nothing.</returns>
        Task UpdatePlayerState(Guid authenticationToken, PlayerState playerState, List<Guid> unitIds);

        /// <summary>
        /// Request the game actor to send all damage reports which have happened in the game so far to the asking player.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns><b>True</b> if the damage reports were sent, <b>false</b> otherwise.</returns>
        Task<bool> RequestDamageReports(Guid authenticationToken);

        /// <summary>
        /// Request the game actor to send the game state to the asking player.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns><b>True</b> if the game state was sent, <b>false</b> otherwise.</returns>
        Task<bool> RequestGameState(Guid authenticationToken);

        /// <summary>
        /// Connect a player to this game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerId">The player ID to connect to the game.</param>
        /// <param name="password">The password to use when connecting.</param>
        /// <returns><b>True</b> if the connection was successful, <b>false</b> otherwise.</returns>
        Task<bool> JoinGame(Guid authenticationToken, string playerId, string password);

        /// <summary>
        /// Disconnect a player from this game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerId">The player ID to disconnect from the game.</param>
        /// <returns><b>True</b> if the disconnection succeeds, <b>false</b> otherwise.</returns>
        Task<bool> LeaveGame(Guid authenticationToken, string playerId);

        /// <summary>
        /// Kick a player from this game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerId">The player ID to disconnect from the game.</param>
        /// <returns><b>True</b> if the disconnection succeeds, <b>false</b> otherwise.</returns>
        Task<bool> KickPlayer(Guid authenticationToken, string playerId);

        /// <summary>
        /// Forces a ready state for all players in the game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns><b>True</b> if the state was successfully forced, <b>false</b> otherwise.</returns>
        Task<bool> ForceReady(Guid authenticationToken);

        /// <summary>
        /// Moves an unit to another player in the game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="unitId">The unit id to move.</param>
        /// <param name="playerId">The player id to move to.</param>
        /// <returns><b>True</b> if the unit was successfully moved, <b>false</b> otherwise.</returns>
        Task<bool> MoveUnit(Guid authenticationToken, Guid unitId, string playerId);

        /// <summary>
        /// Get the options for this game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns>The <see cref="GameOptions"/> object containing the options for this game.</returns>
        Task<GameOptions> GetGameOptions(Guid authenticationToken);

        /// <summary>
        /// Receives new game options and distribute them to other players in the game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="gameOptions">A <see cref="GameOptions"/> object containing the new game options.</param>
        /// <returns><b>True</b> if the game options were successfully updated, <b>false</b> otherwise.</returns>
        Task<bool> SetGameOptions(Guid authenticationToken, GameOptions gameOptions);
    }
}