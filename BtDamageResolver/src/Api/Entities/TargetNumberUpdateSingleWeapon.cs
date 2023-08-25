using System;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// Event class defining a new set of target numbers for a specific weapon class and a specific unit.
/// </summary>
[Serializable]
public class TargetNumberUpdateSingleWeapon
{
    /// <summary>
    /// Target number calculation log shows how the target number was calculated.
    /// </summary>
    public AttackLog CalculationLog { get; set; }

    /// <summary>
    /// The target number.
    /// </summary>
    public int TargetNumber { get; set; }
}