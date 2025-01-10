using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A weapon entry portraying a weapon and its current settings.
/// </summary>
[Serializable]
public class WeaponEntry : WeaponEntryReference
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
    public WeaponEntry(WeaponEntryReference weaponReference)
    {
        Ammo = weaponReference.Ammo;
        Id = Guid.NewGuid();
        State = WeaponState.Active;
        TimeStamp = DateTime.UtcNow;
        WeaponName = weaponReference.WeaponName;
    }

    /// <summary>
    /// The ID of this weapon entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Static attack modifier for this specific weapon entry.
    /// </summary>
    /// <remarks>
    /// Almost always 0. Exceptions are accurate/inaccurate weapon quirks and actuator damage.
    /// </remarks>
    public int Modifier { get; set; }

    /// <summary>
    /// The state of the weapon.
    /// </summary>
    public WeaponState State { get; set; }

    /// <summary>
    /// The timestamp when this weapon entry was last updated.
    /// </summary>
    public DateTime TimeStamp { get; set; }

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
}