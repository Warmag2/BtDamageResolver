using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class CriticalDamageEntry
    {
        /// <summary>
        /// Empty constructor for serialization.
        /// </summary>
        public CriticalDamageEntry()
        {
        }

        /// <summary>
        /// Constructor for CriticalDamageEntry.
        /// </summary>
        /// <param name="damage">The damage amount which induced the critical damage.</param>
        /// <param name="threatType">The critical threat type.</param>
        /// <param name="criticalDamageType">The critical damage type.</param>
        public CriticalDamageEntry(int damage, CriticalThreatType threatType, CriticalDamageType criticalDamageType)
        {
            InducingDamage = damage;
            ThreatType = threatType;
            Type = criticalDamageType;
        }

        public int InducingDamage { get; set; }

        public CriticalThreatType ThreatType { get; set; }

        public CriticalDamageType Type { get; set; }
    }
}