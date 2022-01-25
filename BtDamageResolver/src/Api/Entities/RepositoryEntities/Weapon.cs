using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    [Serializable]
    public class Weapon : NamedEntity
    {
        /// <summary>
        /// Attack type of the weapon. Basically normal or various melee attacks.
        /// </summary>
        public AttackType AttackType { get; set; }

        /// <summary>
        /// How big of a cluster bonus the weapon has, per mode.
        /// </summary>
        public Dictionary<WeaponMode, int> ClusterBonus { get; set; }

        /// <summary>
        /// The damage value of a single cluster instance.
        /// </summary>
        public int ClusterDamage { get; set; }

        /// <summary>
        /// How many cluster instances are dealt at a time with this weapon.
        /// </summary>
        public int ClusterSize { get; set; }

        /// <summary>
        /// The name of the cluster table this weapon references.
        /// </summary>
        public string ClusterTable { get; set; }

        /// <summary>
        /// The damage of the weapon. With cluster weapons, this signifies the damage row in the cluster table.
        /// </summary>
        public Dictionary<RangeBracket, int> Damage { get; set; }

        /// <summary>
        /// The aerospace damage of the weapon. Aerospace damage is fixed even for cluster weapons.
        /// </summary>
        public Dictionary<RangeBracket, int> DamageAerospace { get; set; }

        /// <summary>
        /// The amount of heat that this weapon produces when fired.
        /// </summary>
        public int Heat { get; set; }

        /// <summary>
        /// Hit modifier for this weapon, per weapon use mode.
        /// </summary>
        public Dictionary<WeaponMode, int> HitModifier { get; set; }

        /// <summary>
        /// Description of all fire modes of the weapon.
        /// </summary>
        public Dictionary<WeaponMode, string> ModeDescription { get; set; }

        /// <summary>
        /// Lists modes in which the weapon can be used. For now, either just "Normal" or "Normal" and "Special".
        /// </summary>
        public List<WeaponMode> Modes { get; set; }

        /// <summary>
        /// Dictionary of range bracket values.
        /// </summary>
        public Dictionary<RangeBracket, int> Range { get; set; }

        /// <summary>
        /// Maximum RangeBracket of weapon in aerospace combat.
        /// </summary>
        public RangeBracket RangeAerospace { get; set; }

        /// <summary>
        /// Minimum range for weapon, per-mode.
        /// </summary>
        public Dictionary<WeaponMode, int> RangeMinimum { get; set; }

        /// <summary>
        /// Special damage type of weapon, per-mode.
        /// </summary>
        public Dictionary<WeaponMode, SpecialDamageEntry> SpecialDamage { get; set; }

        /// <summary>
        /// Special features of the weapon in each fire mode.
        /// </summary>
        public Dictionary<WeaponMode, List<WeaponFeatureEntry>> SpecialFeatures { get; set; }

        /// <summary>
        /// Type of the weapon. Affects hit calculation and general weapon properties.
        /// </summary>
        public WeaponType Type { get; set; }

        /// <summary>
        /// Does this weapon expend ammunition or not.
        /// </summary>
        public bool UsesAmmo { get; set; }

        /// <summary>
        /// Informs what phase this weapon is used in.
        /// </summary>
        public Phase GetUsePhase()
        {
            switch (AttackType)
            {
                case AttackType.Normal:
                    return Phase.Weapon;
                case AttackType.Melee:
                    return Phase.Melee;
                case AttackType.Kick:
                    return Phase.Melee;
                case AttackType.Punch:
                    return Phase.Melee;
                default:
                    throw new NotImplementedException("Unknown weapon attack type encountered when trying to determine weapon use phase.");
            }
        }

        /// <summary>
        /// This method exists so that the user does not have to define all parameters
        /// for a weapon, and that creating the database is easier.
        /// </summary>
        public void FillMissingFields()
        {
            if (Modes == null)
            {
                Modes = new List<WeaponMode> {WeaponMode.Normal, WeaponMode.Special};
            }

            if (RangeMinimum == null)
            {
                RangeMinimum = Fill(Modes, -1);
            }

            if (HitModifier == null)
            {
                HitModifier = Fill(Modes, 0);
            }

            if (Damage.Count == 1)
            {
                Damage = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), Damage.Single().Value);
            }

            if (DamageAerospace.Count == 1)
            {
                DamageAerospace = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), DamageAerospace.Single().Value);
            }

            if (ClusterBonus == null)
            {
                ClusterBonus = Fill(Modes, 0);
            }

            if (SpecialDamage == null)
            {
                SpecialDamage = Fill(Modes, new SpecialDamageEntry {Data = "0", Type = SpecialDamageType.None});
            }

            if (SpecialFeatures == null)
            {
                SpecialFeatures = Fill(Modes, new List<WeaponFeatureEntry> { new WeaponFeatureEntry {Data = "0", Type = WeaponFeature.None } });
            }

            if (ModeDescription == null)
            {
                ModeDescription = Fill(Modes, "N/A");
            }

            if (ClusterTable == null)
            {
                ClusterTable = Constants.Names.DefaultClusterTableName;
            }
        }

        private Dictionary<TKey, TValue> Fill<TKey, TValue>(List<TKey> keys, TValue value)
        {
            return keys.ToDictionary(k => k, k => value);
        }
    }
}
