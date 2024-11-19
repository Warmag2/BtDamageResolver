using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// A special damage type.
/// </summary>
[Serializable]
public enum SpecialDamageType
{
    /// <summary>
    /// No effect.
    /// </summary>
    None = 0,

    /// <summary>
    /// Special damage to infantry.
    /// </summary>
    Burst,

    /// <summary>
    /// Causes critical hits.
    /// </summary>
    Critical,

    /// <summary>
    /// Causes EMP effects.
    /// </summary>
    Emp,

    /// <summary>
    /// Adds heat.
    /// </summary>
    Heat,

    /// <summary>
    /// Does alternate damage to units which cannot take heat damage.
    /// </summary>
    HeatConverted,

    /// <summary>
    /// Causes motive hits.
    /// </summary>
    Motive,

    /// <summary>
    /// Attaches a NARC beacon.
    /// </summary>
    Narc,

    /// <summary>
    /// Tags target.
    /// </summary>
    Tag
}