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
    private readonly Dictionary<Guid, UnitEntry> _unitEntries;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitList"/> class.
    /// </summary>
    public UnitList()
    {
        UnitIds = new List<Guid>();
        _unitEntries = new Dictionary<Guid, UnitEntry>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitList"/> class.
    /// </summary>
    /// <param name="units">The list of units to construct from.</param>
    public UnitList(List<UnitEntry> units)
    {
        _unitEntries = new Dictionary<Guid, UnitEntry>();

        UnitIds = units.Select(u => u.Id).ToList();
        foreach (var unit in units)
        {
            _unitEntries.Add(unit.Id, unit);
        }
    }

    /// <summary>
    /// The IDs of the entries in this <see cref="UnitList"/>.
    /// </summary>
    public List<Guid> UnitIds { get; }

    /// <summary>
    /// Checks if the any of the units given are new or have been updated.
    /// </summary>
    /// <param name="units">The units to check.</param>
    /// <returns>List of IDs which are new or have been updated.</returns>
    public List<Guid> AreNewOrNewer(List<UnitEntry> units)
    {
        return (from unit in units where IsNewOrNewer(unit) select unit.Id).ToList();
    }

    /// <summary>
    /// Adds an unit to the list.
    /// </summary>
    /// <param name="unitEntry">The unit to add.</param>
    public void Add(UnitEntry unitEntry)
    {
        _unitEntries.Add(unitEntry.Id, unitEntry);
        UnitIds.Add(unitEntry.Id);
    }

    /// <summary>
    /// Check whether the unit list contains a specific unit.
    /// </summary>
    /// <param name="unitId">The unit ID.</param>
    /// <returns><b>True</b> if the unit list contains the unit, <b>false</b> otherwise.</returns>
    public bool Contains(Guid unitId)
    {
        return _unitEntries.ContainsKey(unitId);
    }

    /// <summary>
    /// Removes an unit from the list.
    /// </summary>
    /// <param name="unitId">The ID of the unit to remove.</param>
    public void Remove(Guid unitId)
    {
        if (!_unitEntries.Remove(unitId))
        {
            throw new InvalidOperationException("Could not find the unit with ID {unitId} in this unit list.");
        }

        UnitIds.Remove(unitId);
    }

    /// <summary>
    /// Get the ordered list of units represented by this UnitList.
    /// </summary>
    /// <returns>The list of units represented by this <see cref="UnitList"/>.</returns>
    public List<UnitEntry> ToList()
    {
        return UnitIds.Select(u => _unitEntries[u]).ToList();
    }

    /// <summary>
    /// Checks if the unit is new or has been updated.
    /// </summary>
    /// <param name="unit">The unit to check.</param>
    /// <returns><b>True</b> if the unit entry is newer than its instance in this list or if it does not exist in this list.</returns>
    private bool IsNewOrNewer(UnitEntry unit)
    {
        if (!_unitEntries.TryGetValue(unit.Id, out var value))
        {
            return true;
        }

        return value.TimeStamp < unit.TimeStamp;
    }
}