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
            _logger.LogWarning("In Game {GameId}, Player {PlayerId} failed to kick player {PlayerToKickId}. No admin authority.", this.GetPrimaryKeyString(), askingPlayerId, playerId);
            return Task.FromResult(false);
        }

        if (askingPlayerId == playerId)
        {
            _logger.LogWarning("In Game {GameId}, Player {PlayerId} tried to kick himself. Disallowing.", this.GetPrimaryKeyString(), playerId);
            return Task.FromResult(false);
        }

        var playerActor = GrainFactory.GetGrain<IPlayerActor>(playerId);

        // Perform the disconnect through the player actor.
        // Must be ignored because the player actor may be sending data simultaneously.
        playerActor.LeaveGame().Ignore();

        _logger.LogInformation("In Game {GameId}, Player {PlayerId} successfully kicked player {PlayerToKickId}.", this.GetPrimaryKeyString(), askingPlayerId, playerId);

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

            _logger.LogInformation("In Game {GameId}, Player {PlayerId} successfully forced ready state for all players.", this.GetPrimaryKeyString(), _gameActorState.State.AdminId);

            await CheckGameStateUpdateEvents();

            return true;
        }

        _logger.LogWarning("In Game {GameId}, Player {PlayerId} failed to force ready state for all players. No admin authority.", this.GetPrimaryKeyString(), askingPlayerId);

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> MoveUnit(string askingPlayerId, Guid unitId, string playerId)
    {
        var unitOwner = _gameActorState.State.PlayerStates.Single(p => p.Value.UnitEntries.Exists(u => u.Id == unitId)).Key;

        if (playerId == unitOwner)
        {
            _logger.LogWarning("Game {GameId} refusing to move unit {UnitId} to Player {SendingPlayerId}. Unit owner would not change.", this.GetPrimaryKeyString(), unitId, unitOwner);

            return false;
        }

        if (!_gameActorState.State.PlayerStates.TryGetValue(playerId, out var value))
        {
            _logger.LogWarning("Game {GameId} refusing to move unit {UnitId} to Player {SendingPlayerId}. Receiving player is not in the game.", this.GetPrimaryKeyString(), unitId, unitOwner);

            return false;
        }

        if (askingPlayerId == unitOwner || askingPlayerId == _gameActorState.State.AdminId)
        {
            var unit = GetUnit(unitId);

            if (_gameActorState.State.PlayerStates[unitOwner].UnitEntries.Remove(unit))
            {
                value.UnitEntries.Add(unit);
                await CheckGameStateUpdateEvents();

                return true;
            }
        }

        _logger.LogWarning("Game {GameId} failed to move Unit {UnitId} from Player {SendingPlayerId} to Player {ReceivingPlayerId}. Unknown error.", this.GetPrimaryKeyString(), unitId, unitOwner, playerId);

        return false;
    }
}