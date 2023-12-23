using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Validation;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;

/// <summary>
/// A weapon entity.
/// </summary>
[Serializable]
public class Weapon : NamedEntity, IEntityWithRulesValidation
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
    /// Is the weapon capital scale.
    /// </summary>
    public bool CapitalScale { get; set; }

    /// <summary>
    /// Weapon class of the weapon. For categorizing weapons in capital ship weapon bays.
    /// </summary>
    public WeaponClass Class { get; set; }

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
    /// The amount of heat that this weapon produces when fired, per rangebracket.
    /// </summary>
    /// <remarks>
    /// Different values for brackets only for capital weapon bays.
    /// </remarks>
    public Dictionary<RangeBracket, int> Heat { get; set; }

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
    /// Special damage types of a weapon.
    /// </summary>
    public List<SpecialDamageEntry> SpecialDamage { get; set; }

    /// <summary>
    /// Special features of the weapon.
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
    /// Create a single weapon from a weapon bay.
    /// </summary>
    /// <param name="weapons">The weapons to list.</param>
    /// <returns>The merged weapon, and whether merging was successful.</returns>
    public static (bool Successful, Weapon WeaponBayWeapon) CreateWeaponBayWeapon(List<Weapon> weapons)
    {
        var weaponBayWeapon = weapons[0].Copy();

        for (int ii = 1; ii < weapons.Count; ii++)
        {
            if (!weaponBayWeapon.MergeIntoBay(weapons[ii]))
            {
                return (false, null);
            }
        }

        return (true, weaponBayWeapon);
    }

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

        if (ammo.Heat != null)
        {
            applyTarget.Heat = ammo.Heat;
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
        Ammo ??= new Dictionary<string, string>();

        ClusterBonus = ClusterBonus.Fill();

        ClusterTable ??= Constants.Names.DefaultClusterTableName;

        Damage = Damage.Fill();
        DamageAerospace = DamageAerospace.Fill(RangeAerospace);
        Heat = Heat.Fill(RangeAerospace);

        SpecialDamage ??= new();

        SpecialFeatures ??= new();
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
            case AttackType.Kick:
            case AttackType.Punch:
                return Phase.Melee;
            default:
                throw new NotImplementedException("Unknown weapon attack type encountered when trying to determine weapon use phase.");
        }
    }

    /// <summary>
    /// Provides a deep copy of a weapon.
    /// </summary>
    /// <returns>A deep copy of this weapon entity.</returns>
    public Weapon Copy()
    {
        return new Weapon
        {
            Ammo = Ammo.Copy(),
            AmmoDefault = AmmoDefault,
            AttackType = AttackType,
            ClusterBonus = ClusterBonus.Copy(),
            ClusterDamage = ClusterDamage,
            ClusterSize = ClusterSize,
            ClusterTable = ClusterTable,
            Damage = Damage.Copy(),
            DamageAerospace = DamageAerospace.Copy(),
            Heat = Heat,
            HitModifier = HitModifier,
            Name = Name,
            Range = Range.Copy(),
            RangeAerospace = RangeAerospace,
            RangeMinimum = RangeMinimum,
            SpecialDamage = SpecialDamage.Select(s => s.Copy()).ToList(),
            SpecialFeatures = SpecialFeatures.Select(s => s.Copy()).ToList(),
            Type = Type,
            UsesAmmo = UsesAmmo
        };
    }

    /// <summary>
    /// Does the given weapon have the given special damage entry.
    /// </summary>
    /// <param name="specialDamageType">A weapon feature type.</param>
    /// <param name="specialDamageEntry">The matching special damage entry, if any.</param>
    /// <returns><b>True</b> if the special damage was found, <b>false</b> otherwise.</returns>
    public bool HasSpecialDamage(SpecialDamageType specialDamageType, out SpecialDamageEntry specialDamageEntry)
    {
        specialDamageEntry = SpecialDamage.SingleOrDefault(w => w.Type == specialDamageType);

        return specialDamageEntry != null;
    }

    /// <summary>
    /// Does the given weapon have the given feature.
    /// </summary>
    /// <param name="feature">A weapon feature type.</param>
    /// <param name="weaponFeatureEntry">The matching weapon feature entry, if any.</param>
    /// <returns><b>True</b> if the weapon feature was found, <b>false</b> otherwise.</returns>
    public bool HasFeature(WeaponFeature feature, out WeaponFeatureEntry weaponFeatureEntry)
    {
        weaponFeatureEntry = SpecialFeatures.SingleOrDefault(w => w.Type == feature);

        return weaponFeatureEntry != null;
    }

    /// <summary>
    /// Merge values from another weapon to this aerospace bay weapon.
    /// </summary>
    /// <remarks>
    /// Should only be used for aerospace capital craft.
    /// </remarks>
    /// <param name="weapon">The weapon to merge with.</param>
    /// <returns><b>True</b> if the merge was successful, and <b>false</b> if it was not.</returns>
    public bool MergeIntoBay(Weapon weapon)
    {
        if (AttackType != weapon.AttackType)
        {
            return false;
        }

        if (CapitalScale != weapon.CapitalScale)
        {
            return false;
        }

        if (!ClusterBonus.DeepEquals(weapon.ClusterBonus))
        {
            return false;
        }

        if (ClusterDamage != weapon.ClusterDamage || ClusterSize != weapon.ClusterSize || ClusterTable != weapon.ClusterTable)
        {
            return false;
        }

        Damage.MergeAdditionally(weapon.Damage);
        DamageAerospace.MergeAdditionally(weapon.Damage);
        Heat.MergeAdditionally(weapon.Heat);

        if (HitModifier != weapon.HitModifier)
        {
            return false;
        }

        // Normal ranges cannot be merged efficiently and are meaningless for merged aerospace bays.
        // Minimum ranges cannot be merged efficiently and are meaningless for merged aerospace bays.
        // Merging aerospace ranges to the larger one is fine, as this will be only used by large aerospace units
        RangeAerospace = (RangeBracket)Math.Max((int)RangeAerospace, (int)weapon.RangeAerospace);

        // Special damage entries are merged additively if they match.
        if (SpecialDamage.Count != weapon.SpecialDamage.Count || !SpecialDamage.TrueForAll(s => weapon.SpecialDamage.Exists(f => f.Type == s.Type)))
        {
            return false;
        }

        MergeSpecialDamageEntries(weapon.SpecialDamage);

        if (!SpecialDamage.Equals(weapon.SpecialDamage))
        {
            return false;
        }

        // Tricky shit
        if (SpecialFeatures.Count != weapon.SpecialFeatures.Count || !SpecialFeatures.TrueForAll(s => weapon.SpecialFeatures.Exists(f => s.Equals(f))))
        {
            return false;
        }

        if (Type != weapon.Type)
        {
            return false;
        }

        if (UsesAmmo != weapon.UsesAmmo)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Multiply the effects of this weapon.
    /// </summary>
    /// <remarks>
    /// Should only be used for aerospace craft, but possible also otherwise.
    /// </remarks>
    /// <param name="amount">The number to multiply the weapon with.</param>
    /// <returns>The multiplied weapon.</returns>
    public Weapon Multiply(int amount)
    {
        var weapon = Copy();

        weapon.Damage.Multiply(amount);
        DamageAerospace.Multiply(amount);
        Heat.Multiply(amount);

        foreach (var specialDamageEntry in SpecialDamage)
        {
            var oneInstance = specialDamageEntry.Data;

            for (int ii = 2; ii <= amount; ii++)
            {
                // Quite horrible, but the only way to preserve all data
                specialDamageEntry.Data = $"{specialDamageEntry.Data} + {oneInstance}";
            }
        }

        return weapon;
    }

    /// <inheritdoc />
    public RulesValidationResult Validate()
    {
        // Make a new validation result and go over error cases one by one
        var validationResult = new RulesValidationResult();

        if (Type == WeaponType.None)
        {
            validationResult.Fail("Weapon has no type.");
        }

        return validationResult;
    }

    private void MergeSpecialDamageEntries(List<SpecialDamageEntry> input)
    {
        foreach (SpecialDamageEntry entry in SpecialDamage)
        {
            var similarType = input.SingleOrDefault(i => i.Type == entry.Type);

            if (similarType != null)
            {
                entry.Data = $"{entry.Data} + {similarType.Data}";
            }
        }
    }
}