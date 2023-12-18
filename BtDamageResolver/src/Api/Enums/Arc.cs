namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Possible firing arcs.
/// </summary>
public enum Arc
{
    /// <summary>
    /// Default arc when unit does not multi-target (nearly always).
    /// </summary>
    /// <remarks>
    /// Invalid if the unit actually multi-targets, not counted otherwise.
    /// </remarks>
    Default,

    /// <summary>
    /// Mech/Vehicle/Aerospace front.
    /// </summary>
    Front,

    /// <summary>
    /// Mech/Vechile left.
    /// </summary>
    Left,

    /// <summary>
    /// Mech/Vechile right.
    /// </summary>
    Right,

    /// <summary>
    /// Mech/Vechile/Aerospace rear.
    /// </summary>
    Rear,

    /// <summary>
    /// Aerodyne aerospace left wing.
    /// </summary>
    LeftWing,

    /// <summary>
    /// Aerodyne aerospace right wing.
    /// </summary>
    RightWing,

    /// <summary>
    /// Aerodyne aerospace left wing backwards.
    /// </summary>
    LeftWingRear,

    /// <summary>
    /// Aerodyne aerospace right wing backwards.
    /// </summary>
    RightWingRear,

    /// <summary>
    /// Capital ship forward left.
    /// </summary>
    ForeLeft,

    /// <summary>
    /// Capital ship forward right.
    /// </summary>
    ForeRight,

    /// <summary>
    /// Capital ship rear left.
    /// </summary>
    RearLeft,

    /// <summary>
    /// Capital ship rear right.
    /// </summary>
    RearRight
}
