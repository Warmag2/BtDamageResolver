using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Possible hit locations.
/// </summary>
[Serializable]
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
    Front,          // Also nose
    Rear,           // Also aft
    FrontLeft,      // Aerospace capital fore-left
    FrontRight,     // Aerospace capital fore-right
    RearLeft,       // Aerospace capital aft-left
    RearRight,      // Aerospace capital aft-right
    Turret,
    Propulsion,     // VTOL propulsion
    BattleArmor,
    Trooper,
    Structure,      // Single-block Internal structure, unimplemented
}