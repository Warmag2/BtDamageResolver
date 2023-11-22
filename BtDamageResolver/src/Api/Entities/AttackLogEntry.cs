using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// The attack log entry.
/// </summary>
[Serializable]
public class AttackLogEntry
{
    /// <summary>
    /// The attack log entry type.
    /// </summary>
    public AttackLogEntryType Type { get; set; }

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
    /// The standard sting conversion for attack log entries.
    /// </summary>
    /// <returns>The string representation of this attack log entry.</returns>
    /// <exception cref="NotImplementedException">Thrown when unknown attack log type is encountered.</exception>
    public override string ToString()
    {
        return Type switch
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
    }
}