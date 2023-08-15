using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A damage request for generating single instances of damage.
/// </summary>
[Serializable]
public class DamageInstance
{
    /// <summary>
    /// The attack type of this damage request.
    /// </summary>
    public AttackType AttackType { get; set; }

    /// <summary>
    /// Cluster size to use while dealing the damage.
    /// </summary>
    public int ClusterSize { get; set; }

    /// <summary>
    /// Cover against the damage, if any.
    /// </summary>
    public Cover Cover { get; set; }

    /// <summary>
    /// The amount of damage to deal.
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// The direction the damage is coming from.
    /// </summary>
    public Direction Direction { get; set; }

    /// <summary>
    /// The Unit to damage.
    /// </summary>
    public Guid UnitId { get; set; }

    /// <summary>
    /// The timestamp of this damage instance.
    /// </summary>
    public DateTime TimeStamp { get; set; }
}