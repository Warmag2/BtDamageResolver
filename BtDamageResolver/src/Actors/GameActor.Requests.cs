using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

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
        _logger.LogInformation("Game {gameId} is delivering the game state to player {playerId}", this.GetPrimaryKeyString(), askingPlayerId);

        await DistributeGameStateToPlayer(askingPlayerId);
    }

    /// <inheritdoc />
    public async Task RequestTargetNumbers(string askingPlayerId)
    {
        // If player state does not yet exist on the server, do not send anything
        if (_gameActorState.State.PlayerStates.TryGetValue(askingPlayerId, out var state))
        {
            var playerUnits = state.UnitEntries.Select(u => u.Id).ToList();

            var targetNumbersForPlayer = await ProcessTargetNumberUpdatesForUnits(playerUnits);

            await DistributeTargetNumberUpdatesToPlayer(askingPlayerId, targetNumbersForPlayer);
        }
        else
        {
            _logger.LogWarning("Game {gameId} refusing to deliver target numbers to player {playerId} - player state is empty.", this.GetPrimaryKeyString(), askingPlayerId);
        }
    }
}