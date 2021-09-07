using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Events;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.Api.Interfaces
{
    public interface IClientInterface : IGrainWithStringKey
    {
        /// <summary>
        /// Connect to the selected player actor.
        /// </summary>
        /// <param name="password">The password for the actor.</param>
        /// <remarks>
        /// If this player actor has not yet been claimed, its password is set to the selected password and the
        /// method returns a success.
        /// </remarks>
        /// <returns>An authentication token to use while communicating to the player actor from the client.</returns>
        Task<LoginState> Connect(string password);

        /// <summary>
        /// Disconnect the selected player actor from all games and external data endpoints.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns><b>True</b> if the player actor was disconnected successfully, <b>false</b> otherwise or if the player actor did not exist previously.</returns>
        Task<bool> Disconnect(Guid authenticationToken);

        /// <summary>
        /// Join a game. If successful, the game ID and password are stored.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="gameId">The game id to connect to.</param>
        /// <param name="password">The password for the game.</param>
        /// <returns><b>True</b> if the client successfully connected to the game, <b>false</b> otherwise.</returns>
        Task<bool> JoinGame(Guid authenticationToken, string gameId, string password);

        /// <summary>
        /// Leave the current game.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns><b>True</b> if the client successfully left the game, <b>false</b> otherwise.</returns>
        Task<bool> LeaveGame(Guid authenticationToken);

        /// <summary>
        /// Connect the subscriber to this grain, so that the grain can send the user data.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="client">The <see cref="IResolverClient"/> grain observer reference.</param>
        /// <returns></returns>
        Task<bool> ConnectSubscriber(Guid authenticationToken, IResolverClient client);

        /// <summary>
        /// Update the state of this Player actor with the most recent data from the client.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerState">A <see cref="PlayerState"/> object containing the new state for this player.</param>
        /// <returns><b>True</b> if the actor has detected that its observer is in a faulted state <b>false</b> otherwise.</returns>
        Task UpdateState(Guid authenticationToken, PlayerState playerState);

        /// <summary>
        /// Request a full list of damage reports from the game the player is currently connected to.
        /// </summary>
        /// <returns><b>True</b> if the damage reports were successfully requested, <b>false</b> otherwise.</returns>
        Task<bool> RequestDamageReports(Guid authenticationToken);

        /// <summary>
        /// Request a full game state from the game the player is currently connected to.
        /// </summary>
        /// <returns><b>True</b> if the damage reports were successfully requested, <b>false</b> otherwise.</returns>
        Task<bool> RequestGameState(Guid authenticationToken);

        /// <summary>
        /// Process Damage request.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="damageRequest">The damage request.</param>
        /// <returns>Nothing.</returns>
        Task ProcessDamageRequest(Guid authenticationToken, DamageRequest damageRequest);

        /// <summary>
        /// Get the options for the game this player is in.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns>The <see cref="GameOptions"/> object containing the options for the game this player is in, or <b>null</b> on error.</returns>
        Task<GameOptions> GetGameOptions(Guid authenticationToken);

        /// <summary>
        /// Update the options for the game this player is in.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerOptions">The game options.</param>
        /// <returns><b>True</b> if the options were successfully updated, <b>false</b> otherwise.</returns>
        Task<bool> SetGameOptions(Guid authenticationToken, GameOptions playerOptions);

        /// <summary>
        /// Get the options for this player.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns>The <see cref="PlayerOptions"/> object containing the options for this player, or <b>null</b> on error.</returns>
        Task<PlayerOptions> GetPlayerOptions(Guid authenticationToken);

        /// <summary>
        /// Update the options for this player.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerOptions">The player options.</param>
        /// <returns><b>True</b> if the options were successfully updated, <b>false</b> otherwise.</returns>
        Task<bool> SetPlayerOptions(Guid authenticationToken, PlayerOptions playerOptions);

        /// <summary>
        /// Forces a ready state for all players in the same game as you, provided you have the authority.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns><b>True</b> if the state was successfully forced, <b>false</b> otherwise.</returns>
        public Task<bool> ForceReady(Guid authenticationToken);

        /// <summary>
        /// Kicks a player from the game you are currently in, provided you have the authority.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="playerId">The player id to kick.</param>
        /// <returns><b>True</b> if the player was successfully kicked, <b>false</b> otherwise.</returns>
        public Task<bool> KickPlayer(Guid authenticationToken, string playerId);

        /// <summary>
        /// Attempts to moves an unit to another player in the same game as this player, provided you have the authority.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <param name="unitId">The unit id to move.</param>
        /// <param name="playerId">The player to move the unit to.</param>
        /// <returns>Nothing.</returns>
        public Task MoveUnit(Guid authenticationToken, Guid unitId, string playerId);

        /// <summary>
        /// Check whether the connection is alive and operating correctly.
        /// </summary>
        /// <param name="authenticationToken">The authentication token.</param>
        /// <returns><b>True</b> if the client is operational.</returns>
        /// <remarks>Will always return false if the authentication token is incorrect.</remarks>
        public Task<bool> CheckConnection(Guid authenticationToken);
    }
}