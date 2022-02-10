using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
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
            switch (Type)
            {
                case AttackLogEntryType.Calculation:
                    return $"{Context} is {Number}.";
                case AttackLogEntryType.Critical:
                    return Context == null ? $"Critical hit ({Number}) to {Location}." : $"Critical hit ({Number}) to {Location}, damage to {Context}.";
                case AttackLogEntryType.Damage:
                    return $"{Number} damage to {Location}";
                case AttackLogEntryType.DiceRoll:
                    return $"{Context} roll is {Number}.";
                case AttackLogEntryType.Fire:
                    return $"{Context} fires.";
                case AttackLogEntryType.Heat:
                    return Number == 0 ? $"{Context} causes no heat." : $"{Context} causes {Number} heat.";
                case AttackLogEntryType.Hit:
                    return $"{Context} hits.";
                case AttackLogEntryType.Information:
                    return $"{Context}.";
                case AttackLogEntryType.Miss:
                    return $"{Context} misses.";
                case AttackLogEntryType.SpecialDamage:
                    return $"{Number} {Context} damage to {Location}";
                default:
                    throw new NotImplementedException($"Explanation for event type {Type} has not yet been implemented.");
            }
        }
    }
}