using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Game actor methods for options handling.
/// </summary>
public partial class GameActor
{
    /// <inheritdoc />
    public async Task<bool> SendGameOptions(string askingPlayerId, GameOptions gameOptions)
    {
        if (askingPlayerId != _gameActorState.State.AdminId)
        {
            _logger.LogWarning("Game {gameId} options not set. Player {playerId} does not have authority.", this.GetPrimaryKeyString(), askingPlayerId);

            return false;
        }

        _gameActorState.State.Options = gameOptions;
        await _gameActorState.WriteStateAsync();
        _logger.LogInformation("Game {gameId} options successfully set by player {playerId}", this.GetPrimaryKeyString(), _gameActorState.State.AdminId);

        await DistributeGameOptionsToPlayers();

        return true;
    }
}