namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Weapon classes.
/// </summary>
/// <remarks>
/// For capital ship weapon bay composition.
/// </remarks>
public enum WeaponClass
{
    /// <summary>
    /// No weapon class. Should never happen, data input error bait.
    /// </summary>
    None,

    /// <summary>
    /// Point defense weapon class. MGs, AMS, small lasers, etc.
    /// </summary>
    PointDefense,

    /// <summary>
    /// All normal lasers.
    /// </summary>
    Laser,

    /// <summary>
    /// All pulse lasers.
    /// </summary>
    PulseLaser,

    /// <summary>
    /// All PPCs.
    /// </summary>
    PPC,

    /// <summary>
    /// All normal and ultra autocannons.
    /// </summary>
    AutoCannon,

    /// <summary>
    /// All LBX Autocannons.
    /// </summary>
    LBX,

    /// <summary>
    /// All Hyper Assault Gauss rifles.
    /// </summary>
    HAG,

    /// <summary>
    /// All ATMs.
    /// </summary>
    ATM,

    /// <summary>
    /// All LRMs.
    /// </summary>
    LRM,

    /// <summary>
    /// All MMLs.
    /// </summary>
    MML,

    /// <summary>
    /// All MRMs.
    /// </summary>
    MRM,

    /// <summary>
    /// All SRMs.
    /// </summary>
    SRM,

    /// <summary>
    /// All Rocket Launchers.
    /// </summary>
    RocketLauncher,

    /// <summary>
    /// All capital missiles.
    /// </summary>
    CapitalMissile
}
