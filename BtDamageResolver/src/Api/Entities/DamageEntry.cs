using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A damage entry.
/// </summary>
[Serializable]
public class DamageEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DamageEntry"/> class.
    /// </summary>
    /// <remarks>
    /// Empty constructor for serialization purposes.
    /// </remarks>
    public DamageEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DamageEntry"/> class.
    /// </summary>
    /// <param name="damage">The damage amount which induced the critical damage.</param>
    /// <param name="location">The location the damage occurred in.</param>
    public DamageEntry(int damage, Location location)
    {
        DamageAmount = damage;
        Location = location;
        ThreatType = CriticalThreatType.Normal;
        CriticalType = CriticalDamageType.None;
        SpecialType = SpecialDamageType.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DamageEntry"/> class.
    /// </summary>
    /// <param name="damage">The damage amount which induced the critical damage.</param>
    /// <param name="location">The location the damage occurred in.</param>
    /// <param name="specialDamageType">The special damage type.</param>
    public DamageEntry(int damage, Location location, SpecialDamageType specialDamageType)
    {
        DamageAmount = damage;
        Location = location;
        ThreatType = CriticalThreatType.Normal;
        CriticalType = CriticalDamageType.None;
        SpecialType = specialDamageType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DamageEntry"/> class.
    /// </summary>
    /// <param name="damage">The damage amount which induced the critical damage.</param>
    /// <param name="location">The location the damage occurred in.</param>
    /// <param name="threatType">The critical threat type.</param>
    /// <param name="criticalDamageType">The critical damage type.</param>
    public DamageEntry(int damage, Location location, CriticalThreatType threatType, CriticalDamageType criticalDamageType)
    {
        DamageAmount = damage;
        Location = location;
        ThreatType = threatType;
        CriticalType = criticalDamageType;
        SpecialType = SpecialDamageType.None;
    }

    /// <summary>
    /// The damage amount or inducing damage amount for critical damage entries.
    /// </summary>
    public int DamageAmount { get; set; }

    /// <summary>
    /// The location the damage occurred.
    /// </summary>
    public Location Location { get; set; }

    /// <summary>
    /// The inducing threat type for critical damage.
    /// </summary>
    public CriticalThreatType ThreatType { get; set; }

    /// <summary>
    /// The critical damage type.
    /// </summary>
    public CriticalDamageType CriticalType { get; set; }

    /// <summary>
    /// The special damage type.
    /// </summary>
    public SpecialDamageType SpecialType { get; set; }

    /// <summary>
    /// Is this damage entry a critical damage entry?
    /// </summary>
    public bool IsCritical => CriticalType != CriticalDamageType.None;

    /// <summary>
    /// Is this damage entry a special damage entry?
    /// </summary>
    public bool IsSpecial => SpecialType != SpecialDamageType.None;

    /// <summary>
    /// Creates a list of damage entries for the given inducing damage, location, critical threat type, and critical damage types.
    /// </summary>
    /// <param name="inducingDamage">The damage amount which induced the critical damage.</param>
    /// <param name="location">The location the damage occurred in.</param>
    /// <param name="damageThreshold">The critical threat type.</param>
    /// <param name="criticalDamageTypes">The list of critical damage types.</param>
    /// <returns>A list of damage entries.</returns>
    public static List<DamageEntry> CreateList(int inducingDamage, Location location, CriticalThreatType damageThreshold, List<CriticalDamageType> criticalDamageTypes)
    {
        return criticalDamageTypes.Select(cdt => new DamageEntry(inducingDamage, location, damageThreshold, cdt)).ToList();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(true);
    }

    /// <summary>
    /// Print a comprehensive string representation of this damage entry, optionally including the location.
    /// </summary>
    /// <param name="includeLocation">Include location in the representation.</param>
    /// <returns>The string representation of the damage entry.</returns>
    public string ToString(bool includeLocation)
    {
        if (IsCritical)
        {
            switch (CriticalType)
            {
                case CriticalDamageType.Critical:
                    return "Critical hit";
                case CriticalDamageType.Immobilized:
                    return "Immobilized";
                case CriticalDamageType.HeavyMotive:
                    return "Heavy motive damage";
                case CriticalDamageType.LightMotive:
                    return "Light motive damage";
                case CriticalDamageType.ModerateMotive:
                    return "Moderate motive damage";
                case CriticalDamageType.LimbBlownOff:
                    return "Limb blown off";
                default:
                    switch (ThreatType)
                    {
                        case CriticalThreatType.DamageThreshold:
                            return $"Critical threat to: {CriticalType} (Threshold {DamageAmount})";
                        case CriticalThreatType.Normal:
                            return $"Critical damage to: {CriticalType} ({DamageAmount})";
                        default:
                            return $"Unknown critical threat type: {ThreatType}";
                    }
            }
        }
        else if (IsSpecial)
        {
            if (DamageAmount != 0)
            {
                return $"<b>{SpecialType}</b> &emdash; {DamageAmount}";
            }
            else
            {
                return $"<b>{SpecialType}</b>";
            }
        }
        else
        {
            return includeLocation ? $"<b>{Location}</b> &emdash; {DamageAmount}" : $"{DamageAmount}";
        }
    }
}