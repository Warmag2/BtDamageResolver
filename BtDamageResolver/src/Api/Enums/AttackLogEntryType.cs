using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// An attack log entry type.
/// </summary>
[Serializable]
public enum AttackLogEntryType
{
    /// <summary>
    /// Attack log calclulation.
    /// </summary>
    Calculation,

    /// <summary>
    /// Critical damage log entry.
    /// </summary>
    Critical,

    /// <summary>
    /// Damage log entry.
    /// </summary>
    Damage,

    /// <summary>
    /// Dice roll.
    /// </summary>
    DiceRoll,

    /// <summary>
    /// Fire event.
    /// </summary>
    Fire,

    /// <summary>
    /// Firing solution specification or calculation.
    /// </summary>
    FiringSolution,

    /// <summary>
    /// Heat event.
    /// </summary>
    Heat,

    /// <summary>
    /// Hit event.
    /// </summary>
    Hit,

    /// <summary>
    /// General information.
    /// </summary>
    Information,

    /// <summary>
    /// Weapon missed event.
    /// </summary>
    Miss,

    /// <summary>
    /// Special damage occurrence.
    /// </summary>
    SpecialDamage
}