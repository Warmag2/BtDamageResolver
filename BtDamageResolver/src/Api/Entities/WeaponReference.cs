using System;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A weapon entry portraying a weapon and its current settings.
/// </summary>
[Serializable]
public class WeaponReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponReference"/> class.
    /// </summary>
    public WeaponReference()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponReference"/> class.
    /// </summary>
    /// <param name="weaponEntry">The weapon entry to create this weapon reference from.</param>
    public WeaponReference(WeaponEntry weaponEntry)
    {
        Ammo = weaponEntry.Ammo;
        WeaponName = weaponEntry.WeaponName;
    }

    /// <summary>
    /// The name of the weapon this weapon entry refers to.
    /// </summary>
    public string WeaponName { get; set; }

    /// <summary>
    /// The name of the ammo to use.
    /// </summary>
    public string Ammo { get; set; }

    /// <summary>
    /// Makes a copy of this weapon entry.
    /// </summary>
    /// <returns>A copy of the weapon entry.</returns>
    public WeaponReference Copy()
    {
        return new WeaponEntry
        {
            Ammo = Ammo,
            WeaponName = WeaponName
        };
    }
}