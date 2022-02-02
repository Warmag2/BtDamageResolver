using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    [Serializable]
    public class Ammo : NamedEntity
    {
        /// <summary>
        /// How big of a cluster bonus the ammo bestows.
        /// </summary>
        public Dictionary<RangeBracket, int> ClusterBonus { get; set; }

        /// <summary>
        /// The damage value of a single cluster instance inflicted by this ammo.
        /// </summary>
        public int? ClusterDamage { get; set; }

        /// <summary>
        /// How many cluster instances are dealt at a time with this ammo.
        /// </summary>
        public int? ClusterSize { get; set; }

        /// <summary>
        /// The damage array for ground units.
        /// </summary>
        public Dictionary<RangeBracket, int> Damage { get; set; }

        /// <summary>
        /// The damage array for aerospace units.
        /// </summary>
        public Dictionary<RangeBracket, int> DamageAerospace { get; set; }

        /// <summary>
        /// The amount of heat produced by firing.
        /// </summary>
        public int? Heat { get; set; }

        /// <summary>
        /// Hit modifier for this weapon, per weapon use mode.
        /// </summary>
        public int? HitModifier { get; set; }

        /// <summary>
        /// Dictionary of range bracket values.
        /// </summary>
        public Dictionary<RangeBracket, int> Range { get; set; }

        /// <summary>
        /// Maximum RangeBracket of weapon in aerospace combat.
        /// </summary>
        public RangeBracket? RangeAerospace { get; set; }

        /// <summary>
        /// Minimum range for weapon, per-mode.
        /// </summary>
        public int? RangeMinimum { get; set; }

        /// <summary>
        /// Special damage type of weapon, per-mode.
        /// </summary>
        public SpecialDamageEntry SpecialDamage { get; set; }

        /// <summary>
        /// Special features of the weapon in each fire mode.
        /// </summary>
        public List<WeaponFeatureEntry> SpecialFeatures { get; set; }
    }
}
