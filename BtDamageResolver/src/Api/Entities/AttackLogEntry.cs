using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// The attack log entry.
/// </summary>
[Serializable]
public class AttackLogEntry
{
    /// <summary>
    /// Parameterized constructor for attack log entry.
    /// </summary>
    /// <param name="type">The attack log entry type.</param>
    /// <param name="ownerId">The instigator of this log entry.</param>
    /// <param name="context">Entry context, if any.</param>
    /// <param name="number">Associated number, if any.</param>
    /// <param name="location">Associated location, if any.</param>
    public AttackLogEntry(AttackLogEntryType type, Guid ownerId, string context, int? number = null, Location? location = null)
    {
        Type = type;
        OwnerId = ownerId;
        Context = context;
        Number = number;
        Location = location;
    }

    /// <summary>
    /// Parameterized constructor for attack log entry.
    /// </summary>
    /// <param name="type">The attack log entry type.</param>
    /// <param name="ownerId">The instigator of this log entry.</param>
    /// <param name="number">Associated number, if any.</param>
    /// <param name="location">Associated location, if any.</param>
    public AttackLogEntry(AttackLogEntryType type, Guid ownerId, int? number = null, Location? location = null)
    {
        Type = type;
        OwnerId = ownerId;
        Number = number;
        Location = location;
    }

    /// <summary>
    /// The attack log entry type.
    /// </summary>
    public AttackLogEntryType Type { get; set; }

    /// <summary>
    /// The ID of the owner (or instigator) of this log entry.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The context string for this entry.
    /// </summary>
    public string Context { get; set; }

    /// <summary>
    /// The number relevant for this log entry, if any.
    /// </summary>
    public int? Number { get; set; }

    /// <summary>
    /// The location this log applies to, if any.
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// The standard string conversion for attack log entries.
    /// </summary>
    /// <returns>The string representation of this attack log entry.</returns>
    /// <exception cref="NotImplementedException">Thrown when unknown attack log type is encountered.</exception>
    public string ToString(bool printUnitNames, Dictionary<Guid, string> unitNames)
    {
        var stringWithoutUnitName = Type switch
        {
            AttackLogEntryType.Calculation => $"{Context} is {Number}.",
            AttackLogEntryType.Critical => Context == null ? $"Critical hit ({Number}) to {Location}." : $"Critical hit ({Number}) to {Location}, damage to {Context}.",
            AttackLogEntryType.Damage => $"{Number} damage to {Location}",
            AttackLogEntryType.DiceRoll => $"{Context} roll is {Number}.",
            AttackLogEntryType.Fire => $"{Context} fires.",
            AttackLogEntryType.FiringSolution => $"{Context} prepares to fire.",
            AttackLogEntryType.Heat => Number == 0 ? $"{Context} causes no heat." : $"{Context} causes {Number} heat.",
            AttackLogEntryType.Hit => $"{Context} hits.",
            AttackLogEntryType.Information => $"{Context}.",
            AttackLogEntryType.Miss => $"{Context} misses.",
            AttackLogEntryType.SpecialDamage => $"{Number} {Context} damage to {Location}",
            _ => throw new NotImplementedException($"Explanation for event type {Type} has not yet been implemented."),
        };

        if (printUnitNames && unitNames.TryGetValue(OwnerId, out var ownerName))
        {
            return $"{ownerName} — {stringWithoutUnitName}";
        }
        else
        {
            return stringWithoutUnitName;
        }
    }
}