using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities
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

        private bool ValidateModeList<TDict>(Dictionary<WeaponMode, TDict> dict)
        {
            if (dict == null)
            {
                return false;
            }

            if (dict.Keys.Count != Modes.Count)
            {
                return false;
            }

            if (!dict.Keys.All(m => Modes.Contains(m)))
            {
                return false;
            }

            return true;
        }

        protected override void EntitySpecificValidate(EntityValidationResult validationResult)
        {
            if (Type == WeaponType.None)
            {
                validationResult.Disqualify("Weapon type not set.");
            }

            if (!Range.AndAllSpecifiedEnumValuesArePresent(new List<RangeBracket> { RangeBracket.PointBlank, RangeBracket.Short, RangeBracket.Medium, RangeBracket.Long, RangeBracket.Extreme }))
            {
                validationResult.Disqualify("Range bracket dictionary has not been specified or is invalid.");
            }

            if (!Damage.AndAllSpecifiedEnumValuesArePresent(new List<RangeBracket> { RangeBracket.PointBlank, RangeBracket.Short, RangeBracket.Medium, RangeBracket.Long, RangeBracket.Extreme }))
            {
                validationResult.Disqualify("Damage bracket dictionary has not been specified or is invalid.");
            }

            if (!DamageAerospace.AndAllSpecifiedEnumValuesArePresent(new List<RangeBracket> { RangeBracket.PointBlank, RangeBracket.Short, RangeBracket.Medium, RangeBracket.Long, RangeBracket.Extreme }))
            {
                validationResult.Disqualify("Aerospace damage bracket dictionary has not been specified or is invalid.");
            }

            if (Modes == null)
            {
                validationResult.Disqualify("List of weapon usage modes has not been specified.");
            }
            else
            {
                if (Modes.Count != 2)
                {
                    validationResult.Disqualify("List of weapon usage modes is invalid.");
                }

                if (!Modes.Contains(WeaponMode.Normal) || !Modes.Contains(WeaponMode.Special))
                {
                    validationResult.Disqualify("List of weapon usage modes is invalid.");
                }
            }

            if (!ValidateModeList(ClusterBonus))
            {
                validationResult.Disqualify("Cluster bonus list is null or invalid.");
            }

            if (!ValidateModeList(HitModifier))
            {
                validationResult.Disqualify("Hit modifier list is null or invalid.");
            }

            if (!ValidateModeList(ModeDescription))
            {
                validationResult.Disqualify("Weapon mode description list is null or invalid.");
            }

            if (!ValidateModeList(RangeMinimum))
            {
                validationResult.Disqualify("Minimum range list is null or invalid.");
            }

            if (!ValidateModeList(SpecialDamage))
            {
                validationResult.Disqualify("Weapon mode special damage list is null or invalid.");
            }

            if (!ValidateModeList(SpecialFeatures))
            {
                validationResult.Disqualify("Weapon mode special feature list is null or invalid.");
            }

            if (SpecialFeatures.Values.SelectMany(s => s).Any(s => s.Type == WeaponFeature.Cluster))
            {
                if (ClusterTable == null)
                {
                    validationResult.Disqualify($"Cluster table name must be specified for a weapon which has cluster features.");
                }

                if (ClusterSize == 0)
                {
                    validationResult.Disqualify($"Cluster size must be specified for a weapon which has cluster features.");
                }
            }
        }
    }
}
