using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces;

/// <summary>
/// Interface for the Unit actor.
/// </summary>
public interface IPlayerActor : IGrainWithStringKey
{
    /// <summary>
    /// Connect to the selected player actor.
    /// </summary>
    /// <param name="password">The password for the actor.</param>
    /// <remarks>
    /// If this player actor has not yet been claimed, its password is set to the selected password and the
    /// method returns a success.
    /// </remarks>
    /// <returns><b>True</b> if the connection request succeeded, <b>false</b> otherwise.</returns>
    Task<bool> Connect(string password);

    /// <summary>
    /// Connect to the selected player actor.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the connection request succeeded, <b>false</b> otherwise.</returns>
    Task<bool> Connect(Guid authenticationToken);

    /// <summary>
    /// Disconnect the selected player actor from all games and external data endpoints.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the player actor was disconnected successfully, <b>false</b> otherwise or if the player actor did not exist previously.</returns>
    Task<bool> Disconnect(Guid authenticationToken);

    /// <summary>
    /// Forces a ready state for all players in the same game as you, provided you have the authority.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the state was successfully forced, <b>false</b> otherwise.</returns>
    public Task<bool> ForceReady(Guid authenticationToken);

    /// <summary>
    /// Gets the ID of the game this player is currently in.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns>The ID of the game this player is currently in, or null, if not connected to a game.</returns>
    Task<string> GetGameId(Guid authenticationToken);

    /// <summary>
    /// Join a game. If successful, the game ID and password are stored.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <param name="gameId">The game id to connect to.</param>
    /// <param name="password">The password for the game.</param>
    /// <returns><b>True</b> if the client successfully connected to the game, <b>false</b> otherwise.</returns>
    Task<bool> JoinGame(Guid authenticationToken, string gameId, string password);

    /// <summary>
    /// Kicks a player from the game you are currently in, provided you have the authority.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <param name="playerId">The player id to kick.</param>
    /// <returns><b>True</b> if the player was successfully kicked, <b>false</b> otherwise.</returns>
    public Task<bool> KickPlayer(Guid authenticationToken, string playerId);

    /// <summary>
    /// Leave the current game.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the client successfully left the game, <b>false</b> otherwise.</returns>
    Task<bool> LeaveGame(Guid authenticationToken);

    /// <summary>
    /// Leave the current game.
    /// </summary>
    /// <remarks>
    /// Overload for game actors to use without authentication.
    /// </remarks>
    /// <returns><b>True</b> if the client successfully left the game, <b>false</b> otherwise.</returns>
    Task<bool> LeaveGame();

    /// <summary>
    /// Request a full list of damage reports from the game the player is currently connected to.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the damage reports were successfully requested, <b>false</b> otherwise.</returns>
    Task<bool> RequestDamageReports(Guid authenticationToken);

    /// <summary>
    /// Request the options for the game this player is in.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the game options were successfully requested, <b>false</b> otherwise.</returns>
    Task<bool> RequestGameOptions(Guid authenticationToken);

    /// <summary>
    /// Request a full game state from the game the player is currently connected to.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the damage reports were successfully requested, <b>false</b> otherwise.</returns>
    Task<bool> RequestGameState(Guid authenticationToken);

    /// <summary>
    /// Request the options for this player.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <returns><b>True</b> if the player options were successfully requested, <b>false</b> otherwise.</returns>
    Task<bool> RequestPlayerOptions(Guid authenticationToken);

    /// <summary>
    /// Process a damage instance.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <param name="damageInstance">The damage request.</param>
    /// <returns><b>True</b> if the damage instance was successfully processed, <b>false</b> otherwise.</returns>
    Task<bool> SendDamageInstance(Guid authenticationToken, DamageInstance damageInstance);

    /// <summary>
    /// Update the options for the game this player is in.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <returns><b>True</b> if the options were successfully updated, <b>false</b> otherwise.</returns>
    Task<bool> SendGameOptions(Guid authenticationToken, GameOptions gameOptions);

    /// <summary>
    /// Update the options for this player.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <param name="playerOptions">The player options.</param>
    /// <returns><b>True</b> if the options were successfully updated, <b>false</b> otherwise.</returns>
    Task<bool> SendPlayerOptions(Guid authenticationToken, PlayerOptions playerOptions);

    /// <summary>
    /// Update the state of this Player actor with data from the client.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    /// <param name="playerState">A <see cref="PlayerState"/> object containing the new state for this player.</param>
    /// <returns><b>True</b> if the actor has detected that its observer is in a faulted state <b>false</b> otherwise.</returns>
    Task<bool> SendPlayerState(Guid authenticationToken, PlayerState playerState);

    /// <summary>
    /// Removes the ready status from this player.
    /// </summary>
    /// <returns>A task which finishes when the player ready state has been cleared.</returns>
    public Task UnReady();
}