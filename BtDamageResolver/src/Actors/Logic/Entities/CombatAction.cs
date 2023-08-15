using System;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Entities;

/// <summary>
/// Represents a combat action (melee or weapon attack, charge, DFA, etc) by an unit.
/// </summary>
[Serializable]
public class CombatAction
{
    /// <summary>
    /// Did the combat action actually happen or not.
    /// </summary>
    /// <remarks>
    /// May not be true for streaks etc. even if the fire event would othetwise happen.
    /// </remarks>
    public bool ActionHappened { get; set; }

    /// <summary>
    /// Did the combat action hit or not.
    /// </summary>
    /// <remarks>
    /// May be modified after the fact, for ex. by AMS and ECM.
    /// </remarks>
    public bool HitHappened { get; set; }

    /// <summary>
    /// The margin of success (or failure if negative) for this combat action.
    /// </summary>
    public int MarginOfSuccess { get; set; }

    /// <summary>
    /// The range bracket in which the combat action happened.
    /// </summary>
    public RangeBracket RangeBracket { get; set; }

    /// <summary>
    /// The instigating unit type.
    /// </summary>
    public UnitType UnitType { get; set; }

    /// <summary>
    /// The number of troopers in the instigating unit.
    /// </summary>
    public int Troopers { get; set; }

    /// <summary>
    /// The weapon which initiated the combat action.
    /// </summary>
    public Weapon Weapon { get; set; }
}