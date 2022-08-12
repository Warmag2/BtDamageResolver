using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    /// <summary>
    /// Player Actor methods for player tools.
    /// </summary>
    public partial class PlayerActor
    {
        /// <inheritdoc />
        public async Task<bool> ForceReady(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} asking Game {gameId} to force ready state for all players.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
            return await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).ForceReady(this.GetPrimaryKeyString());
        }

        /// <inheritdoc />
        public async Task<bool> KickPlayer(Guid authenticationToken, string playerId)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} asking Game {gameId} to kick Player {kickedPlayerId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId, playerId);
            return await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).KickPlayer(this.GetPrimaryKeyString(), playerId);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveUnit(Guid authenticationToken, Guid unitId)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (_playerActorState.State.UnitEntryIds.Contains(unitId))
            {
                _playerActorState.State.UnitEntryIds.Remove(unitId);
                _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;

                _logger.LogInformation("Player {playerId} removed Unit {unitId} from their inventory.", this.GetPrimaryKeyString(), unitId);

                return true;
            }

            _logger.LogWarning("Player {playerId} failed to remove Unit {unitId} from their inventory.", this.GetPrimaryKeyString(), unitId);

            return false;
        }

        /// <inheritdoc />
        public async Task<bool> ReceiveUnit(Guid authenticationToken, Guid unitId, string owningPlayerId, Guid ownerAuthenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            var owningPlayerActor = GrainFactory.GetGrain<IPlayerActor>(owningPlayerId);

            if (!_playerActorState.State.UnitEntryIds.Contains(unitId) && await owningPlayerActor.RemoveUnit(ownerAuthenticationToken, unitId))
            {
                _playerActorState.State.UnitEntryIds.Add(unitId);
                _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;

                _logger.LogInformation("Player {playerId} added Unit {unitId} to their inventory.", this.GetPrimaryKeyString(), unitId);

                return true;
            }

            _logger.LogWarning("Player {playerId} failed to add Unit {unitId} to their inventory.", this.GetPrimaryKeyString(), unitId);

            return false;
        }
    }
}