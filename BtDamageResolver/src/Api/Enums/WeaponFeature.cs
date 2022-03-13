using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// A weapon feature type.
    /// </summary>
    [Serializable]
    public enum WeaponFeature
    {
        None,
        AmsImmune,
        ArmorPiercing,
        Burst,
        Cluster,
        Flak,
        Heat,
        Homing,         // Can home to TAG and NARC
        IndirectFire,
        Melee,
        MeleeCharge,
        MeleeDfa,
        Pulse,
        Rapid,
        Streak
    }
}