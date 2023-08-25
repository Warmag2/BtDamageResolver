using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;

/// <summary>
/// Creates unit logic classes.
/// </summary>
public interface ILogicUnitFactory
{
    /// <summary>
    /// Create an unit logic from an unit.
    /// </summary>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="unit">The unit to create from.</param>
    /// <returns>An unit logic for the given unit.</returns>
    ILogicUnit CreateFrom(GameOptions gameOptions, UnitEntry unit);
}