using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Extensions;

/// <summary>
/// Extensions methods for paper dolls and damage paper dolls used in damage reports.
/// </summary>
public static class PaperDollExtensions
{
    /// <summary>
    /// Converts this paperdoll to a damage paper doll.
    /// </summary>
    /// <param name="paperDoll">The paper doll.</param>
    /// <returns>A damage paper doll based on this paper doll.</returns>
    public static Dictionary<Location, List<int>> ToDamagePaperDoll(this PaperDoll paperDoll)
    {
        var locationList = new List<Location>();

        foreach (var location in paperDoll.LocationMapping)
        {
            locationList.AddRange(location.Value);
        }

        return locationList.Distinct().ToDictionary(location => location, location => new List<int>());
    }
}