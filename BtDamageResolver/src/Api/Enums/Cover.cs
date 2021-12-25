using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    [Serializable]
    public enum Cover
    {
        None,
        Lower,
        Left,
        Right,
        Upper,
        Light,      // For infantry, only protects from burst weapons
        Hardened,   // As above, but blocks 50% of damage
        Heavy       // As above, but blocks 75% of damage
    }
}
