using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
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

        var fireEventHappened = await CheckForFireEvent();

        // If fire event happened or when given an empty unit set, process updated target numbers for everything.
        // Otherwise, only for affected units.
        var targetNumberUpdates = fireEventHappened || updatedUnits == null
            ? await ProcessTargetNumberUpdatesForUnits()
            : await ProcessTargetNumberUpdatesForUnits(updatedUnits);

        await DistributeTargetNumberUpdatesToPlayers(targetNumberUpdates);
        await DistributeGameStateToPlayers(fireEventHappened);

        // Save game actor state
        await _gameActorState.WriteStateAsync();

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
            ClearUnitPenalties(false, true);
            _logger.LogInformation("Firing all tagging weapons in game {gameId}.", this.GetPrimaryKeyString());
            var tagDamageReports = await ProcessFireEvent(true);

            // Apply effects from this turn's damage reports for tagging attacks
            ModifyGameStateBasedOnNarcAndTag(tagDamageReports);

            // Fire non-tagging weapons
            _logger.LogInformation("Firing all non-tagging weapons in game {gameId}.", this.GetPrimaryKeyString());
            var damageReports = await ProcessFireEvent(false);

            // Apply effects from this turns damage reports for narc attacks
            ModifyGameStateBasedOnNarcAndTag(damageReports);

            // Apply all effects and heat from this turn's damage reports
            ClearUnitPenalties(true, false);
            ModifyGameStateBasedOnDamageReports(tagDamageReports.Concat(damageReports).ToList());

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

    private async Task<DamageReport> ProcessDamageInstance(DamageInstance damageInstance)
    {
        var logicUnit = GetUnitLogic(damageInstance.UnitId);

        return await logicUnit.ResolveDamageInstance(damageInstance, Phase.End, false);
    }

    private async Task<List<DamageReport>> ProcessFireEvent(bool processOnlyTags)
    {
        var damageReports = new List<DamageReport>();

        foreach (var unit in _gameActorState.State.PlayerStates.SelectMany(p => p.Value.UnitEntries))
        {
            // Do not fire at an unit which is not in the game
            if (unit.FiringSolution.TargetUnit != Guid.Empty && await IsUnitInGame(unit.FiringSolution.TargetUnit))
            {
                damageReports.AddRange(await ProcessUnitFireEvent(unit, processOnlyTags));
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

    private async Task<List<DamageReport>> ProcessUnitFireEvent(UnitEntry unit, bool processOnlyTags)
    {
        var logicUnitAttacker = GetUnitLogic(unit);
        var logicUnitDefender = GetUnitLogic(unit.FiringSolution.TargetUnit);

        return await logicUnitAttacker.ResolveCombat(logicUnitDefender, processOnlyTags);
    }

    /// <summary>
    /// Processes the target numbers for a given unit.
    /// </summary>
    /// <param name="unitEntry">The unit to calculate target numbers for.</param>
    /// <param name="setBlankNumbers">Should blank numbers be set.</param>
    /// <returns>A list of target number updates.</returns>
    private async Task<TargetNumberUpdate> ProcessUnitTargetNumbers(UnitEntry unitEntry, bool setBlankNumbers = false)
    {
        var targetNumberUpdate = new TargetNumberUpdate
        {
            AmmoEstimate = new Dictionary<string, double>(),
            AmmoWorstCase = new Dictionary<string, int>(),
            TargetNumbers = new Dictionary<Guid, TargetNumberUpdateSingleWeapon>(),
            TimeStamp = DateTime.UtcNow,
            UnitId = unitEntry.Id
        };

        var logicUnitAttacker = GetUnitLogic(unitEntry);
        ILogicUnit logicUnitDefender = null;

        if (!setBlankNumbers)
        {
            logicUnitDefender = GetUnitLogic(unitEntry.FiringSolution.TargetUnit);
        }

        foreach (var weaponEntry in unitEntry.Weapons.Where(w => w.State == WeaponState.Active))
        {
            if (!setBlankNumbers)
            {
                var attackLog = new AttackLog();

                var (targetNumber, _) = await logicUnitAttacker.ResolveHitModifier(attackLog, logicUnitDefender, weaponEntry);

                var (ammoEstimate, ammoMax) = await logicUnitAttacker.ProjectAmmo(targetNumber, weaponEntry);
                var (heatEstimate, heatMax) = await logicUnitAttacker.ProjectHeat(targetNumber, weaponEntry);

                var weaponAmmoCombinedString = string.IsNullOrEmpty(weaponEntry.Ammo) ? $"{weaponEntry.WeaponName}" : $"{weaponEntry.WeaponName} {weaponEntry.Ammo}";
                targetNumberUpdate.AmmoEstimate.AddIfNotZero(weaponAmmoCombinedString, ammoEstimate);
                targetNumberUpdate.AmmoWorstCase.AddIfNotZero(weaponAmmoCombinedString, ammoMax);

                if (logicUnitAttacker.IsHeatTracking())
                {
                    targetNumberUpdate.HeatEstimate += heatEstimate;
                    targetNumberUpdate.HeatWorstCase += heatMax;
                }

                targetNumberUpdate.TargetNumbers.Add(
                    weaponEntry.Id,
                    new TargetNumberUpdateSingleWeapon
                    {
                        CalculationLog = attackLog,
                        TargetNumber = targetNumber,
                    });
            }
            else
            {
                targetNumberUpdate.TargetNumbers.Add(
                    weaponEntry.Id,
                    new TargetNumberUpdateSingleWeapon
                    {
                        CalculationLog = new AttackLog(),
                        TargetNumber = LogicConstants.InvalidTargetNumber,
                    });
            }
        }

        if (logicUnitAttacker.IsHeatTracking())
        {
            var nonWeaponHeatDamageReport = await logicUnitAttacker.ResolveNonWeaponHeat();

            targetNumberUpdate.HeatEstimate += nonWeaponHeatDamageReport.AttackerHeat;
            targetNumberUpdate.HeatWorstCase += nonWeaponHeatDamageReport.AttackerHeat;
        }

        _logger.LogInformation("Unit {unitId} finished calculating new target number values.", unitEntry.Id);

        return targetNumberUpdate;
    }

    private void ModifyGameStateBasedOnNarcAndTag(List<DamageReport> damageReports)
    {
        // Comb through damage reports to find incoming heat damage and EMP damage effects
        foreach (var damageReport in damageReports)
        {
            var narcToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Narc);
            var tagToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Tag);

            var unitEntry = GetUnit(damageReport.TargetUnitId);

            if (narcToTarget > 0)
            {
                unitEntry.Narced = true;
            }

            if (tagToTarget > 0)
            {
                unitEntry.Tagged = true;
            }
        }
    }

    private void ModifyGameStateBasedOnDamageReports(List<DamageReport> damageReports)
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
            }
        }
    }

    private void ClearUnitPenalties(bool clearPenalty, bool clearTag)
    {
        foreach (var (_, playerState) in _gameActorState.State.PlayerStates)
        {
            foreach (var unitEntry in playerState.UnitEntries)
            {
                if (clearPenalty && unitEntry.Penalty != 0)
                {
                    unitEntry.Penalty = 0;
                }

                if (clearTag && unitEntry.Tagged)
                {
                    unitEntry.Tagged = false;
                }
            }
        }
    }
}