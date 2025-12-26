using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Extensions;
using static Faemiyah.BtDamageResolver.Api.Extensions.CollectionExtensions;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// Entity for tracking spent attack resources like heat and ammo.
/// </summary>
public class Consumables
{
    /// <summary>
    /// The ammo spent.
    /// </summary>
    public Dictionary<string, int> AmmoUsage { get; set; } = new();

    /// <summary>
    /// Heat generated.
    /// </summary>
    public int Heat { get; set; }

    /// <summary>
    /// Amount of targeting difficulty -inducing effects for this unit (for now, only EMP).
    /// </summary>
    public int Penalty { get; set; }

    public Consumables Copy()
    {
        return new Consumables
        {
            Heat = Heat,
            Penalty = Penalty,
            AmmoUsage = AmmoUsage.Copy()
        };
    }

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
