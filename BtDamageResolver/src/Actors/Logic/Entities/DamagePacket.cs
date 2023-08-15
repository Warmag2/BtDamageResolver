using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Entities;

/// <summary>
/// A packet of damage which can be directly applied to a location.
/// </summary>
[Serializable]
public class DamagePacket
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DamagePacket"/> class.
    /// </summary>
    /// <param name="damageAmount">The amount of damage for this packet.</param>
    /// <param name="specialDamageEntries">Special damage entries for this packet, if any.</param>
    public DamagePacket(int damageAmount, List<SpecialDamageEntry> specialDamageEntries)
    {
        Damage = damageAmount;
        SpecialDamageEntries = specialDamageEntries;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DamagePacket"/> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor for serialization.
    /// </remarks>
    public DamagePacket()
    {
    }

    /// <summary>
    /// The damage amount.
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// The special damage entries.
    /// </summary>
    public List<SpecialDamageEntry> SpecialDamageEntries { get; set; }
}