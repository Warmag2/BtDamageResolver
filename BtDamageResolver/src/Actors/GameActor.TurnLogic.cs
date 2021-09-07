using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Interfaces.Extensions;
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

            DistributeTargetNumberUpdatesToPlayers(targetNumberUpdates);
            DistributeGameStateToPlayers();
            
            await _gameActorState.WriteStateAsync();

            // Log turns to permanent store
            await _loggingServiceClient.LogGameAction(DateTime.UtcNow, this.GetPrimaryKeyString(), GameActionType.Update, 1);
            // Remark game existence 
            await GrainFactory.GetGameEntryRepository().AddOrUpdate(new GameEntry
            {
                Name = this.GetPrimaryKeyString(),
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
            if (_gameActorState.State.PlayerStates.All(p => p.Value.IsReady))
            {
                _gameActorStateEthereal.Turn++;
                _gameActorStateEthereal.TurnTimeStamp = DateTime.UtcNow;

                _logger.LogInformation("All players in game {gameId} are ready. Incrementing turn to {turn} and performing fire event.", this.GetPrimaryKeyString(), _gameActorStateEthereal.Turn);

                // Mark all units not ready
                foreach (var unit in _gameActorState.State.PlayerStates.Values.SelectMany(p => p.UnitEntries))
                {
                    unit.Ready = false;
                    unit.TimeStamp = _gameActorStateEthereal.TurnTimeStamp;
                }

                var damageReports = new List<DamageReport>();

                foreach (var unitId in _gameActorState.State.PlayerStates.SelectMany(p => p.Value.UnitEntries).Select(u => u.Id))
                {
                    var unitActor = GrainFactory.GetGrain<IUnitActor>(unitId);
                    var unit = await unitActor.GetUnitState();

                    // Do not fire at an unit which is not in the game
                    if (unit.FiringSolution.TargetUnit != Guid.Empty && await IsUnitInGameInternal(unit.FiringSolution.TargetUnit))
                    {
                        damageReports.AddRange(await unitActor.Fire(_gameActorState.State.Options));
                    }
                    else
                    {
                        _logger.LogWarning("Player {playerId} unit {unitId} tried to fire at an unit {targetUnitId} which does not exist or is not in the same game.", this.GetPrimaryKeyString(), unit.Id, unit.FiringSolution.TargetUnit);
                    }
                }

                // Mark that the damage reports happened during this turn
                damageReports.ForEach(d => d.Turn = _gameActorStateEthereal.Turn);

                _gameActorStateEthereal.DamageReports.AddRange(damageReports);
                DistributeDamageReportsToPlayers(damageReports);

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
                await _loggingServiceClient.LogGameAction(DateTime.UtcNow, this.GetPrimaryKeyString(), GameActionType.Turn, _gameActorStateEthereal.Turn);
                // Remark game existence 
                await GrainFactory.GetGameEntryRepository().AddOrUpdate(new GameEntry { Name = this.GetPrimaryKeyString(), Players = _gameActorState.State.PlayerStates.Count, TimeStamp = DateTime.UtcNow });

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

            // Comb through damage reports to find heat and EMP damage effects
            foreach (var damageReport in damageReports)
            {
                GetUnit(damageReport.FiringUnitId).Heat += damageReport.AttackerHeat;
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
                        ProcessUnitHeat(unit);
                    }
                    else
                    {
                        unit.Heat = 0;
                    }

                    // Unit has been modified
                    unit.TimeStamp = DateTime.UtcNow;

                    // Upload this update to the unit itself
                    var unitActor = GrainFactory.GetGrain<IUnitActor>(unit.Id);
                    await unitActor.UpdateState(unit);
                }
            }
        }

        private void ProcessUnitHeat(UnitEntry unit)
        {
            switch (unit.MovementClass)
            {
                case MovementClass.Normal:
                    unit.Heat += 1;
                    break;
                case MovementClass.Fast:
                    unit.Heat += 2;
                    break;
                case MovementClass.Masc:
                    unit.Heat += 5;
                    break;
                case MovementClass.OutOfControl:
                    unit.Heat += 2;
                    break;
                case MovementClass.Jump:
                    unit.Heat += Math.Max(3, unit.Movement);
                    break;
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