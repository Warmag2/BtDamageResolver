using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// A critical damage table type.
/// </summary>
[Serializable]
public enum CriticalDamageTableType
{
    /// <summary>
    /// No critical damage.
    /// </summary>
    None,

    /// <summary>
    /// Standard critical damage table.
    /// </summary>
    Critical,

    /// <summary>
    /// Motive hit table.
    /// </summary>
    Motive
}