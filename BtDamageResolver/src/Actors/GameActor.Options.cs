using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    /// <summary>
    /// Game actor methods for options handling.
    /// </summary>
    public partial class GameActor
    {
        /// <inheritdoc />
        public async Task<bool> SendGameOptions(Guid authenticationToken, GameOptions gameOptions)
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