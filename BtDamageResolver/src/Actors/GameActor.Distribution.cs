using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Entities;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Game actor methods responsible for data distribution.
/// </summary>
public partial class GameActor
{
    /// <summary>
    /// Send all damage reports which have been recorded in this game to the designated player.
    /// </summary>
    /// <param name="playerId">The player ID of the player to send the damage reports to.</param>
    private async Task DistributeAllDamageReportsToPlayer(string playerId)
    {
        await _communicationServiceClient.Send(playerId, EventNames.DamageReports, _gameActorState.State.DamageReports.GetAll());
    }

    /// <summary>
    /// Receives a set of damage reports and sends them to players.
    /// </summary>
    /// <param name="damageReports">A list of <see cref="DamageReport"/>s that are to be distributed to players.</param>
    private async Task DistributeDamageReportsToPlayers(List<DamageReport> damageReports)
    {
        _logger.LogInformation("Game {id} is sending an damage reports to all players.", this.GetPrimaryKeyString());

        await _communicationServiceClient.SendToMany(_gameActorState.State.PlayerIds.ToList(), EventNames.DamageReports, damageReports);
    }

    /// <summary>
    /// Sends the game state update to all players.
    /// </summary>
    /// <param name="markStateAsNew">Mark the game state to be as recent as possible.</param>
    private async Task DistributeGameStateToPlayers(bool markStateAsNew)
    {
        _logger.LogInformation("Game {id} is sending a game state update to all players.", this.GetPrimaryKeyString());
        var gameState = GetGameState(markStateAsNew);

        await _communicationServiceClient.SendToMany(_gameActorState.State.PlayerIds.ToList(), EventNames.GameState, gameState);
    }

    /// <summary>
    /// Sends the game state to a single player.
    /// </summary>
    /// <param name="playerId">The player ID of the player to send the game state to.</param>
    private async Task DistributeGameStateToPlayer(string playerId)
    {
        await _communicationServiceClient.Send(playerId, EventNames.GameState, GetGameState(false));
    }

    /// <summary>
    /// Sends the game options to a player.
    /// </summary>
    /// <param name="playerId">The player ID of the player to send the game options to.</param>
    private async Task DistributeGameOptionsToPlayer(string playerId)
    {
        _logger.LogInformation("Game {id} is sending an options update to player {player}.", this.GetPrimaryKeyString(), playerId);

        await _communicationServiceClient.Send(playerId, EventNames.GameOptions, _gameActorState.State.Options);
    }

    /// <summary>
    /// Sends the game options to all players.
    /// </summary>
    private async Task DistributeGameOptionsToPlayers()
    {
        _logger.LogInformation("Game {id} is sending an options update to all players.", this.GetPrimaryKeyString());

        await _communicationServiceClient.SendToMany(_gameActorState.State.PlayerIds.ToList(), EventNames.GameOptions, _gameActorState.State.Options);
    }

    /// <summary>
    /// Sends the target number updates to a players.
    /// </summary>
    private async Task DistributeTargetNumberUpdatesToPlayer(string playerId, List<TargetNumberUpdate> targetNumberUpdates)
    {
        _logger.LogInformation("Game {id} is sending {count} target number updates to player {player}.", this.GetPrimaryKeyString(), targetNumberUpdates.Count, playerId);

        await _communicationServiceClient.Send(playerId, EventNames.TargetNumbers, targetNumberUpdates);
    }

    /// <summary>
    /// Sends the target number updates to all players.
    /// </summary>
    private async Task DistributeTargetNumberUpdatesToPlayers(List<TargetNumberUpdate> targetNumberUpdates)
    {
        _logger.LogInformation("Game {id} is sending target number updates to all players.", this.GetPrimaryKeyString());

        await _communicationServiceClient.SendToMany(_gameActorState.State.PlayerIds.ToList(), EventNames.TargetNumbers, targetNumberUpdates);
    }
}