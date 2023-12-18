using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Unit types.
/// </summary>
[Serializable]
public enum UnitType
{
    /// <summary>
    /// Buildings.
    /// </summary>
    Building,

    /// <summary>
    /// Capital aerospace craft.
    /// </summary>
    AerospaceCapital,

    /// <summary>
    /// Aerodyne dropships and small craft.
    /// </summary>
    AerospaceDropshipAerodyne,

    /// <summary>
    /// Spheroid dropships and small craft.
    /// </summary>
    AerospaceDropshipSpheroid,

    /// <summary>
    /// Aerospace fighters.
    /// </summary>
    AerospaceFighter,

    /// <summary>
    /// Battle armor.
    /// </summary>
    BattleArmor,

    /// <summary>
    /// Infantry.
    /// </summary>
    Infantry,

    /// <summary>
    /// Normal Mechs.
    /// </summary>
    Mech,

    /// <summary>
    /// Tripod mechs.
    /// </summary>
    MechTripod,

    /// <summary>
    /// Quad mechs.
    /// </summary>
    MechQuad,

    /// <summary>
    /// Hover vehicles.
    /// </summary>
    VehicleHover,

    /// <summary>
    /// Tracked vehicles.
    /// </summary>
    VehicleTracked,

    /// <summary>
    /// VTOL vehicles.
    /// </summary>
    VehicleVtol,

    /// <summary>
    /// Wheeled vehicles.
    /// </summary>
    VehicleWheeled
}