using System;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A weapon entry portraying a weapon and its current settings.
/// </summary>
[Serializable]
public class WeaponEntryReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponEntryReference"/> class.
    /// </summary>
    public WeaponEntryReference()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponEntryReference"/> class.
    /// </summary>
    /// <param name="weaponEntry">The weapon entry to create this weapon reference from.</param>
    public WeaponEntryReference(WeaponEntry weaponEntry)
    {
        Ammo = weaponEntry.Ammo;
        Amount = weaponEntry.Amount;
        Modifier = weaponEntry.Modifier;
        WeaponName = weaponEntry.WeaponName;
    }

    /// <summary>
    /// The name of the ammo to use.
    /// </summary>
    public string Ammo { get; set; }

    /// <summary>
    /// The number of copies of this weapon1.
    /// </summary>
    public int Amount { get; set; } = 1;

    /// <summary>
    /// Static attack modifier for this specific weapon entry.
    /// </summary>
    /// <remarks>
    /// Almost always 0. Exceptions are accurate/inaccurate weapon quirks and actuator damage.
    /// </remarks>
    public int Modifier { get; set; }

    /// <summary>
    /// The name of the weapon this weapon entry refers to.
    /// </summary>
    public string WeaponName { get; set; }

    /// <summary>
    /// Makes a copy of this weapon entry.
    /// </summary>
    /// <returns>A copy of the weapon entry.</returns>
    public WeaponEntryReference Copy()
    {
        return new WeaponEntry
        {
            Ammo = Ammo,
            Amount = Amount,
            Modifier = Modifier,
            WeaponName = WeaponName
        };
    }
}