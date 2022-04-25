using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// Extension of Unit which also contains all volatile data.
    /// </summary>
    [Serializable]
    public class UnitEntry : Unit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitEntry"/> class.
        /// </summary>
        public UnitEntry()
        {
            TimeStamp = DateTime.UtcNow;
            Id = Guid.NewGuid();

            Features = new HashSet<UnitFeature>();
            FiringSolution = new FiringSolution();
            Weapons = new List<WeaponEntry>();

            Troopers = 1; // In practice, 0 is illegal in many situations and this is never bad.
        }

        /// <summary>
        /// The last update time of this unit.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Current amount of targeting difficulty -inducing effects for this unit (for now, only EMP).
        /// </summary>
        public int Penalty { get; set; }

        /// <summary>
        /// The amount of heat this unit currently has.
        /// </summary>
        public int Heat { get; set; }

        /// <summary>
        /// The ID of this unit.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Is this unit currently narced.
        /// </summary>
        public bool Narced { get; set; }

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
        /// The firing solution of this unit this.
        /// </summary>
        public FiringSolution FiringSolution { get; set; }

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
        /// Does this unit track heat.
        /// </summary>
        /// <returns>Is the unit a heat-tracking unit.</returns>
        public bool IsHeatTracking()
        {
            switch (Type)
            {
                case UnitType.AerospaceFighter:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    return true;
            }

            return false;
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

            var result = 0;

            if (Heat >= 8)
            {
                result += 1;
            }

            if (Heat >= 13)
            {
                result += 1;
            }

            if (Heat >= 17)
            {
                result += 1;
            }

            if (Heat >= 24)
            {
                result += 1;
            }

            return result;
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

            var result = 0;

            if (Heat >= 5)
            {
                result += 1;
            }

            if (Heat >= 10)
            {
                result += 1;
            }

            if (Heat >= 15)
            {
                result += 1;
            }

            if (Heat >= 20)
            {
                result += 1;
            }

            if (Heat >= 25)
            {
                result += 1;
            }

            return result;
        }

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

            if (Heat >= 28)
            {
                return 8;
            }

            if (Heat >= 23)
            {
                return 6;
            }

            if (Heat >= 19)
            {
                return 4;
            }

            return 0;
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

            if (Heat >= 30)
            {
                return 13;
            }

            if (Heat >= 26)
            {
                return 10;
            }

            if (Heat >= 22)
            {
                return 8;
            }

            if (Heat >= 18)
            {
                return 6;
            }

            if (Heat >= 14)
            {
                return 4;
            }

            return 0;
        }

        /// <summary>
        /// The current ground or air speed of this unit, when moving at normal speed.
        /// </summary>
        /// <param name="movementClass">The movement class.</param>
        /// <param name="accountForHeat">Account for heat.</param>
        /// <returns>The current maximum speed of this unit.</returns>
        public int GetCurrentSpeed(MovementClass movementClass, bool accountForHeat = true)
        {
            switch (movementClass)
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
                    throw new ArgumentOutOfRangeException(nameof(movementClass), movementClass, null);
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
                Features = Features.Copy(),
                Gunnery = Gunnery,
                JumpJets = JumpJets,
                Piloting = Piloting,
                Sinks = Sinks,
                Speed = Speed,
                Tonnage = Tonnage,
                Troopers = Troopers,
                Type = Type,
                Weapons = Weapons.Select(w => w.Copy()).ToList(),
                FiringSolution = FiringSolution.Copy(),
                Heat = Heat,
                Id = id,
                Movement = Movement,
                MovementClass = MovementClass,
                Name = GenerateName(Name),
                Narced = Narced,
                Penalty = Penalty,
                StaticDataHidden = StaticDataHidden,
                Tagged = Tagged,
                TimeStamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Imports the contents of a given <see cref="Unit"/> into this unit.
        /// </summary>
        /// <param name="unit">The <see cref="Unit"/> to import data from.</param>
        /// <remarks>Name will not be overwritten.</remarks>
        public void ImportFromUnit(Unit unit)
        {
            Features = unit.Features.Copy();
            Gunnery = unit.Gunnery;
            JumpJets = unit.JumpJets;
            Piloting = unit.Piloting;
            Sinks = unit.Sinks;
            Speed = unit.Speed;
            Tonnage = unit.Tonnage;
            Troopers = unit.Troopers;
            Weapons = Weapons.Select(w => w.Copy()).ToList();
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
            var copy = Copy();

            // In this case we want to keep the name because Copy() generates a new one
            copy.Name = Name;

            return copy;
        }

        private static string GenerateName(string name)
        {
            var numbersAtEndOfString = name.AsEnumerable().Reverse().TakeWhile(char.IsNumber).Reverse().ToArray();

            return numbersAtEndOfString.Any() ? $"{name.TrimEnd(numbersAtEndOfString)}{int.Parse(string.Concat(numbersAtEndOfString)) + 1}" : $"{name} 2";
        }

        private int GetCurrentSpeedInternal(bool accountForHeat = true)
        {
            return accountForHeat ? Math.Max(Speed - GetHeatSpeedPenalty(), 1) : Speed;
        }
    }
}
