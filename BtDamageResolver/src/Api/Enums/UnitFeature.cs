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

    // Unit has ECM
    Ecm,

    // Unit has improved communications systems (for satellite uplinks)
    ImprovedCommunications,

    // Unit has MASC
    Masc,

    // Half damage from glancing cluster shots (hit exactly at target value)
    NarrowLowProfile,

    // Jumping provides 1 more evasion bonus
    NimbleJumper,

    // Half damage to legs from DFAs
    ReinforcedLegs,

    // Running only gives -1 to hit
    StabilizedWeapons,

    // Bonus of 2 to hit air units
    TargetingAntiAir,

    // Bonus of 1 to hit at extreme range
    TargetingExtremeRange,

    // Bonus of 1 to hit at long range
    TargetingLongRange,

    // Bonus of 1 to hit at medium range
    TargetingMediumRange,

    // Bonus of 1 to hit at short range
    TargetingShortRange
}