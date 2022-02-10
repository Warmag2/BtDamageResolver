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
        Melee,
        MeleeCharge,
        MeleeDfa,
        IndirectFire,
        Pulse,
        Rapid,
        Streak
    }
}