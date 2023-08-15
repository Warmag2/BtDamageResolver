using System;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;

/// <summary>
/// An entity which contains a cluster table for hit amount randomization.
/// </summary>
[Serializable]
public class ClusterTable : NamedEntity
{
    /// <summary>
    /// The actual cluster dable.
    /// </summary>
    public int[][] Table { get; set; }

    /// <summary>
    /// Gets a damage amount for a specific damage value and cluster roll.
    /// </summary>
    /// <param name="damageValue">The damage value.</param>
    /// <param name="clusterRoll">The cluster roll.</param>
    /// <returns>The damage for the given damage value and cluster roll.</returns>
    public int GetDamage(int damageValue, int clusterRoll)
    {
        clusterRoll = Math.Clamp(clusterRoll, 2, 12);

        return Table[clusterRoll][damageValue];
    }

    /// <summary>
    /// Gets a damage amount for a given damage value.
    /// </summary>
    /// <param name="damageValue">The damage value.</param>
    /// <returns>The damage amount for a given damage value.</returns>
    public int GetDamage(int damageValue)
    {
        return Table[0][damageValue];
    }
}