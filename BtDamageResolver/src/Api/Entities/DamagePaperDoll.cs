using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

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
        DamageCollection = [];
        DamageCollectionSpecial = [];
        DamageCollectionCritical = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DamagePaperDoll"/> class.
    /// </summary>
    /// <param name="basePaperDoll">The base paper doll to generate from.</param>
    public DamagePaperDoll(PaperDoll basePaperDoll)
    {
        DamageCollection = [];
        DamageCollectionSpecial = [];
        DamageCollectionCritical = [];
        PaperDoll = basePaperDoll;
    }

    /// <summary>
    /// The collection of normal damage entries.
    /// </summary>
    public Dictionary<Location, List<int>> DamageCollection { get; set; }

    /// <summary>
    /// The collection of critical damage entries.
    /// </summary>
    public Dictionary<Location, List<CriticalDamageEntry>> DamageCollectionCritical { get; set; }

    /// <summary>
    /// The collection of special damage entries.
    /// </summary>
    public Dictionary<Location, List<SpecialDamageEntry>> DamageCollectionSpecial { get; set; }

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
            result += value.Where(l => l.Type == type).Sum(d => int.Parse(d.Data));
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

        foreach (var entry in damagePaperDoll.DamageCollection)
        {
            InsertDamage(entry.Key, entry.Value);
        }

        foreach (var entry in damagePaperDoll.DamageCollectionSpecial)
        {
            InsertSpecialDamage(entry.Key, entry.Value);
        }

        foreach (var entry in damagePaperDoll.DamageCollectionCritical)
        {
            InsertCriticalDamage(entry.Key, entry.Value);
        }
    }

    /// <summary>
    /// Record damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="amount">The amount of damage to record.</param>
    public void RecordDamage(Location location, int amount)
    {
        InsertDamage(location, amount);
    }

    /// <summary>
    /// Record critical damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="damage">The damage amount to record.</param>
    /// <param name="threatType">The type of the critical damage threat.</param>
    /// <param name="criticalDamageType">The type of the critical damage to record.</param>
    public void RecordCriticalDamage(Location location, int damage, CriticalThreatType threatType, CriticalDamageType criticalDamageType)
    {
        InsertCriticalDamage(location, new CriticalDamageEntry(damage, threatType, criticalDamageType));
    }

    /// <summary>
    /// Record critical damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="damage">The damage amount to record.</param>
    /// <param name="threatType">The type of the critical damage threat.</param>
    /// <param name="criticalDamageTypes">The list of critical damage types to record.</param>
    public void RecordCriticalDamage(Location location, int damage, CriticalThreatType threatType, List<CriticalDamageType> criticalDamageTypes)
    {
        foreach (var criticalDamageType in criticalDamageTypes)
        {
            InsertCriticalDamage(location, new CriticalDamageEntry(damage, threatType, criticalDamageType));
        }
    }

    /// <summary>
    /// Record special damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="specialDamageEntry">The special damage to record.</param>
    public void RecordSpecialDamage(Location location, SpecialDamageEntry specialDamageEntry)
    {
        InsertSpecialDamage(location, specialDamageEntry);
    }

    /// <summary>
    /// Insert critical damage to location.
    /// </summary>
    /// <param name="location">The location to insert to.</param>
    /// <param name="entry">The critical damage entry to insert.</param>
    private void InsertCriticalDamage(Location location, CriticalDamageEntry entry)
    {
        InsertCriticalDamage(location, [entry]);
    }

    /// <summary>
    /// Insert critical damage to location.
    /// </summary>
    /// <param name="location">The location to insert to.</param>
    /// <param name="entries">The critical damage entries to insert.</param>
    private void InsertCriticalDamage(Location location, List<CriticalDamageEntry> entries)
    {
        if (DamageCollectionCritical.TryGetValue(location, out var value))
        {
            value.AddRange(entries);
        }
        else
        {
            DamageCollectionCritical.Add(location, entries);
        }
    }

    /// <summary>
    /// Insert damage to location.
    /// </summary>
    /// <param name="location">The location to insert to.</param>
    /// <param name="amount">The amount of damage to insert.</param>
    private void InsertDamage(Location location, int amount)
    {
        InsertDamage(location, [amount]);
    }

    /// <summary>
    /// Insert damage to location.
    /// </summary>
    /// <param name="location">The location to insert to.</param>
    /// <param name="amounts">A list of damage amounts to insert.</param>
    private void InsertDamage(Location location, List<int> amounts)
    {
        if (DamageCollection.TryGetValue(location, out var value))
        {
            value.AddRange(amounts);
        }
        else
        {
            DamageCollection.Add(location, amounts);
        }
    }

    /// <summary>
    /// Insert special damage to location.
    /// </summary>
    /// <param name="location">The location to insert to.</param>
    /// <param name="entry">The special damage entry to insert.</param>
    private void InsertSpecialDamage(Location location, SpecialDamageEntry entry)
    {
        InsertSpecialDamage(location, [entry]);
    }

    /// <summary>
    /// Insert special damage to location.
    /// </summary>
    /// <param name="location">The location to insert to.</param>
    /// <param name="entries">The special damage entries to insert.</param>
    private void InsertSpecialDamage(Location location, List<SpecialDamageEntry> entries)
    {
        if (DamageCollectionSpecial.TryGetValue(location, out var value))
        {
            value.AddRange(entries);
        }
        else
        {
            DamageCollectionSpecial.Add(location, entries);
        }
    }
}