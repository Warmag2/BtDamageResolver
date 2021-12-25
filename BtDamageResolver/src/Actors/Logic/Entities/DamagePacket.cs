using Faemiyah.BtDamageResolver.Api.Entities;
using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Entities
{
    /// <summary>
    /// A packet of damage which can be directly applied to a location.
    /// </summary>
    [Serializable]
    public class DamagePacket
    {
        /// <summary>
        /// Property-setting constructor.
        /// </summary>
        /// <param name="damageAmount">The amount of damage for this packet.</param>
        /// <param name="specialDamageEntry">Special damage for this packet, if any.</param>
        public DamagePacket(int damageAmount, List<SpecialDamageEntry> specialDamageEntries)
        {
            Damage = damageAmount;
            SpecialDamageEntries = specialDamageEntries;
        }

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        public DamagePacket()
        {
        }

        public int Damage { get; set; }

        public List<SpecialDamageEntry> SpecialDamageEntries { get; set; }
    }
}
