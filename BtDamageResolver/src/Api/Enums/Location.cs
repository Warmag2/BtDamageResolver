using System;
using System.Diagnostics.CodeAnalysis;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Possible hit locations.
/// </summary>
[Serializable]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Self-evident and fix would be very noisy.")]
public enum Location
{
    Reroll,
    Head,
    LeftTorso,
    RightTorso,
    CenterTorso,
    RearLeftTorso,
    RearRightTorso,
    RearCenterTorso,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    CenterLeg,
    RearLeftLeg,
    RearRightLeg,
    Left,
    Right,
    Front,
    Rear,
    Turret,
    Propulsion,
    BattleArmor,
    Trooper,
    Structure
}