using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A critical damage entry.
/// </summary>
[Serializable]
public class CriticalDamageEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalDamageEntry"/> class.
    /// </summary>
    /// <remarks>
    /// Empty constructor for serialization purposes.
    /// </remarks>
    public CriticalDamageEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalDamageEntry"/> class.
    /// </summary>
    /// <param name="damage">The damage amount which induced the critical damage.</param>
    /// <param name="threatType">The critical threat type.</param>
    /// <param name="criticalDamageType">The critical damage type.</param>
    public CriticalDamageEntry(int damage, CriticalThreatType threatType, CriticalDamageType criticalDamageType)
    {
        InducingDamage = damage;
        ThreatType = threatType;
        Type = criticalDamageType;
    }

    /// <summary>
    /// The inducing damage amount.
    /// </summary>
    public int InducingDamage { get; set; }

    /// <summary>
    /// The inducing threat type.
    /// </summary>
    public CriticalThreatType ThreatType { get; set; }

    /// <summary>
    /// The critical damage type.
    /// </summary>
    public CriticalDamageType Type { get; set; }
}