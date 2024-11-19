using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces;

/// <summary>
/// Interface for the Unit actor.
/// </summary>
public interface IGameActor : IGrainWithStringKey
{
    /// <summary>
    /// Ask whether a certain unit is in this game.
    /// </summary>
    /// <param name="unitId">The id of the unit.</param>
    /// <returns><b>True</b> if the unit is in this game, <b>false</b> otherwise.</returns>
    public Task<bool> IsUnitInGame(Guid unitId);

    /// <summary>
    /// Forces a ready state for all players in the game.
    /// </summary>
    /// <param name="askingPlayerId">The player asking for force ready.</param>
    /// <returns><b>True</b> if the state was successfully forced, <b>false</b> otherwise.</returns>
    public Task<bool> ForceReady(string askingPlayerId);

    /// <summary>
    /// Connect a player to this game.
    /// </summary>
    /// <param name="playerId">The player ID wanting to connect to the game.</param>
    /// <param name="password">The password to use when connecting.</param>
    /// <returns><b>True</b> if the connection was successful, <b>false</b> otherwise.</returns>
    public Task<bool> JoinGame(string playerId, string password);

    /// <summary>
    /// Disconnect a player from this game.
    /// </summary>
    /// <param name="playerId">The player ID wanting to disconnect from the game.</param>
    /// <returns><b>True</b> if the disconnection succeeds, <b>false</b> otherwise.</returns>
    public Task<bool> LeaveGame(string playerId);

    /// <summary>
    /// Kick a player from this game.
    /// </summary>
    /// <param name="askingPlayerId">The asking player Id.</param>
    /// <param name="playerId">The player ID to disconnect from the game.</param>
    /// <returns><b>True</b> if the disconnection succeeds, <b>false</b> otherwise.</returns>
    public Task<bool> KickPlayer(string askingPlayerId, string playerId);

    /// <summary>
    /// Moves an unit to another player in the game.
    /// </summary>
    /// <param name="askingPlayerId">The asking player Id.</param>
    /// <param name="unitId">The unit id to move.</param>
    /// <param name="playerId">The player id to move to.</param>
    /// <returns><b>True</b> if the unit was successfully moved, <b>false</b> otherwise.</returns>
    public Task<bool> MoveUnit(string askingPlayerId, Guid unitId, string playerId);

    /// <summary>
    /// Request the game actor to send all damage reports which have happened in the game so far to the asking player.
    /// </summary>
    /// <param name="askingPlayerId">The asking player Id.</param>
    /// <returns><b>True</b> if the damage reports were sent, <b>false</b> otherwise.</returns>
    public Task RequestDamageReports(string askingPlayerId);

    /// <summary>
    /// Request the game actor to send the game options to the asking player.
    /// </summary>
    /// <param name="askingPlayerId">The asking player Id.</param>
    /// <returns><b>True</b> if the game options were sent, <b>false</b> otherwise.</returns>
    public Task RequestGameOptions(string askingPlayerId);

    /// <summary>
    /// Request the game actor to send the game state to the asking player.
    /// </summary>
    /// <param name="askingPlayerId">The asking player Id.</param>
    /// <returns><b>True</b> if the game state was sent, <b>false</b> otherwise.</returns>
    public Task RequestGameState(string askingPlayerId);

    /// <summary>
    /// Request the game actor to send the current target numbers to the asking player.
    /// </summary>
    /// <param name="askingPlayerId">The asking player Id.</param>
    /// <returns>A task which finishes when the request was processed.</returns>
    public Task RequestTargetNumbers(string askingPlayerId);

    /// <summary>
    /// Receive a damage instance, process it and distribute the results to other players in the game.
    /// </summary>
    /// <param name="sendingPlayerId">The sending player Id.</param>
    /// <param name="damageInstance">The damage instance.</param>
    /// <returns>A task which finishes when the damage instance was processed.</returns>
    public Task<bool> SendDamageInstance(string sendingPlayerId, DamageInstance damageInstance);

    /// <summary>
    /// Receives new game options and distribute them to other players in the game.
    /// </summary>
    /// <param name="askingPlayerId">The asking player Id.</param>
    /// <param name="gameOptions">A <see cref="GameOptions"/> object containing the new game options.</param>
    /// <returns><b>True</b> if the game options were successfully updated, <b>false</b> otherwise.</returns>
    public Task<bool> SendGameOptions(string askingPlayerId, GameOptions gameOptions);

    /// <summary>
    /// Receives a player state and distribute the new state to other players in the game.
    /// </summary>
    /// <param name="sendingPlayerId">The sending player Id.</param>
    /// <param name="playerState">A <see cref="PlayerState"/> object containing the player state to be distributed.</param>
    /// <param name="unitIds">List of unit IDs which were actually updated in this update request.</param>
    /// <returns>A task which finishes when the player state was processed.</returns>
    public Task<bool> SendPlayerState(string sendingPlayerId, PlayerState playerState, List<Guid> unitIds);
}