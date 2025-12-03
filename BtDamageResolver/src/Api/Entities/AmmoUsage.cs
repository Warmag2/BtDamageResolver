using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities;

public class AmmoUsage
{
    /// <summary>
    /// The ammo used.
    /// </summary>
    public Dictionary<string, int> AmmoDict { get; set; } = new();

    /// <summary>
    /// Combine two <see cref="AmmoUsage"/>  instances.
    /// </summary>
    /// <param name="input">The <see cref="AmmoUsage"/> instance to combine with.</param>
    public void Merge(AmmoUsage input)
    {
        foreach (var (ammoType, ammoAmount) in input.AmmoDict)
        {
            SpendAmmo(ammoType, ammoAmount);
        }
    }

    /// <summary>
    /// Spend ammo of given type and amount.
    /// </summary>
    /// <param name="ammoType">The type of ammo to spend.</param>
    /// <param name="ammoAmount">The amount of ammo to spend.</param>
    public void SpendAmmo(string ammoType, int ammoAmount)
    {
        AmmoDict.Insert(ammoType, ammoAmount);
    }
}
