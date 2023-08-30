using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A weapon entry portraying a weapon and its current settings.
/// </summary>
[Serializable]
public class WeaponEntry : WeaponReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponEntry"/> class.
    /// </summary>
    /// <remarks>
    /// Randomizes ID when created.
    /// </remarks>
    public WeaponEntry()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponEntry"/> class.
    /// </summary>
    /// <param name="weaponReference">The weapon reference to create this weapon entry from.</param>
    /// <remarks>
    /// Randomizes ID when created.
    /// </remarks>
    public WeaponEntry(WeaponReference weaponReference)
    {
        Ammo = weaponReference.Ammo;
        Id = Guid.NewGuid();
        State = WeaponState.Active;
        TimeStamp = DateTime.UtcNow;
        WeaponName = weaponReference.WeaponName;
    }

    /// <summary>
    /// The timestamp when this weapon entry was last updated.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// The ID of this weapon entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The state of the weapon.
    /// </summary>
    public WeaponState State { get; set; }

    /// <summary>
    /// Makes a copy of this weapon entry.
    /// </summary>
    /// <returns>A copy of the weapon entry.</returns>
    public new WeaponEntry Copy()
    {
        return new WeaponEntry
        {
            Ammo = Ammo,
            TimeStamp = DateTime.UtcNow,
            State = State,
            WeaponName = WeaponName
        };
    }

    /// <summary>
    /// Is this a battle armor weapon.
    /// </summary>
    /// <returns><b>True</b> if the weapon is a battle armor weapon, <b>false</b> otherwise.</returns>
    public bool IsBattleArmorWeapon()
    {
        return WeaponName.StartsWith("BA ");
    }

    /// <summary>
    /// Is this an infantry weapon.
    /// </summary>
    /// <returns><b>True</b> if the weapon is an infantry weapon, <b>false</b> otherwise.</returns>
    public bool IsInfantryWeapon()
    {
        return WeaponName.StartsWith("Infantry ");
    }
}