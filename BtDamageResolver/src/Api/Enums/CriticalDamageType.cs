using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// The critical damage type.
/// </summary>
/// <remarks>
/// Indicates either a generic critical or an explicit item which has been damaged or destroyed.
/// </remarks>
[Serializable]
public enum CriticalDamageType
{
    None,
    Ammunition,
    Avionics,
    BlownOff,
    Bomb,
    Cargo,
    CombatInformationCenter,
    Commander,
    Control,
    CoPilot,
    Crew,
    CrewKilled,
    CrewStunned,
    Critical,
    DockingCollar,
    Door,
    Driver,
    Engine,
    Equipment,
    Fcs,
    FlightStabilizer,
    Fuel,
    Gear,
    GravDeck,
    HeatSink,
    HeavyMotive,
    Immobilized,
    LifeSupport,
    LightMotive,
    LimbBlownOff,
    ModerateMotive,
    Pilot,
    Propulsion,
    PropulsionDestroyed,
    Sensors,
    Stabilizer,
    Thruster,
    TurretJam,
    TurretLock,
    WarpDrive,
    WarpDriveSupport,
    WeaponDestroyed,
    WeaponDestroyedBroadside,
    WeaponMalfunction,
}