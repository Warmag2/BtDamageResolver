using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// The state of a weapon.
/// </summary>
/// <remarks>
/// Silly as an enum, but in the future, this may include "Destroyed", "Jammed" etc.
/// </remarks>
[Serializable]
public enum WeaponState
{
    Active,
    Inactive,
    Destroyed
}