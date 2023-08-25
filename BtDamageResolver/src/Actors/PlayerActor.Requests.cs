using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Partial class for handling any requests from the client.
/// </summary>
public partial class PlayerActor
{
    /// <inheritdoc />
    public async Task<bool> RequestDamageReports(Guid authenticationToken)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        _logger.LogInformation("Player {playerId} requested damage reports.", this.GetPrimaryKeyString());

        if (IsConnectedToGame())
        {
            await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).RequestDamageReports(this.GetPrimaryKeyString());
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> RequestGameOptions(Guid authenticationToken)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        _logger.LogInformation("Player {playerId} requested game options.", this.GetPrimaryKeyString());
        if (_playerActorState.State.GameId != null)
        {
            var gameActor = GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId);
            await gameActor.RequestGameOptions(this.GetPrimaryKeyString());
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RequestGameState(Guid authenticationToken)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        if (IsConnectedToGame())
        {
            await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).RequestGameState(this.GetPrimaryKeyString());
        }
        else
        {
            await SendOnlyThisPlayerGameStateToClient();
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RequestPlayerOptions(Guid authenticationToken)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        _logger.LogInformation("Player {playerId} requested player options.", this.GetPrimaryKeyString());
        await SendDataToClient(EventNames.PlayerOptions, _playerActorState.State.Options);

        return true;
    }

    /// <summary>
    /// Request target numbers for the units this player controls.
    /// </summary>
    /// <remarks>No outside call path so authentication is not needed.</remarks>
    private async Task RequestTargetNumbers()
    {
        if (_playerActorState.State.GameId != null)
        {
            _logger.LogInformation("Player {playerId} requesting target numbers from game {gameId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
            var gameActor = GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId);
            await gameActor.RequestTargetNumbers(this.GetPrimaryKeyString());
        }
        else
        {
            _logger.LogInformation("Player {playerId} is not in a game and won't ask for target numbers.", this.GetPrimaryKeyString());
        }
    }
}