namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// This enum contains any features for units, which change the way game rules are applied to them.
/// This comprises both quirks and certain equipment.
/// </summary>
public enum UnitFeature
{
    // Unit has an AMS missile defense system
    Ams,

    // Heat resistant armor negates extra damage from heat weapons to heat-vulnerable units
    ArmorHeatResistant,

    // Stealth armor bestows -1 to hit from medium and -2 to hit from long range
    ArmorStealth,

    // Unit has a beagle active probe (or the Improved Sensors quirk)
    Bap,

    // Bonus of 1 to hit in melee
    BattleFists,

    // Always generates 4 points less heat
    CombatComputer,

    // Better initiative rolls (no effect yet)
    CommandUnit,

    // Better initiative rolls (no effect yet)
    Cowl,

    // Unit is easy to pilot (no effect yet)
    EasyToPilot,

    // Unit has ECM
    Ecm,

    // Unit can torso twist one more hex side (no effect yet)
    ExtendedTorsoTwist,

    // Unit is hard to pilot (no effect yet) PS: This quirk is EXACTLY THE SAME as Cramped Cockpit, so the latter is not included!
    HardToPilot,

    // Unit has improved communications systems (no effect yet)
    ImprovedCommunications,

    // The main weapon of this unit is externally carried and can be picked up and put down during combat (no effect yet)
    JettisonableWeapon,

    // Unit has MASC
    Masc,

    // Half damage from glancing cluster shots (hit exactly at target value)
    NarrowLowProfile,

    // Jumping provides 1 more evasion bonus
    NimbleJumper,

    // Unit has no arms or minimal arms (no effect yet)
    NoMinimalArms,

    // Unit cannot go from standstill to running speed in one turn (no effect yet)
    PoorPerformance,

    // Infantry/BattleArmor swarm attacks are less effective against the actuators of this unit (no effect yet)
    ProtectedActuators,

    // Half damage to legs from DFAs
    ReinforcedLegs,

    // Running only gives -1 to hit
    StabilizedWeapons,

    // +1 to resist falling from impacts and attacks (no effect yet)
    Stable,

    // Bonus of 2 to hit air units
    TargetingAntiAir,

    // Bonus of 1 to hit at extreme range
    TargetingExtremeRange,

    // Bonus of 1 to hit at long range
    TargetingLongRange,

    // Bonus of 1 to hit at medium range
    TargetingMediumRange,

    // Bonus of 1 to hit at short range
    TargetingShortRange,

    // -1 to piloting when entering difficult terrain hexes (no effect yet)
    Unbalanced,

    // Unit is liable to damage its own legs if it tries to kick/dfa
    WeakLegs
}