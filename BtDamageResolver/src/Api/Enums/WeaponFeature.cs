using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// A weapon feature type.
/// </summary>
[Serializable]
public enum WeaponFeature
{
    /// <summary>
    /// Missile weapon unaffected by AMS.
    /// </summary>
    AmsImmune,

    /// <summary>
    /// Weapon may cause crits through armor.
    /// </summary>
    ArmorPiercing,

    /// <summary>
    /// Weapon damage is clusterized.
    /// </summary>
    Cluster,

    /// <summary>
    /// Weapon has an easier time hitting aerial targets.
    /// </summary>
    Flak,

    /// <summary>
    /// Weapon can home to TAG.
    /// </summary>
    Homing,

    /// <summary>
    /// Weapon can be used in indirect fire.
    /// </summary>
    IndirectFire,

    /// <summary>
    /// Weapon does not suffer anattack penalty when fired from an evading unit.
    /// </summary>
    IgnoreOwnEvasion,

    /// <summary>
    /// Melee weapon.
    /// </summary>
    Melee,

    /// <summary>
    /// Melee charge attack.
    /// </summary>
    MeleeCharge,

    /// <summary>
    /// Melee DFA attack.
    /// </summary>
    MeleeDfa,

    /// <summary>
    /// Melee kick.
    /// </summary>
    MeleeKick,

    /// <summary>
    /// Pulse weapon.
    /// </summary>
    Pulse,

    /// <summary>
    /// Rapid-fire weapon.
    /// </summary>
    Rapid,

    /// <summary>
    /// Streak weapon.
    /// </summary>
    Streak,

    /// <summary>
    /// Targeting Computers may be used with this weapon.
    /// </summary>
    TargetingComputerValid,

    /// <summary>
    /// Targeting Computers help when performing aimed attacks with this weapon.
    /// </summary>
    TargetingComputerValidAimed,
}