using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// Stance represents the positioning of an unit from its own point of view. Applies mainly to mechs and infantry.
    /// </summary>
    [Serializable]
    public enum Stance
    {
        Normal,     // For all units
        Crouch,     // For mechs
        DugIn,      // For infantry, -2 to hit
        Prone,      // -1 to hit for infantry, various effects for mechs
        Light,      // For infantry, only protects from burst weapons
        Hardened,   // As above, but blocks 50% of damage
        Heavy       // As above, but blocks 75% of damage
    }
}