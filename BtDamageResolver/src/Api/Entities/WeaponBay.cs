using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A weapon bay containing multiple weapons with individual targeting.
/// </summary>
[Serializable]
public class WeaponBay : WeaponBayReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponBay"/> class.
    /// </summary>
    public WeaponBay()
    {
        FiringSolution = new FiringSolution();
        Id = Guid.NewGuid();
        Name = Arc.Default.ToString();
        Weapons = new List<WeaponEntry>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponBay"/> class.
    /// </summary>
    /// <param name="weaponBayReference">The weapon reference to create this weapon entry from.</param>
    /// <remarks>
    /// Randomizes ID when created.
    /// </remarks>
    public WeaponBay(WeaponBayReference weaponBayReference)
    {
        Id = Guid.NewGuid();
        Name = weaponBayReference.Name;
        TimeStamp = DateTime.UtcNow;
        Weapons = weaponBayReference.Weapons.Select(w => new WeaponEntry(w)).ToList();
    }

    /// <summary>
    /// The firing solution for this bay.
    /// </summary>
    public FiringSolution FiringSolution { get; set; }

    /// <summary>
    /// The ID of this weapon entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The timestamp when this weapon bay was last updated.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// The weapons in this bay.
    /// </summary>
    public new List<WeaponEntry> Weapons { get; set; }

    /// <summary>
    /// Makes a copy of this weapon entry.
    /// </summary>
    /// <returns>A copy of the weapon entry.</returns>
    public new WeaponBay Copy()
    {
        return new WeaponBay
        {
            FiringSolution = FiringSolution.Copy(),
            Name = Name,
            TimeStamp = DateTime.UtcNow,
            Weapons = Weapons.Select(w => w.Copy()).ToList()
        };
    }
}