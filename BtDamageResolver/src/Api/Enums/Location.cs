using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
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
        Front,
        Rear,
        Turret,
        Propulsion,
        BattleArmor,
        Trooper,
        Structure
    }
}
