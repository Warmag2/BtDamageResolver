using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class PlayerActor
    {
        /// <inheritdoc />
        public async Task<GameOptions> GetGameOptions(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return null;
            }

            _logger.LogInformation("Player {playerId} requested game options.", this.GetPrimaryKeyString());

            if (_playerActorState.State.GameId != null)
            {
                var grain = GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId);
                return await grain.GetGameOptions(authenticationToken);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<bool> SetGameOptions(Guid authenticationToken, GameOptions gameOptions)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} is trying to set game options.", this.GetPrimaryKeyString());

            if (_playerActorState.State.GameId != null)
            {
                return await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).SetGameOptions(authenticationToken, gameOptions);
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<PlayerOptions> GetPlayerOptions(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return null;
            }
            
            _logger.LogInformation("Player {playerId} requested player options.", this.GetPrimaryKeyString());

            return _playerActorState.State.Options;
        }

        /// <inheritdoc />
        public async Task<bool> SetPlayerOptions(Guid authenticationToken, PlayerOptions playerOptions)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _playerActorState.State.Options = playerOptions;
            await _playerActorState.WriteStateAsync();

            _logger.LogInformation("Player {playerId} updated player options.", this.GetPrimaryKeyString());

            return true;
        }
    }
}