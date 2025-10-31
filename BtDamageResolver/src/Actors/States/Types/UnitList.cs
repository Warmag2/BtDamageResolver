using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors.States.Types;

/// <summary>
/// Data type for storing and transferring player unit lists.
/// Preserves order, does not accept duplicates.
/// </summary>
public class UnitList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitList"/> class.
    /// </summary>
    public UnitList()
    {
        UnitIds = [];
        UnitEntries = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitList"/> class.
    /// </summary>
    /// <param name="units">The list of units to construct from.</param>
    public UnitList(List<UnitEntry> units)
    {
        UnitIds = units.Select(u => u.Id).ToList();
        UnitEntries = units.ToDictionary(u => u.Id);
    }

    /// <summary>
    /// The unit ID list.
    /// </summary>
    public List<Guid> UnitIds { get; set; }

    /// <summary>
    /// The unit entires.
    /// </summary>
    public Dictionary<Guid, UnitEntry> UnitEntries { get; set; }

    /// <summary>
    /// Checks if the any of the units given are new or have been updated.
    /// </summary>
    /// <param name="units">The units to check.</param>
    /// <returns>List of IDs which are new or have been updated.</returns>
    public IReadOnlyCollection<Guid> AreNewOrNewer(List<UnitEntry> units)
    {
        return (from unit in units where IsNewOrNewer(unit) select unit.Id).ToList();
    }

    /// <summary>
    /// Get the ordered list of units represented by this UnitList.
    /// </summary>
    /// <returns>The list of units represented by this <see cref="UnitList"/>.</returns>
    public List<UnitEntry> ToList()
    {
        return UnitIds.Select(u => UnitEntries[u]).ToList();
    }

    /// <summary>
    /// Checks if the unit is new or has been updated.
    /// </summary>
    /// <param name="unit">The unit to check.</param>
    /// <returns><b>True</b> if the unit entry is newer than its instance in this list or if it does not exist in this list.</returns>
    private bool IsNewOrNewer(UnitEntry unit)
    {
        if (!UnitEntries.TryGetValue(unit.Id, out var value))
        {
            return true;
        }

        return value.TimeStamp < unit.TimeStamp;
    }
}