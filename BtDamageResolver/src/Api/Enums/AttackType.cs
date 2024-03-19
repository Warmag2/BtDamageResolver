using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// An attack type.
/// </summary>
[Serializable]
public enum AttackType
{
    /// <summary>
    /// Normal weapon attack.
    /// </summary>
    Normal,

    /// <summary>
    /// Generic melee attack (hits full paperdoll like a weapon attack).
    /// </summary>
    Melee,

    /// <summary>
    /// Kick.
    /// </summary>
    Kick,

    /// <summary>
    /// Punch.
    /// </summary>
    Punch,
}