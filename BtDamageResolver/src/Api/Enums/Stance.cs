using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Stance represents the positioning of an unit from its own point of view. Applies mainly to mechs and infantry.
/// </summary>
[Serializable]
public enum Stance
{
    // For all units
    Normal,

    // For mechs
    Crouch,

    // For infantry, -2 to hit
    DugIn,

    // -1 to hit for infantry, various effects for mechs
    Prone,

    // For infantry, only gives no protection but provides a -1 to hit
    Light,

    // As above, but blocks 50% of damage
    Hardened,

    // As above, but blocks 75% of damage
    Heavy
}