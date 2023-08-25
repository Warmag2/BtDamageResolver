using System;
using System.Linq;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Internal support methods for the game actor.
/// </summary>
public partial class GameActor
{
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

    /// <summary>
    /// Get the logic corresponding to an unit.
    /// </summary>
    /// <param name="unitEntry">The unit to create logic from.</param>
    /// <returns>The <see cref="ILogicUnit"/> object containing the unit logic of the unit represented by this <see cref="UnitEntry"/>.</returns>
    private ILogicUnit GetUnitLogic(UnitEntry unitEntry)
    {
        return _logicUnitFactory.CreateFrom(_gameActorState.State.Options, unitEntry);
    }

    /// <summary>
    /// Get the logic corresponding to an unit.
    /// </summary>
    /// <param name="unitId">The ID of the unit to create the unit logic from.</param>
    /// <returns>The <see cref="ILogicUnit"/> object containing the unit logic of the unit represented by this <see cref="unitId"/>.</returns>
    private ILogicUnit GetUnitLogic(Guid unitId)
    {
        return _logicUnitFactory.CreateFrom(_gameActorState.State.Options, GetUnit(unitId));
    }
}