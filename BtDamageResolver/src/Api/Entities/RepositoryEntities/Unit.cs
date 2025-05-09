using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;

/// <summary>
/// Contains the non-volatile data of an unit.
/// </summary>
[Serializable]
public class Unit : NamedEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Unit"/> class.
    /// </summary>
    public Unit()
    {
        WeaponBays = [new()];
    }

    /// <summary>
    /// The features this unit possesses.
    /// </summary>
    public HashSet<UnitFeature> Features { get; set; }

    /// <summary>
    /// How many jump jets does this unit have, if any.
    /// </summary>
    public int JumpJets { get; set; }

    /// <summary>
    /// The amount of heat this unit sinks per turn.
    /// </summary>
    public int Sinks { get; set; } = 10;

    /// <summary>
    /// The base ground or air speed of this unit, when moving at normal speed, without modifications, in units per turn.
    /// </summary>
    public int Speed { get; set; } = 4;

    /// <summary>
    /// The unit type of this unit.
    /// </summary>
    public UnitType Type { get; set; } = UnitType.Mech;

    /// <summary>
    /// The tonnage of this unit.
    /// </summary>
    public int Tonnage { get; set; } = 50;

    /// <summary>
    /// The number of individual acting troopers left in this battle armor or infantry unit.
    /// </summary>
    public int Troopers { get; set; } = 1;

    /// <summary>
    /// The weapons this unit has equipped, listed in bays/arcs.
    /// </summary>
    /// <remarks>
    /// Typically only a single bay with all weapons.
    /// </remarks>
    public List<WeaponBayReference> WeaponBays { get; set; }

    /// <summary>
    /// Can this unit mount a specific weapon.
    /// </summary>
    /// <param name="weaponEntry">The weapon entry.</param>
    /// <returns><b>True</b> if the unit can mount it. <b>False</b> otherwise.</returns>
    public bool CanMountWeapon(WeaponEntry weaponEntry)
    {
        return Type switch
        {
            UnitType.BattleArmor => weaponEntry.WeaponName.StartsWith(Constants.Names.BattleArmorWeaponPrefix),
            UnitType.Infantry => weaponEntry.WeaponName.StartsWith(Constants.Names.InfantryWeaponPrefix),
            _ => !weaponEntry.WeaponName.StartsWith(Constants.Names.BattleArmorWeaponPrefix) && !weaponEntry.WeaponName.StartsWith(Constants.Names.InfantryWeaponPrefix),
        };
    }

    /// <summary>
    /// Does this unit track heat.
    /// </summary>
    /// <returns>Is the unit a heat-tracking unit.</returns>
    public bool IsHeatTracking()
    {
        return Type switch
        {
            UnitType.AerospaceFighter or UnitType.Mech or UnitType.MechTripod or UnitType.MechQuad => true,
            _ => false,
        };
    }

    /// <summary>
    /// Check whether the unit has a specific feature or not.
    /// </summary>
    /// <param name="feature">The feature to check.</param>
    /// <returns><b>True</b> if the unit has the specified feature, <b>false</b> otherwise.</returns>
    public bool HasFeature(UnitFeature feature)
    {
        return Features.Contains(feature);
    }

    /// <summary>
    /// Add or remove a feature from an unit.
    /// </summary>
    /// <param name="feature">The feature to alter.</param>
    /// <param name="present">Should the feature be present or not.</param>
    public void SetFeature(UnitFeature feature, bool present)
    {
        Features.Set(feature, present);
    }
}