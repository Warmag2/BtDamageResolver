using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;

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
    /// The weapons in this bay.
    /// </summary>
    public List<WeaponEntryReference> Weapons { get; set; }

    /// <summary>
    /// Makes a copy of this weapon bay reference.
    /// </summary>
    /// <returns>A copy of the weapon entry.</returns>
    public WeaponBayReference Copy()
    {
        return new WeaponBayReference
        {
            Weapons = Weapons.Select(w => w.Copy()).ToList()
        };
    }
}