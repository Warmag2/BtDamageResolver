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
    public Dictionary<Guid, List<DamageEntry>> DamageCollection { get; set; } = [];

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
    public HashSet<Location> Locations => DamageCollection.SelectMany(d => d.Value).Select(v => v.Location).ToHashSet();

    /// <summary>
    /// Gets the total damage of a specific special damage type.
    /// </summary>
    /// <param name="type">The type of special damage to fetch.</param>
    /// <returns>The total damage of a specific special damage type.</returns>
    public int GetTotalDamageOfType(SpecialDamageType type)
    {
        return DamageCollection.SelectMany(d => d.Value).Where(l => l.SpecialType == type).Sum(d => d.DamageAmount);
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
    }

    /// <summary>
    /// Record damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="damageEntry">The damage entry to record.</param>
    public void RecordDamage(Guid creatorId, DamageEntry damageEntry)
    {
        DamageCollection.Insert(creatorId, damageEntry);
    }

    /// <summary>
    /// Record damage to this damage report.
    /// </summary>
    /// <param name="location">The location to record to.</param>
    /// <param name="damageEntry">The damage entry to record.</param>
    public void RecordDamage(Guid creatorId, List<DamageEntry> damageEntries)
    {
        DamageCollection.Insert(creatorId, damageEntries);
    }
}