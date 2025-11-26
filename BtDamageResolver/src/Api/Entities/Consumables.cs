using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// Entity for tracking spent attack resources like heat and ammo.
/// </summary>
public class Consumables
{
    /// <summary>
    /// Heat generated.
    /// </summary>
    public int Heat { get; set; }

    /// <summary>
    /// The ammo spent.
    /// </summary>
    public Dictionary<string, int> AmmoUsage { get; set; } = new();

    public void Merge(Consumables input)
    {
        Heat += input.Heat;
        
        foreach (var (ammoType, ammoAmount) in input.AmmoUsage)
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
        AmmoUsage.Insert(ammoType, ammoAmount);
    }
}
