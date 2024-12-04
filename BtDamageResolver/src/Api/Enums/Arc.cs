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
    /// Arc for 360 degree weapons, such as infantry, battle armor and vehicle turrets.
    /// </summary>
    Full,

    /// <summary>
    /// Mech/Vehicle front.
    /// </summary>
    Front,

    /// <summary>
    /// Vechile left.
    /// </summary>
    Left,

    /// <summary>
    /// Vechile right.
    /// </summary>
    Right,

    /// <summary>
    /// Mech/Vechile rear.
    /// </summary>
    Rear,

    /// <summary>
    /// Mech left arm.
    /// </summary>
    LeftArm,

    /// <summary>
    /// Mech right arm.
    /// </summary>
    RightArm,

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
    FrontLeft,

    /// <summary>
    /// Capital ship forward right.
    /// </summary>
    FrontRight,

    /// <summary>
    /// Capital ship left broadside.
    /// </summary>
    BroadsideLeft,

    /// <summary>
    /// Capital ship right broadside.
    /// </summary>
    BroadsideRight,

    /// <summary>
    /// Capital ship rear left.
    /// </summary>
    RearLeft,

    /// <summary>
    /// Capital ship rear right.
    /// </summary>
    RearRight,

    /// <summary>
    /// Aerospace nose.
    /// </summary>
    Nose,

    /// <summary>
    /// Aerospace aft.
    /// </summary>
    Aft
}
