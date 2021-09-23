using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class GameActor
    {
        /// <inheritdoc />
        public Task<GameOptions> GetGameOptions(Guid authenticationToken)
        {
            if (!CheckAuthentication(authenticationToken))
            {
                return null;
            }

            return Task.FromResult(_gameActorState.State.Options);
        }

        /// <inheritdoc />
        public async Task<bool> SetGameOptions(Guid authenticationToken, GameOptions gameOptions)
        {
            if (!CheckAuthentication(authenticationToken, _gameActorState.State.AdminId))
            {
                return false;
            }

            _gameActorState.State.Options = gameOptions;
            await _gameActorState.WriteStateAsync();
            _logger.LogInformation("Game {gameId} options successfully set by player {playerId}", this.GetPrimaryKeyString(), _gameActorState.State.AdminId);

            await DistributeGameOptionsToPlayers();

            return true;
        }
    }
}