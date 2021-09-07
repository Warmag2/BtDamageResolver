using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class AttackLogEntry
    {
        public AttackLogEntryType Type { get; set; }

        public string Context { get; set; }

        public int? Number { get; set; }

        public Location? Location { get; set; }

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