using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// The damage paper doll for recording unit damage.
/// </summary>
[Serializable]
public class DamagePaperDoll
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DamagePaperDoll"/> class.
    /// </summary>
    /// <remarks>
    /// Blank constructor for serialization purposes.
    /// </remarks>
    public DamagePaperDoll()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DamagePaperDoll"/> class.
    /// </summary>
    /// <param name="basePaperDoll">The base paper doll to generate from.</param>
    public DamagePaperDoll(PaperDoll basePaperDoll)
    {
        PaperDoll = basePaperDoll;
    }

    /// <summary>
    /// The collection of normal damage entries.
    /// </summary>
    public Dictionary<Location, Dictionary<Guid, List<int>>> DamageCollection { get; set; } = [];

    /// <summary>
    /// The collection of critical damage entries.
    /// </summary>
    public Dictionary<Location, Dictionary<Guid, List<CriticalDamageEntry>>> DamageCollectionCritical { get; set; } = [];

    /// <summary>
    /// The collection of special damage entries.
    /// </summary>
    public Dictionary<Location, Dictionary<Guid, List<SpecialDamageEntry>>> DamageCollectionSpecial { get; set; } = [];

    /// <summary>
    /// The paperdoll.
    /// </summary>
    public PaperDoll PaperDoll { get; set; }

    /// <summary>
    /// The paperdoll type.
    /// </summary>
    public PaperDollType Type => PaperDoll.Type;

    /// <summary>
    /// Get all locations for this damage report.
    /// </summary>
    /// <returns>The locations for this damage report.</returns>
    public List<Location> Locations => [.. DamageCollection.Keys];

    /// <summary>
    /// Gets the total damage of a specific special damage type.
    /// </summary>
    /// <param name="type">The type of special damage to fetch.</param>
    /// <returns>The total damage of a specific special damage type.</returns>
    public int GetTotalDamageOfType(SpecialDamageType type)
    {
        var result = 0;

        foreach (var (_, value) in DamageCollectionSpecial)
        {
            result += value.SelectMany(r => r.Value).Where(l => l.Type == type).Sum(d => int.Parse(d.Data));
        }

        return result;
    }

    /// <summary>
    /// Merge this damage report with another damage report.
    /// </summary>
    /// <param name="damagePaperDoll">The damage report to merge with.</param>
    /// <exception cref="InvalidOperationException">Thrown when the damage report is for a different unit type.</exception>
    public void Merge(DamagePaperDoll damagePaperDoll)
    {
        if (Type != damagePaperDoll.Type)
        {
            throw new InvalidOperationException("Cannot merge two DamagePaperDolls of different types.");
        }

        DamageCollection.Merge(damagePaperDoll.DamageCollection);
        DamageCollectionCritical.Merge(damagePaperDoll.DamageCollectionCritical);
        DamageCollectionSpecial.Merge(damagePaperDoll.DamageCollectionSpecial);
    }

    /// <summary>
    /// Record damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="amount">The amount of damage to record.</param>
    public void RecordDamage(Location location, Guid creatorId, int amount)
    {
        DamageCollection.Insert(location, creatorId, [amount]);
    }

    /// <summary>
    /// Record critical damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="creatorId">The creator of the critical damage entry.</param>
    /// <param name="damage">The damage amount to record.</param>
    /// <param name="threatType">The type of the critical damage threat.</param>
    /// <param name="criticalDamageType">The type of the critical damage to record.</param>
    public void RecordCriticalDamage(Location location, Guid creatorId, int damage, CriticalThreatType threatType, CriticalDamageType criticalDamageType)
    {
        DamageCollectionCritical.Insert(location, creatorId, [new CriticalDamageEntry(damage, threatType, criticalDamageType)]);
    }

    /// <summary>
    /// Record critical damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="creatorId">The creator of the critical damage entry.</param>
    /// <param name="damage">The damage amount to record.</param>
    /// <param name="threatType">The type of the critical damage threat.</param>
    /// <param name="criticalDamageTypes">The list of critical damage types to record.</param>
    public void RecordCriticalDamage(Location location, Guid creatorId, int damage, CriticalThreatType threatType, List<CriticalDamageType> criticalDamageTypes)
    {
        foreach (var criticalDamageType in criticalDamageTypes)
        {
            DamageCollectionCritical.Insert(location, creatorId, [new CriticalDamageEntry(damage, threatType, criticalDamageType)]);
        }
    }

    /// <summary>
    /// Record special damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="specialDamageEntry">The special damage to record.</param>
    public void RecordSpecialDamage(Location location, Guid creatorId, SpecialDamageEntry specialDamageEntry)
    {
        DamageCollectionSpecial.Insert(location, creatorId, [specialDamageEntry]);
    }
}