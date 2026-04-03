using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A weapon bay containing multiple weapons with individual targeting.
/// </summary>
[Serializable]
public class WeaponBayReference : NamedEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponBayReference"/> class.
    /// </summary>
    public WeaponBayReference()
    {
        Arc = Arc.Default;
        Name = "All";
        Weapons = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponBayReference"/> class.
    /// </summary>
    /// <param name="weaponBay">The weapon bay to construct this weapon bay reference from.</param>
    public WeaponBayReference(WeaponBay weaponBay)
    {
        Name = weaponBay.Name;
        Weapons = weaponBay.Weapons.Select(w => new WeaponEntryReference(w)).ToList();
    }

    /// <summary>
    /// The arc the bay is in.
    /// </summary>
    public Arc Arc { get; set; }

    /// <summary>
    /// The weapons in this bay.
    /// </summary>
    public List<WeaponEntryReference> Weapons { get; set; }

    /// <summary>
    /// Makes a copy of this weapon bay reference.
    /// </summary>
    /// <returns>A copy of the weapon bay reference.</returns>
    public WeaponBayReference Copy()
    {
        return new WeaponBayReference
        {
            Weapons = Weapons.Select(w => w.Copy()).ToList()
        };
    }

    /// <summary>
    /// Gets the formal name of the weapon bay reference, which includes the arc and the name.
    /// </summary>
    /// <returns>The formal name of the weapon bay reference.</returns>
    public string GetFormalName() => Arc.ToString() + " " + Name;
}