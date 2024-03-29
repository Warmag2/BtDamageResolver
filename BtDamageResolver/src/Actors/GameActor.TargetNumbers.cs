﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Game actor methods for target number calculation.
/// </summary>
public partial class GameActor
{
    /// <summary>
    /// Get all units which target this specific unit.
    /// </summary>
    /// <param name="unitId">The unit id whose targets should be processed.</param>
    /// <returns>A list of unit IDs which target this unit.</returns>
    private List<Guid> GetAllUnitsWhichTargetUnit(Guid unitId)
    {
        var targetingUnits = new List<Guid>();

        // Loop through all players
        foreach (var player in _gameActorState.State.PlayerStates.Select(p => p.Value))
        {
            // Select all units from this player which have the chosen unit as their target
            targetingUnits.AddRange(player.UnitEntries.Where(u => u.FiringSolution.TargetUnit == unitId).Select(u => u.Id).ToList());
        }

        return targetingUnits;
    }

    /// <summary>
    /// Update the target numbers for each of the given units.
    /// </summary>
    /// <param name="unitIds">The unit ids whose target numbers to calculate.</param>
    /// <returns>A list of target number update events corresponding for the new target numbers.</returns>
    private async Task<List<TargetNumberUpdate>> UpdateTargetNumbers(IEnumerable<Guid> unitIds)
    {
        var targetNumberUpdates = new List<TargetNumberUpdate>();

        foreach (var unitId in unitIds)
        {
            _logger.LogInformation("GameActor {gameId} is calculating updated target numbers for unit {unitId}.", this.GetPrimaryKeyString(), unitId);

            var unit = GetUnit(unitId);

            // Only fire at units which are in the game
            if (unit.FiringSolution.TargetUnit != Guid.Empty && await IsUnitInGame(unit.FiringSolution.TargetUnit))
            {
                targetNumberUpdates.Add(await ProcessUnitTargetNumbers(unit));
                _logger.LogInformation("GameActor succeeded in calculating target numbers for unit {unitId}.", unitId);
            }
            else
            {
                targetNumberUpdates.Add(await ProcessUnitTargetNumbers(unit, true));
                _logger.LogInformation("GameActor succeeded in setting blank target numbers for unit {unitId}.", unitId);

                if (unit.FiringSolution.TargetUnit != Guid.Empty)
                {
                    _logger.LogWarning(
                        "In Game {gameId}, unit {unitId} tried to calculate target numbers for target {targetUnitId} which does not exist or is not in the same game.",
                        this.GetPrimaryKeyString(),
                        unit.Id,
                        unit.FiringSolution.TargetUnit);
                }
            }
        }

        return targetNumberUpdates;
    }

    private async Task<List<TargetNumberUpdate>> ProcessTargetNumberUpdatesForUnits(List<Guid> unitIds = null)
    {
        var targetNumberUpdates = new List<TargetNumberUpdate>();
        var unitIdsToUpdate = new List<Guid>();

        if (unitIds != null)
        {
            unitIdsToUpdate.AddRange(unitIds);

            foreach (var unitId in unitIds)
            {
                // Also Update the target numbers for all units which target these units
                unitIdsToUpdate.AddRange(GetAllUnitsWhichTargetUnit(unitId));
            }

            targetNumberUpdates.AddRange(await UpdateTargetNumbers(unitIdsToUpdate.Distinct()));
        }
        else
        {
            targetNumberUpdates.AddRange(await UpdateTargetNumbers(_gameActorState.State.PlayerStates.Values.SelectMany(p => p.UnitEntries.Select(u => u.Id)).ToList()));
        }

        return targetNumberUpdates;
    }
}