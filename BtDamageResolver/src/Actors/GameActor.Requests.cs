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
        public async Task RequestDamageReports(string askingPlayerId)
        {
            _logger.LogInformation("Game {gameId} is delivering a list of all damage reports to player actor {playerId}", this.GetPrimaryKeyString(), askingPlayerId);

            await DistributeAllDamageReportsToPlayer(askingPlayerId);
        }

        /// <inheritdoc />
        public async Task RequestGameOptions(string askingPlayerId)
        {
            await DistributeGameOptionsToPlayer(askingPlayerId);
        }

        /// <inheritdoc />
        public async Task RequestGameState(string askingPlayerId)
        {
            _logger.LogInformation("Game {gameId} is delivering the game state to player actor {playerId}", this.GetPrimaryKeyString(), askingPlayerId);

            await DistributeGameStateToPlayer(askingPlayerId);
        }

        /// <inheritdoc />
        public async Task RequestTargetNumbers(string askingPlayerId)
        {
            var units = _gameActorState.State.PlayerStates[askingPlayerId].UnitEntries.Select(u => u.Id).ToList();

            var targetNumbersForPlayer = await ProcessTargetNumberUpdatesForUnits(units);

            await DistributeTargetNumberUpdatesToPlayer(askingPlayerId, targetNumbersForPlayer);
        }
    }
}