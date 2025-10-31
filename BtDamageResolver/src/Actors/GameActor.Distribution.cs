using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Constants;
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
        _logger.LogInformation("Game {GameId} is sending a damage report update to player {Player}.", this.GetPrimaryKeyString(), playerId);
        await _communicationServiceClient.Send(playerId, EventNames.DamageReports, _gameActorDamageReportState.State.DamageReports.GetAll());
    }

    /// <summary>
    /// Receives a set of damage reports and sends them to players.
    /// </summary>
    /// <param name="damageReports">A list of <see cref="DamageReport"/>s that are to be distributed to players.</param>
    private async Task DistributeDamageReportsToPlayers(IReadOnlyCollection<DamageReport> damageReports)
    {
        _logger.LogInformation("Game {GameId} is sending a damage report update to all players ({PlayerIds}).", this.GetPrimaryKeyString(), string.Join(", ", _gameActorState.State.PlayerIds));
        await _communicationServiceClient.SendToMany([.. _gameActorState.State.PlayerIds], EventNames.DamageReports, damageReports);
    }

    /// <summary>
    /// Sends the game state to a single player.
    /// </summary>
    /// <param name="playerId">The player ID of the player to send the game state to.</param>
    private async Task DistributeGameStateToPlayer(string playerId)
    {
        _logger.LogInformation("Game {GameId} is sending a game state update to player {Player}.", this.GetPrimaryKeyString(), playerId);
        await _communicationServiceClient.Send(playerId, EventNames.GameState, GetGameState(false));
    }

    /// <summary>
    /// Sends the game state update to all players.
    /// </summary>
    /// <param name="markStateAsNew">Mark the game state to be as recent as possible.</param>
    private async Task DistributeGameStateToPlayers(bool markStateAsNew)
    {
        _logger.LogInformation("Game {GameId} is sending a game state update to all players ({PlayerIds}).", this.GetPrimaryKeyString(), string.Join(", ", _gameActorState.State.PlayerIds));
        var gameState = GetGameState(markStateAsNew);

        await _communicationServiceClient.SendToMany([.. _gameActorState.State.PlayerIds], EventNames.GameState, gameState);
    }

    /// <summary>
    /// Sends the game options to a player.
    /// </summary>
    /// <param name="playerId">The player ID of the player to send the game options to.</param>
    private async Task DistributeGameOptionsToPlayer(string playerId)
    {
        _logger.LogInformation("Game {GameId} is sending an options update to player {Player}.", this.GetPrimaryKeyString(), playerId);
        await _communicationServiceClient.Send(playerId, EventNames.GameOptions, _gameActorState.State.Options);
    }

    /// <summary>
    /// Sends the game options to all players.
    /// </summary>
    private async Task DistributeGameOptionsToPlayers()
    {
        _logger.LogInformation("Game {GameId} is sending an options update to all players ({PlayerIds}).", this.GetPrimaryKeyString(), string.Join(", ", _gameActorState.State.PlayerIds));
        await _communicationServiceClient.SendToMany([.. _gameActorState.State.PlayerIds], EventNames.GameOptions, _gameActorState.State.Options);
    }

    /// <summary>
    /// Sends the target number updates to a players.
    /// </summary>
    private async Task DistributeTargetNumberUpdatesToPlayer(string playerId, List<TargetNumberUpdate> targetNumberUpdates)
    {
        _logger.LogInformation("Game {GameId} is sending {Count} target number updates to player {Player}.", this.GetPrimaryKeyString(), targetNumberUpdates.Count, playerId);
        await _communicationServiceClient.Send(playerId, EventNames.TargetNumbers, targetNumberUpdates);
    }

    /// <summary>
    /// Sends the target number updates to all players.
    /// </summary>
    private async Task DistributeTargetNumberUpdatesToPlayers(List<TargetNumberUpdate> targetNumberUpdates)
    {
        _logger.LogInformation("Game {GameId} is sending target number updates to all players ({PlayerIds}).", this.GetPrimaryKeyString(), string.Join(", ", _gameActorState.State.PlayerIds));
        await _communicationServiceClient.SendToMany([.. _gameActorState.State.PlayerIds], EventNames.TargetNumbers, targetNumberUpdates);
    }
}