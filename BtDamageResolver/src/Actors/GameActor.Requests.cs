using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    /// <summary>
    /// Any data requests for this game actor are processed here.
    /// </summary>
    public partial class GameActor
    {
        /// <inheritdoc />
        public async Task<bool> RequestDamageReports(Guid authenticationToken)
        {
            if (!CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Game {gameId} is delivering a list of all damage reports to player actor {playerId}", this.GetPrimaryKeyString(), _gameActorState.State.AuthenticationTokens[authenticationToken]);

            await DistributeAllDamageReportsToPlayer(authenticationToken);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RequestGameOptions(Guid authenticationToken)
        {
            if (!CheckAuthentication(authenticationToken))
            {
                return false;
            }

            await DistributeGameOptionsToPlayer(GetPlayerForAuthenticationToken(authenticationToken));

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RequestGameState(Guid authenticationToken)
        {
            if (!CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Game {gameId} is delivering the game state to player actor {playerId}", this.GetPrimaryKeyString(), _gameActorState.State.AuthenticationTokens[authenticationToken]);

            await DistributeGameStateToPlayer(authenticationToken);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RequestTargetNumbers(Guid authenticationToken)
        {
            if (!CheckAuthentication(authenticationToken))
            {
                return false;
            }

            var player = GetPlayerForAuthenticationToken(authenticationToken);
            var units = _gameActorState.State.PlayerStates[player].UnitEntries.Select(u => u.Id).ToList();

            var targetNumbersForPlayer = await ProcessTargetNumberUpdatesForUnits(units);

            await DistributeTargetNumberUpdatesToPlayer(player, targetNumbersForPlayer);

            return true;
        }
    }
}