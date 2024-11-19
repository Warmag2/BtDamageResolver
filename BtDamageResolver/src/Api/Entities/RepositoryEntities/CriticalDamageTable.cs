using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;

/// <summary>
/// The critical damage table.
/// </summary>
[Serializable]
public class CriticalDamageTable : IEntity<string>
{
    /// <summary>
    /// The location of the hit.
    /// </summary>
    public Location Location { get; set; }

    /// <summary>
    /// The mapping for the critical damage, based on dice roll.
    /// </summary>
    public Dictionary<int, List<CriticalDamageType>> Mapping { get; set; }

    /// <summary>
    /// The type of the critical damage table.
    /// </summary>
    public CriticalDamageTableType Type { get; set; }

    /// <summary>
    /// The unit type for this critical damage table.
    /// </summary>
    public PaperDollType UnitType { get; set; }

    /// <summary>
    /// Get a critical damage table ID from its properties.
    /// </summary>
    /// <param name="paperDollType">The paper doll type.</param>
    /// <param name="criticalDamageTableType">The critical damage table type.</param>
    /// <param name="location">The location of the hit.</param>
    /// <returns>The ID of the critical damage table.</returns>
    public static string GetIdFromProperties(PaperDollType paperDollType, CriticalDamageTableType criticalDamageTableType, Location location)
    {
        return $"{paperDollType}_{criticalDamageTableType}_{location}";
    }

    /// <summary>
    /// Needed because the id is concatenated from several properties in this critical damage table.
    /// </summary>
    /// <returns>The ID of this specific critical damage table.</returns>
    public string GetId()
    {
        return GetIdFromProperties(UnitType, Type, Location);
    }

    /// <inheritdoc />
    public void SetId(string id)
    {
        throw new InvalidOperationException("You should never have to set a Critical Damage Table ID manually.");
    }
}