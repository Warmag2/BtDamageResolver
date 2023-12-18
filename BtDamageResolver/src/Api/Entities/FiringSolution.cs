using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// The firing solution.
/// </summary>
[Serializable]
public class FiringSolution
{
    /// <summary>
    /// The arc the target is in.
    /// </summary>
    public Arc Arc { get; set; }

    /// <summary>
    /// The attack modifier.
    /// </summary>
    public int AttackModifier { get; set; }

    /// <summary>
    /// Target cover for this firing solution.
    /// </summary>
    public Cover Cover { get; set; }

    /// <summary>
    /// Direction of fire.
    /// </summary>
    public Direction Direction { get; set; }

    /// <summary>
    /// Distance to target.
    /// </summary>
    public int Distance { get; set; }

    /// <summary>
    /// The ID of the target unit.
    /// </summary>
    public Guid Target { get; set; }

    /// <summary>
    /// The last update time of this firing solution.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Produces a deep copy of a firing solution.
    /// </summary>
    /// <returns>A copy of the firing solution.</returns>
    public FiringSolution Copy()
    {
        return new FiringSolution
        {
            AttackModifier = AttackModifier,
            Cover = Cover,
            Direction = Direction,
            Distance = Distance,
            Target = Target
        };
    }
}