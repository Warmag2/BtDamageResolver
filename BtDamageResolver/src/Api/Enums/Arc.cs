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
    /// Arc for 360 degree weapons without facing, such as infantry and battle armor.
    /// </summary>
    Full,

    /// <summary>
    /// Arc for 360 degree weapons for units with facing, such as vehicle and quad mech turrets.
    /// </summary>
    Turret,

    /// <summary>
    /// Mech/Vehicle front.
    /// </summary>
    Front,

    /// <summary>
    /// Vehicle left.
    /// </summary>
    Left,

    /// <summary>
    /// Vehicle right.
    /// </summary>
    Right,

    /// <summary>
    /// Mech/Vehicle rear.
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
    /// Capital ship / dropship forward left.
    /// </summary>
    ForeLeft,

    /// <summary>
    /// Capital ship / dropship forward right.
    /// </summary>
    ForeRight,

    /// <summary>
    /// Capital ship left broadside.
    /// </summary>
    BroadsideLeft,

    /// <summary>
    /// Capital ship right broadside.
    /// </summary>
    BroadsideRight,

    /// <summary>
    /// Capital ship / dropship aft left.
    /// </summary>
    AftLeft,

    /// <summary>
    /// Capital ship / dropship aft right.
    /// </summary>
    AftRight,

    /// <summary>
    /// Aerospace nose.
    /// </summary>
    Nose,

    /// <summary>
    /// Aerospace aft.
    /// </summary>
    Aft
}
