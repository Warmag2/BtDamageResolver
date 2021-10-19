namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// This enum contains any features for units, which change the way game rules are applied to them.
    /// This comprises both quirks and certain equipment.
    /// </summary>
    public enum UnitFeature
    {
        Ams,                    // Unit has an AMS missile defense system
        Bap,                    // Unit has a beagle active probe
        BattleFists,            // Bonus of 1 to hit in melee
        CombatComputer,         // Always generates 4 points less heat
        Ecm,                    // Unit has ECM
        Masc,                   // Unit has MASC
        NarrowLowProfile,       // Half damage from glancing cluster shots (hit exactly at target value)
        NimbleJumper,           // Jumping provides 1 more evasion bonus
        ReinforcedLegs,         // Half damage to legs from DFAs
        StabilizedWeapons,      // Running only gives -1 to hit
        TargetingAntiAir,       // Bonus of 2 to hit air units
        TargetingExtremeRange,  // Bonus of 1 to hit at extreme range
        TargetingLongRange,     // Bonus of 1 to hit at long range
        TargetingMediumRange,   // Bonus of 1 to hit at medium range
        TargetingShortRange     // Bonus of 1 to hit at short range
    }
}