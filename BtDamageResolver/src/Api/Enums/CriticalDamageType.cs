using System;
using System.Diagnostics.CodeAnalysis;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// The critical damage type.
/// </summary>
/// <remarks>
/// Indicates either a generic critical or an explicit item which has been damaged or destroyed.
/// </remarks>
[Serializable]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Self-evident and fix would be very noisy.")]
public enum CriticalDamageType
{
    None,
    Ammunition,
    Avionics,
    BlownOff,
    Bomb,
    Cargo,
    Commander,
    Control,
    CoPilot,
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
    WeaponDestroyed,
    WeaponMalfunction,
}