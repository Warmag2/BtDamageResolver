using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// The turn advancement logic for the game actor.
/// </summary>
public partial class GameActor
{
    private static void ProcessUnitHeat(List<DamageReport> damageReports, UnitEntry unit)
    {
        var heatGeneratedByThisUnit = damageReports.Where(d => d.FiringUnitId == unit.Id).Sum(damageReport => damageReport.AttackerHeat);

        // Only apply heat if the generation is positive and the unit tracks heat.
        // However, combat computer and other heat generation reductions never take the heat generated below 0.
        if (heatGeneratedByThisUnit > 0)
        {
            unit.Heat += heatGeneratedByThisUnit;
        }

        unit.Heat -= unit.Sinks;

        // Sinks won't take the heat to negative levels
        if (unit.Heat < 0)
        {
            unit.Heat = 0;
        }
    }

    private async Task CheckGameStateUpdateEvents(List<Guid> updatedUnits = null)
    {
        // Don't check against the player timestamp. If we received new data, then this actor state has updated by definition
        _gameActorState.State.TimeStamp = DateTime.UtcNow;

        CheckForPlayerCountEvents();

        _logger.LogInformation("DEBUG LOG - GameActor {gameId} Checked for player count events.", this.GetPrimaryKeyString());

        var fireEventHappened = await CheckForFireEvent();

        _logger.LogInformation("DEBUG LOG - GameActor {gameId} checked for fire events.", this.GetPrimaryKeyString());

        // If fire event happened or when given an empty unit set, process updated target numbers for everything.
        // Otherwise, only for affected units.
        var targetNumberUpdates = fireEventHappened || updatedUnits == null
            ? await ProcessTargetNumberUpdatesForUnits()
            : await ProcessTargetNumberUpdatesForUnits(updatedUnits);

        _logger.LogInformation("DEBUG LOG - GameActor {gameId} processed target numbers.", this.GetPrimaryKeyString());

        await DistributeTargetNumberUpdatesToPlayers(targetNumberUpdates);
        await DistributeGameStateToPlayers(fireEventHappened);

        _logger.LogInformation("DEBUG LOG - GameActor {gameId} distributed target numbers and game state.", this.GetPrimaryKeyString());

        // Save game actor state
        await _gameActorState.WriteStateAsync();

        _logger.LogInformation("DEBUG LOG - GameActor {gameId} wrote game state.", this.GetPrimaryKeyString());

        // Log update to permanent store
        await _loggingServiceClient.LogGameAction(DateTime.UtcNow, this.GetPrimaryKeyString(), GameActionType.Update, 1);

        // Remark game existence
        await GrainFactory.GetGameEntryRepository().AddOrUpdate(
            new GameEntry
            {
                Name = this.GetPrimaryKeyString(),
                PasswordProtected = !string.IsNullOrEmpty(_gameActorState.State.Password),
                Players = _gameActorState.State.PlayerStates.Count,
                TimeStamp = DateTime.UtcNow
            });

        _logger.LogInformation("DEBUG LOG - GameActor {gameId} logged events.", this.GetPrimaryKeyString());
    }

    /// <summary>
    /// Checks whether fire event should happen.
    /// </summary>
    /// <returns><b>True</b> if a fire event happened, <b>false</b> otherwise.</returns>
    private async Task<bool> CheckForFireEvent()
    {
        if (_gameActorState.State.PlayerStates.Any() && _gameActorState.State.PlayerStates.All(p => p.Value.IsReady))
        {
            _gameActorState.State.Turn++;
            _gameActorState.State.TurnTimeStamp = DateTime.UtcNow;

            _logger.LogInformation("All players in game {gameId} are ready. Incrementing turn to {turn} and performing fire event.", this.GetPrimaryKeyString(), _gameActorState.State.Turn);

            // Clear tagged state before firing tagging weapons
            await ClearUnitPenalties(false, true);
            _logger.LogInformation("Firing all tagging weapons in game {gameId}.", this.GetPrimaryKeyString());
            var tagDamageReports = await ProcessFireEvent(true);

            // Apply effects from this turn's damage reports for tagging attacks
            await ModifyGameStateBasedOnNarcAndTag(tagDamageReports);

            // Fire non-tagging weapons
            _logger.LogInformation("Firing all non-tagging weapons in game {gameId}.", this.GetPrimaryKeyString());
            var damageReports = await ProcessFireEvent(false);

            // Apply effects from this turns damage reports for narc attacks
            await ModifyGameStateBasedOnNarcAndTag(damageReports);

            // Apply all effects and heat from this turn's damage reports
            await ClearUnitPenalties(true, false);
            await ModifyGameStateBasedOnDamageReports(tagDamageReports.Concat(damageReports).ToList());

            await DistributeDamageReportsToPlayers(_gameActorState.State.DamageReports.GetReportsForTurn(_gameActorState.State.Turn));

            // Unmark ready in local memory and for the actors themselves
            foreach (var playerState in _gameActorState.State.PlayerStates.Values)
            {
                playerState.IsReady = false;
                playerState.TimeStamp = DateTime.UtcNow;
                GrainFactory.GetGrain<IPlayerActor>(playerState.PlayerId).UnReady().Ignore(); // Must be ignored because this may come through the player actor call chain
            }

            // Log turns to permanent store
            await _loggingServiceClient.LogGameAction(DateTime.UtcNow, this.GetPrimaryKeyString(), GameActionType.Turn, _gameActorState.State.Turn);

            return true;
        }

        return false;
    }

