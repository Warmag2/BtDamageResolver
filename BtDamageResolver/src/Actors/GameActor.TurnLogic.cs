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

namespace Faemiyah.BtDamageResolver.Actors
{
    /// <summary>
    /// The turn advancement logic for the game actor.
    /// </summary>
    public partial class GameActor
    {
        private async Task CheckGameStateUpdateEvents(List<Guid> updatedUnits = null)
        {
            // Don't check against the player timestamp. If we received new data, then this actor state has updated by definition
            _gameActorState.State.TimeStamp = DateTime.UtcNow;

            CheckForPlayerCountEvents();

            var fireEventHappened = await CheckForFireEvent();

            var targetNumberUpdates = fireEventHappened || updatedUnits == null
                ? await ProcessTargetNumberUpdatesForUnits()
                : await ProcessTargetNumberUpdatesForUnits(updatedUnits);

            await DistributeTargetNumberUpdatesToPlayers(targetNumberUpdates);
            await DistributeGameStateToPlayers();
            
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

                // Mark all units not ready
                foreach (var unit in _gameActorState.State.PlayerStates.Values.SelectMany(p => p.UnitEntries))
                {
                    unit.Ready = false;
                    unit.TimeStamp = _gameActorState.State.TurnTimeStamp;
                }

                var damageReports = new List<DamageReport>();

                foreach (var unitId in _gameActorState.State.PlayerStates.SelectMany(p => p.Value.UnitEntries).Select(u => u.Id))
                {
                    var unitActor = GrainFactory.GetGrain<IUnitActor>(unitId);
                    var unit = await unitActor.GetUnit();

                    // Do not fire at an unit which is not in the game
                    if (unit.FiringSolution.TargetUnit != Guid.Empty && await IsUnitInGame(unit.FiringSolution.TargetUnit))
                    {
                        damageReports.AddRange(await unitActor.ProcessFireEvent(_gameActorState.State.Options));
                    }
                    else
                    {
                        _logger.LogWarning("Player {playerId} unit {unitId} tried to fire at an unit {targetUnitId} which does not exist or is not in the same game.", this.GetPrimaryKeyString(), unit.Id, unit.FiringSolution.TargetUnit);
                    }
                }

                // Mark that the damage reports happened during this turn
                damageReports.ForEach(d => d.Turn = _gameActorState.State.Turn);

                _gameActorState.State.DamageReports.AddRange(damageReports);
                await DistributeDamageReportsToPlayers(damageReports);

                // Unmark ready in local memory and for the actors themselves
                foreach (var playerState in _gameActorState.State.PlayerStates.Values)
                {
                    playerState.IsReady = false;
                    playerState.TimeStamp = DateTime.UtcNow;
                    await GrainFactory.GetGrain<IPlayerActor>(playerState.PlayerId).UnReady();
                }

                // Apply effects from this turn's damage reports
                await ModifyGameStateBasedOnDamageReports(damageReports);

                // Log turns to permanent store
                await _loggingServiceClient.LogGameAction(DateTime.UtcNow, this.GetPrimaryKeyString(), GameActionType.Turn, _gameActorState.State.Turn);

                return true;
            }

            return false;
        }

        private async Task ModifyGameStateBasedOnDamageReports(List<DamageReport> damageReports)
        {
            // EMP/other firing difficulty effects for all units are reset after each turn
            foreach (var (_, playerState) in _gameActorState.State.PlayerStates)
            {
                foreach (var unit in playerState.UnitEntries)
                {
                    unit.Penalty = 0;
                }
            }

            // Comb through damage reports to find incoming heat damage and EMP damage effects
            foreach (var damageReport in damageReports)
            {
                var heatToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Heat);
                var empToTarget = damageReport.DamagePaperDoll.GetTotalDamageOfType(SpecialDamageType.Emp);

                if (heatToTarget > 0 || empToTarget > 0)
                {
                    var unit = GetUnit(damageReport.TargetUnitId);
                    unit.Heat += heatToTarget;
                    unit.Penalty += empToTarget;
                }
            }

            // Heat sinks for all units operate after damage resolution
            foreach (var (_, playerState) in _gameActorState.State.PlayerStates)
            {
                foreach (var unit in playerState.UnitEntries)
                {
                    if (unit.IsHeatTracking())
                    {
                        ProcessUnitHeat(damageReports, unit);
                    }
                    else
                    {
                        unit.Heat = 0;
                    }

                    // Unit has been modified
                    unit.TimeStamp = DateTime.UtcNow;

                    // Upload this update to the unit itself
                    var unitActor = GrainFactory.GetGrain<IUnitActor>(unit.Id);
                    await unitActor.SendState(unit);
                }
            }
        }

        private void ProcessUnitHeat(List<DamageReport> damageReports, UnitEntry unit)
        {
            var heatGeneratedByThisUnit = damageReports.Where(d => d.FiringUnitId == unit.Id).Sum(damageReport => damageReport.AttackerHeat);

            switch (unit.MovementClass)
            {
                case MovementClass.Immobile:
                case MovementClass.Stationary:
                    heatGeneratedByThisUnit += 0;
                    break;
                case MovementClass.Normal:
                    heatGeneratedByThisUnit += 1;
                    break;
                case MovementClass.Fast:
                    heatGeneratedByThisUnit += 2;
                    break;
                case MovementClass.Masc:
                    heatGeneratedByThisUnit += 5;
                    break;
                case MovementClass.OutOfControl:
                    heatGeneratedByThisUnit += 2;
                    break;
                case MovementClass.Jump:
                    heatGeneratedByThisUnit += Math.Max(3, unit.Movement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Combat computer sinks 4 heat by itself
            if (unit.HasFeature(UnitFeature.CombatComputer))
            {
                heatGeneratedByThisUnit -= 4;
            }

            // Only apply heat if the generation is positive and the unit tracks heat.
            // Combat computer and other heat generation reductions never take the heat below 0.
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

        /// <summary>
        /// Gets a single unit in this game's state.
        /// </summary>
        /// <param name="unitId">The ID of the unit to get.</param>
        /// <exception cref="InvalidOperationException">Thrown when the unit cannot be found.</exception>
        /// <remarks>Should never fail, since the unit has just produced a <see cref="DamageReport"/> or has been part of one.</remarks>
        /// <returns>The unit with the given ID.</returns>
        private UnitEntry GetUnit(Guid unitId)
        {
            return _gameActorState.State.PlayerStates.Values.Single(p => p.UnitEntries.Any(u => u.Id == unitId)).UnitEntries.Single(u => u.Id == unitId);
        }
    }
}