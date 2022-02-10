using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    /// <summary>
    /// A weapon entity.
    /// </summary>
    [Serializable]
    public class Weapon : NamedEntity
    {
        /// <summary>
        /// Lists the ammo the weapon can use. Dictionary key is the display name and dictionary value is the name of the ammo entity.
        /// </summary>
        public Dictionary<string, string> Ammo { get; set; }

        /// <summary>
        /// The default ammo type, if the weapon has multiple varieties. Can be left null or empty if there are none.
        /// </summary>
        public string AmmoDefault { get; set; }

        /// <summary>
        /// Attack type of the weapon. Basically normal or various melee attacks.
        /// </summary>
        public AttackType AttackType { get; set; }

        /// <summary>
        /// How big of a cluster bonus the weapon has.
        /// </summary>
        public Dictionary<RangeBracket, int> ClusterBonus { get; set; }

        /// <summary>
        /// The damage value of a single cluster instance inflicted by this weapon.
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
        /// Hit modifier for this weapon.
        /// </summary>
        public int HitModifier { get; set; }

        /// <summary>
        /// Dictionary of range bracket values.
        /// </summary>
        public Dictionary<RangeBracket, int> Range { get; set; }

        /// <summary>
        /// Maximum RangeBracket of weapon in aerospace combat.
        /// </summary>
        public RangeBracket RangeAerospace { get; set; }

        /// <summary>
        /// Minimum range for weapon.
        /// </summary>
        public int RangeMinimum { get; set; }

        /// <summary>
        /// Special damage type of weapon.
        /// </summary>
        public SpecialDamageEntry SpecialDamage { get; set; }

        /// <summary>
        /// Special features of the weapon..
        /// </summary>
        public List<WeaponFeatureEntry> SpecialFeatures { get; set; }

        /// <summary>
        /// Type of the weapon. Affects hit calculation and general weapon properties.
        /// </summary>
        public WeaponType Type { get; set; }

        /// <summary>
        /// Does this weapon expend ammunition or not.
        /// </summary>
        public bool UsesAmmo { get; set; }

        /// <summary>
        /// Generates a new weapon with the given ammo type applied to it.
        /// </summary>
        /// <param name="ammo">The ammo to apply.</param>
        /// <returns>The weapon with the ammo applied.</returns>
        public Weapon ApplyAmmo(Ammo ammo)
        {
            var applyTarget = Copy();

            if (ammo.ClusterBonus != null)
            {
                applyTarget.ClusterBonus = ammo.ClusterBonus;
            }

            if (ammo.ClusterDamage != null)
            {
                applyTarget.ClusterDamage = ammo.ClusterDamage.Value;
            }

            if (ammo.ClusterSize != null)
            {
                applyTarget.ClusterSize = ammo.ClusterSize.Value;
            }

            if (ammo.Damage != null)
            {
                applyTarget.Damage = ammo.Damage;
            }

            if (ammo.DamageAerospace != null)
            {
                applyTarget.DamageAerospace = ammo.DamageAerospace;
            }

            if (ammo.Heat.HasValue)
            {
                applyTarget.Heat = ammo.Heat.Value;
            }

            if (ammo.HitModifier.HasValue)
            {
                applyTarget.HitModifier = ammo.HitModifier.Value;
            }

            if (ammo.Range != null)
            {
                applyTarget.Range = ammo.Range;
            }

            if (ammo.RangeAerospace.HasValue)
            {
                applyTarget.RangeAerospace = ammo.RangeAerospace.Value;
            }

            if (ammo.RangeMinimum.HasValue)
            {
                applyTarget.RangeMinimum = ammo.RangeMinimum.Value;
            }

            if (ammo.SpecialDamage != null)
            {
                applyTarget.SpecialDamage = ammo.SpecialDamage;
            }

            if (ammo.SpecialFeatures != null)
            {
                applyTarget.SpecialFeatures = ammo.SpecialFeatures;
            }

            return applyTarget;
        }

        /// <summary>
        /// This method exists so that the user does not have to define all parameters
        /// for a weapon, and that creating the database is easier.
        /// </summary>
        public void FillMissingFields()
        {
            if (Ammo == null)
            {
                Ammo = new Dictionary<string, string>();
            }

            if (ClusterBonus == null)
            {
                ClusterBonus = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), 0);
            }
            else if (ClusterBonus.Count == 1)
            {
                ClusterBonus = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), ClusterBonus.Single().Value);
            }

            if (ClusterTable == null)
            {
                ClusterTable = Constants.Names.DefaultClusterTableName;
            }

            if (Damage == null)
            {
                Damage = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), 0);
            }
            else if (Damage.Count == 1)
            {
                Damage = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), Damage.Single().Value);
            }

            if (DamageAerospace == null)
            {
                DamageAerospace = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), 0);
            }
            else if (DamageAerospace.Count == 1)
            {
                DamageAerospace = Fill(Enum.GetValues(typeof(RangeBracket)).Cast<RangeBracket>().ToList(), DamageAerospace.Single().Value);
            }

            if (SpecialDamage == null)
            {
                SpecialDamage = new SpecialDamageEntry { Data = "0", Type = SpecialDamageType.None };
            }

            if (SpecialFeatures == null)
            {
                SpecialFeatures = new List<WeaponFeatureEntry> { new WeaponFeatureEntry { Data = "0", Type = WeaponFeature.None } };
            }
        }

        /// <summary>
        /// Informs what phase this weapon is used in.
        /// </summary>
        /// <returns>The phase where this weapon is used in.</returns>
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

        private static Dictionary<TKey, TValue> Fill<TKey, TValue>(List<TKey> keys, TValue value)
        {
            return keys.ToDictionary(k => k, k => value);
        }

        /// <summary>
        /// Provides a shallow copy of a weapon.
        /// </summary>
        /// <returns>A shallow copy of this weapon entity.</returns>
        private Weapon Copy()
        {
            return new Weapon
            {
                Ammo = Ammo,
                AttackType = AttackType,
                ClusterBonus = ClusterBonus,
                ClusterDamage = ClusterDamage,
                ClusterSize = ClusterSize,
                ClusterTable = ClusterTable,
                Damage = Damage,
                DamageAerospace = DamageAerospace,
                Heat = Heat,
                HitModifier = HitModifier,
                Name = Name,
                Range = Range,
                RangeAerospace = RangeAerospace,
                RangeMinimum = RangeMinimum,
                SpecialDamage = SpecialDamage,
                SpecialFeatures = SpecialFeatures,
                Type = Type,
                UsesAmmo = UsesAmmo
            };
        }
    }
}
