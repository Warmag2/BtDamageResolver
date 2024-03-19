using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

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

        _logger.LogInformation("Player {PlayerId} asking Game {GameId} to force ready state for all players.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
        return await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).ForceReady(this.GetPrimaryKeyString());
    }

    /// <inheritdoc />
    public async Task<bool> KickPlayer(Guid authenticationToken, string playerId)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        _logger.LogInformation("Player {PlayerId} asking Game {GameId} to kick Player {KickedPlayerId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId, playerId);
        return await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).KickPlayer(this.GetPrimaryKeyString(), playerId);
    }
}