    private async Task<List<DamageReport>> ProcessFireEvent(bool processOnlyTags)
    {
        var damageReports = new List<DamageReport>();

        foreach (var unitId in _gameActorState.State.PlayerStates.SelectMany(p => p.Value.UnitEntries).Select(u => u.Id))
        {
            var unitActor = GrainFactory.GetGrain<IUnitActor>(unitId);
            var unit = await unitActor.GetUnit();

            // Do not fire at an unit which is not in the game
            if (unit.FiringSolution.TargetUnit != Guid.Empty && await IsUnitInGame(unit.FiringSolution.TargetUnit))
            {
                damageReports.AddRange(await unitActor.ProcessFireEvent(_gameActorState.State.Options, processOnlyTags));
            }
            else
            {
                _logger.LogWarning("Player {playerId} unit {unitId} tried to fire at an unit {targetUnitId} which does not exist or is not in the same game.", this.GetPrimaryKeyString(), unit.Id, unit.FiringSolution.TargetUnit);
            }
        }

        // Mark that the damage reports happened during this turn
        damageReports.ForEach(d => d.Turn = _gameActorState.State.Turn);

        _gameActorState.State.DamageReports.AddRange(damageReports);

        return damageReports;
    }

    private async Task ModifyGameStateBasedOnNarcAndTag(List<DamageReport> damageReports)
    {
        // Comb through damage reports to find incoming heat damage and EMP damage effects
        foreach (var damageReport in damageReports)
        {
            var altered = false;
            var narcToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Narc);
            var tagToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Tag);

            var unitEntry = GetUnit(damageReport.TargetUnitId);

            if (narcToTarget > 0)
            {
                unitEntry.Narced = true;
                altered = true;
            }

            if (tagToTarget > 0)
            {
                unitEntry.Tagged = true;
                altered = true;
            }

            if (altered)
            {
                var unitActor = GrainFactory.GetGrain<IUnitActor>(unitEntry.Id);
                await unitActor.SendState(unitEntry);
            }
        }
    }

    private async Task ModifyGameStateBasedOnDamageReports(List<DamageReport> damageReports)
    {
        // Comb through damage reports to find incoming heat damage and EMP damage effects
        foreach (var damageReport in damageReports)
        {
            var heatToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Heat);
            var empToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Emp);

            var unitEntry = GetUnit(damageReport.TargetUnitId);

            unitEntry.Heat += heatToTarget;
            unitEntry.Penalty += empToTarget;
        }

        // Heat sinks for all units operate after damage resolution
        foreach (var (_, playerState) in _gameActorState.State.PlayerStates)
        {
            foreach (var unitEntry in playerState.UnitEntries)
            {
                if (unitEntry.IsHeatTracking())
                {
                    ProcessUnitHeat(damageReports, unitEntry);
                }
                else
                {
                    unitEntry.Heat = 0;
                }

                unitEntry.TimeStamp = _gameActorState.State.TurnTimeStamp;
                var unitActor = GrainFactory.GetGrain<IUnitActor>(unitEntry.Id);
                await unitActor.SendState(unitEntry);
            }
        }
    }

    private async Task ClearUnitPenalties(bool clearPenalty, bool clearTag)
    {
        foreach (var (_, playerState) in _gameActorState.State.PlayerStates)
        {
            foreach (var unitEntry in playerState.UnitEntries)
            {
                var altered = false;

                if (clearPenalty && unitEntry.Penalty != 0)
                {
                    unitEntry.Penalty = 0;
                    altered = true;
                }

                if (clearTag && unitEntry.Tagged)
                {
                    unitEntry.Tagged = false;
                    altered = true;
                }

                if (altered)
                {
                    var unitActor = GrainFactory.GetGrain<IUnitActor>(unitEntry.Id);
                    await unitActor.SendState(unitEntry);
                }
            }
        }
    }

    /// <summary>
    /// Gets a single unit in this game's state.
    /// </summary>
    /// <param name="unitId">The ID of the unit to get.</param>
    /// <exception cref="InvalidOperationException">Thrown when the unit cannot be found.</exception>
    /// <remarks>Should never fail, since the unit has just produced a <see cref="DamageReport"/> or has been part of one.</remarks>
    /// <returns>The unit with the given ID.</returns>
    private UnitEntry GetUnit(Guid unitId)
    {
        return _gameActorState.State.PlayerStates.Values.Single(p => p.UnitEntries.Exists(u => u.Id == unitId)).UnitEntries.Single(u => u.Id == unitId);
    }
}