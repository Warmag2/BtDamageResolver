using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class PlayerActor
    {
        /// <inheritdoc />
        public async Task<bool> GetGameOptions(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} requested game options.", this.GetPrimaryKeyString());

            if (_playerActorState.State.GameId != null)
            {
                var grain = GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId);
                await SendDataToClient(EventNames.GameOptions, await grain.GetGameOptions(authenticationToken));
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> SendGameOptions(Guid authenticationToken, GameOptions gameOptions)
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
        public async Task<bool> GetPlayerOptions(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }
            
            _logger.LogInformation("Player {playerId} requested player options.", this.GetPrimaryKeyString());
            await SendDataToClient(EventNames.PlayerOptions, _playerActorState.State.Options);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> SendPlayerOptions(Guid authenticationToken, PlayerOptions playerOptions)
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