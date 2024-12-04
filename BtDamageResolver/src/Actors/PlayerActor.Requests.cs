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

        _logger.LogInformation("Player {PlayerId} requested damage reports.", this.GetPrimaryKeyString());

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

        if (IsConnectedToGame())
        {
            _logger.LogInformation("Player {PlayerId} requested game options from game {GameId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
            await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).RequestGameOptions(this.GetPrimaryKeyString());
        }
        else
        {
            _logger.LogInformation("Player {PlayerId} requested game options, but is not in a game.", this.GetPrimaryKeyString());
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
            _logger.LogInformation("Player {PlayerId} requested game state from game {GameId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
            await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).RequestGameState(this.GetPrimaryKeyString());
        }
        else
        {
            _logger.LogInformation("Player {PlayerId} requested game state, but is not in a game. Sending only player own state.", this.GetPrimaryKeyString());
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

        _logger.LogInformation("Player {PlayerId} requested player options.", this.GetPrimaryKeyString());
        await SendDataToClient(EventNames.PlayerOptions, _playerActorState.State.Options);

        return true;
    }

    /// <summary>
    /// Request target numbers for the units this player controls.
    /// </summary>
    /// <remarks>No outside call path so authentication is not needed.</remarks>
    private async Task RequestTargetNumbers()
    {
        if (IsConnectedToGame())
        {
            _logger.LogInformation("Player {PlayerId} requesting target numbers from game {GameId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
            await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).RequestTargetNumbers(this.GetPrimaryKeyString());
        }
        else
        {
            _logger.LogInformation("Player {PlayerId} requested target numbers but is not in a game.", this.GetPrimaryKeyString());
        }
    }
}