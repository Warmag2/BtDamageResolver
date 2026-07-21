using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// Extension of Unit which also contains all volatile data.
/// </summary>
[Serializable]
public partial class UnitEntry : Unit, IEntityWithRulesValidation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitEntry"/> class.
    /// </summary>
    public UnitEntry()
    {
        TimeStamp = DateTime.UtcNow;
        Id = Guid.NewGuid();
        Consumables = new();
        Features = [];
        WeaponBays = [];
        Troopers = 1; // In practice, 0 is illegal in many situations and this is never bad.
    }

    /// <summary>
    /// All the resources this unit entry has currently spent.
    /// </summary>
    public Consumables Consumables { get; set; }

    /// <summary>
    /// Is the unit currently evading.
    /// </summary>
    /// <remarks>
    /// Only valid for aerospace units.
    /// </remarks>
    public bool Evading { get; set; }

    /// <summary>
    /// The gunnery skill of this unit.
    /// </summary>
    public int Gunnery { get; set; } = 4;

    /// <summary>
    /// The ID of this unit.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Is this unit currently narced.
    /// </summary>
    public bool Narced { get; set; }

    /// <summary>
    /// The piloting skill of this unit.
    /// </summary>
    public int Piloting { get; set; } = 5;

    /// <summary>
    /// Is this unit currently tagged.
    /// </summary>
    public bool Tagged { get; set; }

    /// <summary>
    /// The current movement class.
    /// </summary>
    public MovementClass MovementClass { get; set; }

    /// <summary>
    /// The number of hexes this unit has moved.
    /// </summary>
    public int Movement { get; set; }

    /// <summary>
    /// The stance the unit is in.
    /// </summary>
    public Stance Stance { get; set; }

    /// <summary>
    /// Is the unit "finished", i.e. should its editing settings be shown by default.
    /// </summary>
    /// <remarks>
    /// This isn't really something that is integral to the concept of the unit itself,
    /// but when units are loaded from the repository, this setting should be remembered.
    /// </remarks>
    public bool StaticDataHidden { get; set; }

    /// <summary>
    /// The last update time of this unit.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// The weapons this unit has equipped, listed in bays/arcs.
    /// </summary>
    /// <remarks>
    /// Typically only a single bay with all weapons.
    /// </remarks>
    public new List<WeaponBay> WeaponBays { get; set; }

    /// <summary>
    /// Gets the difficulty of the ammo explosion roll, based on heat.
    /// </summary>
    /// <returns>The difficulty level of an ammo explosion roll for the current heat.</returns>
    public int GetHeatAmmoExplosionDifficulty()
    {
        if (!IsHeatTracking())
        {
            return 0;
        }

        return Consumables.Heat switch
        {
            >= 28 => 8,
            >= 23 => 6,
            >= 19 => 4,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the penalty to attacks from heat.
    /// </summary>
    /// <returns>The penalty to attacks from unit heat.</returns>
    public int GetHeatAttackPenalty()
    {
        if (!IsHeatTracking())
        {
            return 0;
        }

        return Consumables.Heat switch
        {
            >= 24 => 4,
            >= 17 => 3,
            >= 13 => 2,
            >= 8 => 1,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the threshold for avoiding pilot damage from heat.
    /// </summary>
    /// <returns>The difficulty level for avoiding pilot damage for the current heat.</returns>
    public int GetHeatPilotDamageDifficulty()
    {
        if (!IsHeatTracking())
        {
            return 0;
        }

        return Consumables.Heat switch
        {
            >= 27 => 9,
            >= 21 => 6,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the threshold for avoiding random movement from heat.
    /// </summary>
    /// <returns>The difficulty level for avoiding random movement for the current heat.</returns>
    public int GetHeatRandomMovementDifficulty()
    {
        if (!IsHeatTracking())
        {
            return 0;
        }

        return Consumables.Heat switch
        {
            >= 25 => 10,
            >= 20 => 8,
            >= 15 => 7,
            >= 10 => 6,
            >= 5 => 5,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the difficulty of the shutdown, based on heat.
    /// </summary>
    /// <returns>The difficulty level of the shutdown roll for current heat.</returns>
    public int GetHeatShutdownDifficulty()
    {
        if (!IsHeatTracking())
        {
            return 0;
        }

        return Consumables.Heat switch
        {
            >= 30 => 13,
            >= 26 => 10,
            >= 22 => 8,
            >= 18 => 6,
            >= 14 => 4,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the penalty to speed from heat.
    /// </summary>
    /// <returns>The penalty to speed from unit heat.</returns>
    public int GetHeatSpeedPenalty()
    {
        if (!IsHeatTracking())
        {
            return 0;
        }

        return Consumables.Heat switch
        {
            >= 25 => 5,
            >= 20 => 4,
            >= 15 => 3,
            >= 10 => 2,
            >= 5 => 1,
            _ => 0
        };
    }

    /// <summary>
    /// The current maximum ground or air speed of this unit, considering its movement class.
    /// </summary>
    /// <param name="accountForHeat">Account for heat.</param>
    /// <returns>The current maximum speed of this unit.</returns>
    public int GetCurrentSpeed(bool accountForHeat = true)
    {
        switch (MovementClass)
        {
            case MovementClass.Immobile:
            case MovementClass.Stationary:
                return 0;
            case MovementClass.Normal:
                return GetCurrentSpeedInternal(accountForHeat);
            case MovementClass.Fast:
                return (int)Math.Ceiling(GetCurrentSpeedInternal(accountForHeat) * 1.5m);
            case MovementClass.Masc:
                return GetCurrentSpeedInternal(accountForHeat) * 2;
            case MovementClass.Jump:
                return JumpJets;
            case MovementClass.OutOfControl:
                return (int)Math.Ceiling(GetCurrentSpeedInternal(accountForHeat) * 1.5m);
            default:
                throw new InvalidOperationException($"No current speed handling for movement class: {MovementClass}");
        }
    }

    /// <summary>
    /// Provides a true copy of the unit.
    /// </summary>
    /// <remarks>
    /// No references are copied, all entities in the new object are new ones.
    /// A new Guid is generated.
    /// A new Name is generated based on the existing name.
    /// The TimeStamp is marked as the present.
    /// </remarks>
    /// <returns>A copy of the unit in question.</returns>
    public UnitEntry Copy()
    {
        var id = Guid.NewGuid();

        return new UnitEntry
        {
            Consumables = Consumables.Copy(),
            Evading = Evading,
            Features = Features.Copy(),
            Gunnery = Gunnery,
            Id = id,
            JumpJets = JumpJets,
            Movement = Movement,
            MovementClass = MovementClass,
            Name = GenerateName(Name),
            Narced = Narced,
            Piloting = Piloting,
            Sinks = Sinks,
            Speed = Speed,
            Stance = Stance,
            StaticDataHidden = StaticDataHidden,
            Tagged = Tagged,
            TimeStamp = DateTime.UtcNow,
            Tonnage = Tonnage,
            Troopers = Troopers,
            Type = Type,
            WeaponBays = WeaponBays.Select(w => w.Copy()).ToList(),
        };
    }

    /// <summary>
    /// Imports the contents of a given <see cref="Unit"/> into this unit.
    /// </summary>
    /// <param name="unit">The <see cref="Unit"/> to import data from.</param>
    /// <remarks>Name will not be overwritten.</remarks>
    public void FromUnit(Unit unit)
    {
        Features = unit.Features.Copy();
        JumpJets = unit.JumpJets;
        Sinks = unit.Sinks;
        Speed = unit.Speed;
        TimeStamp = DateTime.UtcNow;
        Tonnage = unit.Tonnage;
        Troopers = unit.Troopers;
        Type = unit.Type;
        WeaponBays = unit.WeaponBays.Select(w => new WeaponBay(w)).ToList();
    }

    /// <summary>
    /// Creates an <see cref="Unit"/> based on this unit.
    /// </summary>
    /// <remarks>
    /// No references are copied, all entities in the new object are new ones.
    /// </remarks>
    /// <returns>An <see cref="Unit"/> based on this unit.</returns>
    public Unit ToUnit()
    {
        return new Unit
        {
            Features = Features.Copy(),
            JumpJets = JumpJets,
            Name = Name,
            Sinks = Sinks,
            Speed = Speed,
            Tonnage = Tonnage,
            Troopers = Troopers,
            Type = Type,
            WeaponBays = WeaponBays.Select(w => new WeaponBayReference(w)).ToList()
        };
    }

    private static string GenerateName(string name)
    {
        var numbersAtEndOfString = name.AsEnumerable().Reverse().TakeWhile(char.IsAsciiDigit).Reverse().ToArray();

        if (numbersAtEndOfString.Length == 0)
        {
            return $"{name} 2";
        }

        var nextNumber = int.Parse(string.Concat(numbersAtEndOfString), CultureInfo.InvariantCulture) + 1;
        return $"{name.TrimEnd(numbersAtEndOfString)}{nextNumber.ToString(CultureInfo.InvariantCulture)}";
    }

    private int GetCurrentSpeedInternal(bool accountForHeat = true)
    {
        return accountForHeat ? Math.Max(Speed - GetHeatSpeedPenalty(), 1) : Speed;
    }
}