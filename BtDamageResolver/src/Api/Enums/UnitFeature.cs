namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// This enum contains any features for units, which change the way game rules are applied to them.
/// This comprises both quirks and certain equipment.
/// </summary>
public enum UnitFeature
{
    /// <summary>
    /// Unit has an AMS missile defense system.
    /// </summary>
    Ams,

    /// <summary>
    /// Heat resistant armor negates extra damage from heat weapons to heat-vulnerable units.
    /// </summary>
    ArmorHeatResistant,

    /// <summary>
    /// Stealth armor bestows -1 to hit from medium and -2 to hit from long range.
    /// </summary>
    ArmorStealth,

    /// <summary>
    /// Unit has an active probe.
    /// </summary>
    ActiveProbe,

    /// <summary>
    /// Unit has a bad reputation.
    /// </summary>
    BadReputation,

    /// <summary>
    /// Unit may make melee punch attacks normally in the absence of hand actuators (no effect yet).
    /// </summary>
    BarrelFist,

    /// <summary>
    /// Bonus of 1 to hit in melee.
    /// </summary>
    BattleFists,

    /// <summary>
    /// Always generates 4 points less heat.
    /// </summary>
    CombatComputer,

    /// <summary>
    /// Better initiative rolls (no effect yet).
    /// </summary>
    CommandUnit,

    /// <summary>
    /// Two of these units fit one bay.
    /// </summary>
    CompactUnit,

    /// <summary>
    /// More head armor from the sides and back, less directly from the front (no effect yet).
    /// </summary>
    Cowl,

    /// <summary>
    /// Ejection is harder from this unit (no effect yet).
    /// </summary>
    DifficultEjection,

    /// <summary>
    /// Unit is harder to repair and maintain (no effect yet).
    /// </summary>
    DifficultToMaintain,

    /// <summary>
    /// Torso mount that is actually a turret (no effect yet).
    /// </summary>
    DirectionalTorsoMount,

    /// <summary>
    /// Unit is fearsome or distracting (no effect yet).
    /// </summary>
    Distracting,

    /// <summary>
    /// Unit is easier to repair and maintain (no effect yet).
    /// </summary>
    EasyToMaintain,

    /// <summary>
    /// Unit is easier to pilot (no effect yet).
    /// </summary>
    EasyToPilot,

    /// <summary>
    /// Unit has ECM.
    /// </summary>
    Ecm,

    /// <summary>
    /// Infantry/BattleArmor swarm attacks are more effective against the actuators of this unit (no effect yet).
    /// </summary>
    ExposedActuators,

    /// <summary>
    /// Unit can torso twist one more hex side (no effect yet).
    /// </summary>
    ExtendedTorsoTwist,

    /// <summary>
    /// Weapon may break on hit to weapon location (2D6 10+, no effect yet).
    /// </summary>
    ExposedWeaponLinkage,

    /// <summary>
    /// It is faster to reload the weapons of this unit (no effect yet).
    /// </summary>
    FastReload,

    /// <summary>
    /// Unit is harder to pilot (no effect yet) PS: This quirk is EXACTLY THE SAME as Cramped Cockpit, so the latter is not included.
    /// </summary>
    HardToPilot,

    /// <summary>
    /// Arm weapons can fire into rear arc even with lower arm and/or hand actuators present (no effect yet).
    /// </summary>
    HyperExtendingActuators,

    /// <summary>
    /// Unit has improved communications systems (no effect yet).
    /// </summary>
    ImprovedCommunications,

    /// <summary>
    /// Unit has an improved cooling jacket for its autocannons.
    /// </summary>
    ImprovedCoolingAutoCannon,

    /// <summary>
    /// Unit has an improved cooling jacket for its large energy weapons.
    /// </summary>
    ImprovedCoolingLargeEnergy,

    /// <summary>
    /// Unit has better life support systems (no effect yet).
    /// </summary>
    ImprovedLifeSupport,

    /// <summary>
    /// Unit has a more capable sensor system.
    /// </summary>
    ImprovedSensors,

    /// <summary>
    /// The main weapon of this unit is externally carried and can be picked up and put down during combat (no effect yet).
    /// </summary>
    JettisonableWeapon,

    /// <summary>
    /// Unit has MASC.
    /// </summary>
    Masc,

    /// <summary>
    /// Unit suffers no penalty from attacking multiple targets.
    /// </summary>
    MultiTarget,

    /// <summary>
    /// Half damage from glancing cluster shots (hit exactly at target value).
    /// </summary>
    NarrowLowProfile,

    /// <summary>
    /// Jumping provides 1 more evasion bonus.
    /// </summary>
    NimbleJumper,

    /// <summary>
    /// Unit has no arms or minimal arms (no effect yet).
    /// </summary>
    NoMinimalArms,

    /// <summary>
    /// Unit has high-mounted arms (no effect yet).
    /// </summary>
    OverheadArms,

    /// <summary>
    /// Unit cannot go from standstill to running speed in one turn (no effect yet).
    /// </summary>
    PoorPerformance,

    /// <summary>
    /// A vehicle may use full fast movement in reverse.
    /// </summary>
    PowerReverse,

    /// <summary>
    /// Infantry/BattleArmor swarm attacks are less effective against the actuators of this unit (no effect yet).
    /// </summary>
    ProtectedActuators,

    /// <summary>
    /// Half damage to legs from DFAs.
    /// </summary>
    ReinforcedLegs,

    /// <summary>
    /// Needs maintenance half as often as a regular unit (no effect yet).
    /// </summary>
    Rugged,

    /// <summary>
    /// Needs maintenance three times less often than a regular unit (no effect yet).
    /// </summary>
    Rugged2,

    /// <summary>
    /// Needs maintenance four times less often than a regular unit (no effect yet).
    /// </summary>
    Rugged3,

    /// <summary>
    /// Free searchlight component (no effect yet).
    /// </summary>
    Searchlight,

    /// <summary>
    /// Fast movement only gives -1 to hit.
    /// </summary>
    StabilizedWeapons,

    /// <summary>
    /// +1 to resist falling from impacts and attacks (no effect yet).
    /// </summary>
    Stable,

    /// <summary>
    /// Bonus of 2 to hit air units.
    /// </summary>
    TargetingAntiAir,

    /// <summary>
    /// Bonus of 1 to hit with direct-fire weapons.
    /// </summary>
    TargetingComputer,

    /// <summary>
    /// Bonus of 1 to hit at extreme range.
    /// </summary>
    TargetingExtremeRange,

    /// <summary>
    /// Bonus of 1 to hit at long range.
    /// </summary>
    TargetingLongRange,

    /// <summary>
    /// Bonus of 1 to hit at medium range.
    /// </summary>
    TargetingMediumRange,

    /// <summary>
    /// Bonus of 1 to hit at short range.
    /// </summary>
    TargetingShortRange,

    /// <summary>
    /// A very common and easily available unit (no effect yet).
    /// </summary>
    Ubiquitous,

    /// <summary>
    /// -1 to piloting when entering difficult terrain hexes (no effect yet).
    /// </summary>
    Unbalanced,

    /// <summary>
    /// Effective head armor is 1 less than assigned (no effect yet).
    /// </summary>
    WeakHeadArmor,

    /// <summary>
    /// Unit is liable to damage its own legs if it tries to kick/dfa.
    /// </summary>
    WeakLegs
}