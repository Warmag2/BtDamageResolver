using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Weapon types.
/// </summary>
[Serializable]
public enum WeaponType
{
    /// <summary>
    /// No type.
    /// </summary>
    None,

    /// <summary>
    /// Ballistic weapon.
    /// </summary>
    Ballistic,

    /// <summary>
    /// Energy weapon.
    /// </summary>
    Energy,

    /// <summary>
    /// Melee weapon.
    /// </summary>
    Melee,

    /// <summary>
    /// Missile weapon.
    /// </summary>
    Missile
}