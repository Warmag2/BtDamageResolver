using System;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// A partial class for GameActor containing game tools.
/// </summary>
public partial class GameActor
{
    /// <inheritdoc />
    public Task<bool> KickPlayer(string askingPlayerId, string playerId)
    {
        if (askingPlayerId != _gameActorState.State.AdminId)
        {
            _logger.LogWarning("In Game {gameId}, Player {playerId} failed to kick player {playerToKickId}. No admin authority.", this.GetPrimaryKeyString(), askingPlayerId, playerId);
            return Task.FromResult(false);
        }

        if (askingPlayerId == playerId)
        {
            _logger.LogWarning("In Game {gameId}, Player {playerId} tried to kick himself. Disallowing.", this.GetPrimaryKeyString(), playerId);
            return Task.FromResult(false);
        }

        var playerActor = GrainFactory.GetGrain<IPlayerActor>(playerId);

        // Perform the disconnect through the player actor.
        // Must be ignored because the player actor may be sending data simultaneously.
        playerActor.LeaveGame().Ignore();

        _logger.LogInformation("In Game {gameId}, Player {playerId} successfully kicked player {playerToKickId}.", this.GetPrimaryKeyString(), askingPlayerId, playerId);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<bool> ForceReady(string askingPlayerId)
    {
        if (askingPlayerId == _gameActorState.State.AdminId)
        {
            foreach (var state in _gameActorState.State.PlayerStates.Values)
            {
                state.IsReady = true;
            }

            _logger.LogInformation("In Game {gameId}, Player {playerId} successfully forced ready state for all players.", this.GetPrimaryKeyString(), _gameActorState.State.AdminId);

            await CheckGameStateUpdateEvents();

            return true;
        }

        _logger.LogWarning("In Game {gameId}, Player {playerId} failed to force ready state for all players. No admin authority.", this.GetPrimaryKeyString(), askingPlayerId);

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> MoveUnit(string askingPlayerId, Guid unitId, string playerId)
    {
        var unitOwner = _gameActorState.State.PlayerStates.Single(p => p.Value.UnitEntries.Exists(u => u.Id == unitId)).Key;

        if (playerId == unitOwner)
        {
            _logger.LogWarning("Game {gameId} refusing to move unit {unitId} to Player {sendingPlayerId}. Unit owner would not change.", this.GetPrimaryKeyString(), unitId, unitOwner);

            return false;
        }

        if (!_gameActorState.State.PlayerStates.ContainsKey(playerId))
        {
            _logger.LogWarning("Game {gameId} refusing to move unit {unitId} to Player {sendingPlayerId}. Receiving player is not in the game.", this.GetPrimaryKeyString(), unitId, unitOwner);

            return false;
        }

        if (askingPlayerId == unitOwner || askingPlayerId == _gameActorState.State.AdminId)
        {
            var unit = GetUnit(unitId);

            if (_gameActorState.State.PlayerStates[unitOwner].UnitEntries.Remove(unit))
            {
                _gameActorState.State.PlayerStates[playerId].UnitEntries.Add(unit);
                await CheckGameStateUpdateEvents();

                return true;
            }
        }

        _logger.LogWarning("Game {gameId} failed to move Unit {unitId} from Player {sendingPlayerId} to Player {receivingPlayerId}. Unknown error.", this.GetPrimaryKeyString(), unitId, unitOwner, playerId);

        return false;
    }
}