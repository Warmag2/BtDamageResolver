using System;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    /// <summary>
    /// A partial class for GameActor containing game tools.
    /// </summary>
    public partial class GameActor
    {
        /// <inheritdoc />
        public Task<bool> KickPlayer(Guid authenticationToken, string playerId)
        {
            if (!CheckAuthentication(authenticationToken, _gameActorState.State.AdminId))
            {
                _logger.LogWarning("In Game {gameId}, Player {playerId} failed to kick player {playerToKickId}.", this.GetPrimaryKeyString(), _gameActorState.State.AuthenticationTokens[authenticationToken], playerId);
                return Task.FromResult(false);
            }

            if (_gameActorState.State.AuthenticationTokens[authenticationToken] == playerId)
            {
                _logger.LogWarning("In Game {gameId}, Player {playerId} tried to kick himself. Disallowing.", this.GetPrimaryKeyString(), _gameActorState.State.AuthenticationTokens[authenticationToken]);
                return Task.FromResult(false);
            }

            var playerActor = GrainFactory.GetGrain<IPlayerActor>(playerId);

            // Perform the disconnect through the player actor, since the game knows his authentication token
            playerActor.LeaveGame(_gameActorState.State.AuthenticationTokens.SingleOrDefault(a => a.Value == playerId).Key).Ignore();

            _logger.LogInformation("In Game {gameId}, Player {playerId} successfully kicked player {playerToKickId}.", this.GetPrimaryKeyString(), _gameActorState.State.AuthenticationTokens[authenticationToken], playerId);

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<bool> ForceReady(Guid authenticationToken)
        {
            if (CheckAuthentication(authenticationToken, _gameActorState.State.AdminId))
            {
                foreach (var state in _gameActorState.State.PlayerStates.Values)
                {
                    state.IsReady = true;
                }

                _logger.LogInformation("In Game {gameId}, Player {playerId} successfully forced ready state for all players.", this.GetPrimaryKeyString(), _gameActorState.State.AuthenticationTokens[authenticationToken]);

                await CheckGameStateUpdateEvents();

                return true;
            }

            _logger.LogWarning("In Game {gameId}, Player {playerId} failed to force ready state for all players.", this.GetPrimaryKeyString(), _gameActorState.State.AuthenticationTokens[authenticationToken]);

            return false;
        }

        /// <inheritdoc />
        public async Task<bool> MoveUnit(Guid authenticationToken, Guid unitId, string playerId)
        {
            var unitOwner = _gameActorState.State.PlayerStates.Single(p => p.Value.UnitEntries.Any(u => u.Id == unitId)).Key;

            if (playerId == unitOwner)
            {
                _logger.LogWarning("Game {gameId} refusing to move unit {unitId} to Player {sendingPlayerId}. Unit owner would not change.", this.GetPrimaryKeyString(), unitId, unitOwner);

                return false;
            }

            if (CheckAuthentication(authenticationToken, unitOwner) || CheckAuthentication(authenticationToken, _gameActorState.State.AdminId))
            {
                var playerActorReceivingUnit = GrainFactory.GetGrain<IPlayerActor>(playerId);

                if (await playerActorReceivingUnit.ReceiveUnit(GetAuthenticationTokenForPlayer(playerId), unitId, unitOwner, GetAuthenticationTokenForPlayer(unitOwner)))
                {
                    await UpdateStateForPlayer(unitOwner);
                    await UpdateStateForPlayer(playerId);

                    _logger.LogInformation("Game {gameId} successfully moved Unit {unitId} from Player {sendingPlayerId} to Player {receivingPlayerId}.", this.GetPrimaryKeyString(), unitId, unitOwner, playerId);

                    await CheckGameStateUpdateEvents();

                    return true;
                }
            }

            _logger.LogWarning("Game {gameId} failed to move Unit {unitId} from Player {sendingPlayerId} to Player {receivingPlayerId}.", this.GetPrimaryKeyString(), unitId, unitOwner, playerId);

            return false;
        }
    }
}