using System;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public class LogicHitModifier : ILogicHitModifier
    {
        private readonly IMathExpression _mathExpression;
        private readonly int[] _movementModifierArray;

        public LogicHitModifier(IMathExpression mathExpression)
        {
            _mathExpression = mathExpression;
            _movementModifierArray = new int[26];
            for(int ii=3; ii <= 4; ii++)
            {
                _movementModifierArray[ii] = 1;
            }
            for (int ii = 5; ii <= 6; ii++)
            {
                _movementModifierArray[ii] = 2;
            }
            for (int ii = 7; ii <= 9; ii++)
            {
                _movementModifierArray[ii] = 3;
            }
            for (int ii = 10; ii <= 17; ii++)
            {
                _movementModifierArray[ii] = 4;
            }
            for (int ii = 18; ii <= 24; ii++)
            {
                _movementModifierArray[ii] = 5;
            }
            for (int ii = 18; ii <= 24; ii++)
            {
                _movementModifierArray[ii] = 5;
            }
            _movementModifierArray[25] = 6;
        }

        /// <inheritdoc />
        public (int targetNumber, RangeBracket rangeBracket) ResolveHitModifier(AttackLog attackLog, GameOptions options, UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon, WeaponMode mode)
        {
            var modifierBase = weapon.AttackType == AttackType.Normal ? firingUnit.Gunnery : firingUnit.Piloting; 
            
            // NOTE: In this method, the damageReport may be null, as the target number calculation does not need a damage log. Everywhere else, it must exist.
            attackLog.Append(new AttackLogEntry { Context = "Base hit modifier",  Type = AttackLogEntryType.Calculation, Number = modifierBase });

            var modifierHeat = firingUnit.GetHeatAttackPenalty();

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from heat effects", Type = AttackLogEntryType.Calculation, Number = modifierHeat });

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from EMP effects", Type = AttackLogEntryType.Calculation, Number = firingUnit.Penalty });

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from firing solution", Type = AttackLogEntryType.Calculation, Number = firingUnit.FiringSolution.AttackModifier });

            var rangeBracket = GetRangeBracket(firingUnit, weapon);
            var modifierRange = GetRangeModifier(firingUnit, rangeBracket);
            modifierRange += GetMinimumRangeModifier(firingUnit, weapon, mode);

            attackLog.Append(new AttackLogEntry {Context = "Hit modifier from range", Type = AttackLogEntryType.Calculation, Number = modifierRange});

            var modifierWeapon = GetWeaponModifier(weapon, mode);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from weapon properties", Type = AttackLogEntryType.Calculation, Number = modifierWeapon });

            var modifierUnitType = GetUnitTypeModifier(targetUnit);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from unit type", Type = AttackLogEntryType.Calculation, Number = modifierUnitType });

            var modifierCover = GetCoverModifier(firingUnit, targetUnit);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from cover", Type = AttackLogEntryType.Calculation, Number = modifierCover });

            var modifierMovementDirection = GetMovementDirectionModifier(firingUnit, targetUnit);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from movement direction", Type = AttackLogEntryType.Calculation, Number = modifierMovementDirection });

            var modifierMovementClass = GetMovementClassModifier(targetUnit, weapon, mode);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from movement class", Type = AttackLogEntryType.Calculation, Number = modifierMovementClass });

            var modifierMovement = GetMovementModifier(targetUnit, weapon, mode);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from target movement", Type = AttackLogEntryType.Calculation, Number = modifierMovement });

            var modifierOwnMovement = GetOwnMovementModifier(firingUnit);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from own movement", Type = AttackLogEntryType.Calculation, Number = modifierOwnMovement });

            var modifierFeatures = GetFeatureModifier(targetUnit, weapon, mode);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from weapon features", Type = AttackLogEntryType.Calculation, Number = modifierFeatures });

            var modifierWeather = GetWeatherModifier(options, weapon);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from weather effects", Type = AttackLogEntryType.Calculation, Number = modifierWeather });

            var modifierQuirks = GetQuirkModifier(firingUnit, targetUnit, weapon);

            attackLog.Append(new AttackLogEntry { Context = "Hit modifier from unit quirks", Type = AttackLogEntryType.Calculation, Number = modifierQuirks });

            var modifierTotal = modifierBase +
                                   modifierHeat +
                                   firingUnit.Penalty +
                                   firingUnit.FiringSolution.AttackModifier +
                                   modifierRange +
                                   modifierWeapon +
                                   modifierUnitType +
                                   modifierCover +
                                   modifierMovementDirection +
                                   modifierMovementClass +
                                   modifierMovement +
                                   modifierOwnMovement +
                                   modifierFeatures +
                                   modifierWeather +
                                   modifierQuirks;
            
            attackLog.Append(new AttackLogEntry { Context = "Target number", Type = AttackLogEntryType.Calculation, Number = modifierTotal });

            return (modifierTotal, rangeBracket);
        }

        private int GetQuirkModifier(UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon)
        {
            if (firingUnit.HasQuirk(Quirk.TargetingAntiAir))
            {
                switch (targetUnit.Type)
                {
                    case UnitType.AerospaceCapital:
                    case UnitType.AerospaceDropship:
                    case UnitType.AerospaceFighter:
                    case UnitType.VehicleVtol:
                        return -2;
                }
            }

            if (firingUnit.HasQuirk(Quirk.BattleFists) && weapon.AttackType == AttackType.Punch)
            {
                return -1;
            }

            return 0;
        }

        private int GetWeatherModifier(GameOptions options, Weapon weapon)
        {
            var penalty = options.PenaltyAll;

            switch (weapon.Type)
            {
                case WeaponType.Ballistic:
                    penalty += options.PenaltyBallistic;
                    break;
                case WeaponType.Energy:
                    penalty += options.PenaltyEnergy;
                    break;
                case WeaponType.Melee:
                    break;
                case WeaponType.Missile:
                    penalty += options.PenaltyMissile;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(weapon), "Invalid weapon type.");
            }

            return penalty;
        }

        private int GetFeatureModifier(UnitEntry targetUnit, Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Flak, out var flakFeatureEntry))
            {
                switch (targetUnit.Type)
                {
                    case UnitType.AerospaceCapital:
                    case UnitType.AerospaceDropship:
                    case UnitType.AerospaceFighter:
                    case UnitType.VehicleVtol:
                        return _mathExpression.Parse(flakFeatureEntry.Data);
                }
            }

            return 0;
        }

        private int GetWeaponModifier(Weapon weapon, WeaponMode mode)
        {
            return weapon.HitModifier[mode];
        }

        private int GetOwnMovementModifier(UnitEntry firingUnit)
        {
            switch (firingUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    return firingUnit.MovementClass == MovementClass.OutOfControl || firingUnit.MovementClass == MovementClass.Fast ? 2 : 0;
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    switch (firingUnit.MovementClass)
                    {
                        case MovementClass.Normal:
                            return 1;
                        case MovementClass.Fast:
                        case MovementClass.Masc:
                            return firingUnit.HasQuirk(Quirk.StabilizedWeapons) ? 1 : 2;
                        case MovementClass.Jump:
                            return 3;
                        default:
                            return 0;
                    }
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleVtol:
                case UnitType.VehicleWheeled:
                    switch (firingUnit.MovementClass)
                    {
                        case MovementClass.Normal:
                            return 1;
                        case MovementClass.Fast:
                        case MovementClass.Masc:
                            return firingUnit.HasQuirk(Quirk.StabilizedWeapons) ? 1 : 2;
                        default:
                            return 0;
                    }
                default:
                    return 0;
            }
        }

        private int GetMovementClassModifier(UnitEntry targetUnit, Weapon weapon, WeaponMode mode)
        {
            // Missile weapons ignore attacker movement modifier if the target is tagged
            if (weapon.Type == WeaponType.Missile && targetUnit.Tagged && weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.IndirectFire, out _))
            {
                return 0;
            }

            if (targetUnit.MovementClass == MovementClass.Immobile)
            {
                return -4;
            }

            switch (targetUnit.Type)
            {
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    if (targetUnit.MovementClass == MovementClass.Jump)
                    {
                        return targetUnit.HasQuirk(Quirk.NimbleJumper) ? 2 : 1;
                    }

                    return 0;
                default:
                    return 0;
            }
        }

        private int GetMovementModifier(UnitEntry targetUnit, Weapon weapon, WeaponMode mode)
        {
            // Missile weapons ignore defender movement modifier if the target is tagged
            if (weapon.Type == WeaponType.Missile && targetUnit.Tagged && weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.IndirectFire, out _))
            {
                return 0;
            }

            switch (targetUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    return 0;
                default:
                    return _movementModifierArray[Math.Clamp(targetUnit.Movement, 0, 25)];
            }
            
        }

        private static int GetCoverModifier(UnitEntry firingUnit, UnitEntry targetUnit)
        {
            switch (targetUnit.Type)
            {
                case UnitType.Mech:
                    return firingUnit.FiringSolution.Cover != Cover.None ? 1 : 0;
                default:
                    return 0;
            }
        }

        private static int GetMovementDirectionModifier(UnitEntry firingUnit, UnitEntry targetUnit)
        {
            switch (targetUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    switch (firingUnit.FiringSolution.Direction)
                    {
                        case Direction.Front:
                            return 1;
                        case Direction.Left:
                        case Direction.Right:
                        case Direction.Bottom:
                        case Direction.Top:
                            return 2;
                    }
                    return 0;
                default:
                    return 0;
            }
        }

        private static int GetUnitTypeModifier(UnitEntry unit)
        {
            switch (unit.Type)
            {
                case UnitType.VehicleVtol:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int GetRangeModifier(UnitEntry firingUnit, RangeBracket rangeBracket)
        {
            switch (rangeBracket)
            {
                case RangeBracket.PointBlank:
                    switch (firingUnit.Type)
                    {
                        case UnitType.BattleArmor:
                            return 0;
                        case UnitType.Infantry:
                            return -2;
                        default:
                            return LogicConstants.InvalidTargetNumber; // Point blank weapon attacks are only allowed for Infantry and Battle Armor
                    }
                case RangeBracket.Short:
                    if (firingUnit.HasQuirk(Quirk.TargetingShortRange))
                    {
                        return -1;
                    }
                    return 0;
                case RangeBracket.Medium:
                    if (firingUnit.HasQuirk(Quirk.TargetingMediumRange))
                    {
                        return 1;
                    }
                    return 2;
                case RangeBracket.Long:
                    if (firingUnit.HasQuirk(Quirk.TargetingLongRange))
                    {
                        return 3;
                    }
                    return 4;
                case RangeBracket.Extreme:
                    if (firingUnit.HasQuirk(Quirk.TargetingExtremeRange))
                    {
                        return 5;
                    }
                    return 6;
                case RangeBracket.OutOfRange:
                    return LogicConstants.InvalidTargetNumber; // Out-of-range weapon attacks are not allowed
                default:
                    throw new ArgumentOutOfRangeException(nameof(rangeBracket), rangeBracket, "Penalty for this range bracket could not be determined.");
            }
        }

        public static RangeBracket GetRangeBracket(UnitEntry firingUnit, Weapon weapon)
        {
            switch (firingUnit.Type)
            {
                case UnitType.AerospaceCapital:
                    return GetRangeBracketAerospace(weapon, firingUnit.FiringSolution.Distance, 2);
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    return GetRangeBracketAerospace(weapon, firingUnit.FiringSolution.Distance);
                default:
                    return GetRangeBracketGround(weapon, firingUnit.FiringSolution.Distance);
            }
        }

        /// <summary>
        /// Determine the range bracket for an aerospace weapon.
        /// </summary>
        /// <param name="weapon">The weapon in use.</param>
        /// <param name="distance">Distance to target.</param>
        /// <param name="rangeMultiplier">Range multiplier for unit. Should be 1 for fighters and dropships and 2 for capital vessels.</param>
        /// <remarks>Yes, this looks like hardcoded shit, but it's because the ranges for aerospace are hardcoded in the rules.</remarks>
        /// <returns>The Range bracket that the target is in.</returns>
        private static RangeBracket GetRangeBracketAerospace(Weapon weapon, int distance, int rangeMultiplier = 1)
        {
            int outOfRange;

            switch (weapon.RangeAerospace)
            {
                case RangeBracket.Short:
                    outOfRange = 6 * rangeMultiplier + 1;
                    break;
                case RangeBracket.Medium:
                    outOfRange = 12 * rangeMultiplier + 1;
                    break;
                case RangeBracket.Long:
                    outOfRange = 20 * rangeMultiplier + 1;
                    break;
                case RangeBracket.Extreme:
                    outOfRange = 25 * rangeMultiplier + 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Aerospace range should not be of type {weapon.RangeAerospace}.");
            }

            if (distance >= outOfRange)
            {
                return RangeBracket.OutOfRange;
            }

            if (distance <= 6 * rangeMultiplier)
            {
                return RangeBracket.Short;
            }

            if (distance <= 12 * rangeMultiplier)
            {
                return RangeBracket.Medium;
            }

            if (distance <= 20 * rangeMultiplier)
            {
                return RangeBracket.Long;
            }

            if (distance <= 25 * rangeMultiplier)
            {
                return RangeBracket.Extreme;
            }

            throw new InvalidOperationException($"Range was unable to be determined for weapon-distance-rangeMultiplier {weapon.Name}-{distance}-{rangeMultiplier}.");
        }

        /// <summary>
        /// Determine the range bracket for a ground weapon.
        /// </summary>
        /// <param name="weapon">The weapon to use.</param>
        /// <param name="distance">The distance to target.</param>
        private static RangeBracket GetRangeBracketGround(Weapon weapon, int distance)
        {
            // Allow short range to be selected if point blank and short ranges are the same
            if (distance <= weapon.Range[RangeBracket.PointBlank] && weapon.Range[RangeBracket.PointBlank] < weapon.Range[RangeBracket.Short])
            {
                return RangeBracket.PointBlank;
            }

            if (distance <= weapon.Range[RangeBracket.Short])
            {
                return RangeBracket.Short;
            }

            if (distance <= weapon.Range[RangeBracket.Medium])
            {
                return RangeBracket.Medium;
            }

            if (distance <= weapon.Range[RangeBracket.Long])
            {
                return RangeBracket.Long;
            }

            if (distance <= weapon.Range[RangeBracket.Extreme])
            {
                return RangeBracket.Extreme;
            }

            return RangeBracket.OutOfRange;
        }

        /// <summary>
        /// Get penalty caused by minimum range
        /// </summary>
        /// <param name="firingUnit">The firing unit.</param>
        /// <param name="weapon">The weapon used.</param>
        /// <param name="mode">The weapon usage mode.</param>
        /// <returns></returns>
        private static int GetMinimumRangeModifier(UnitEntry firingUnit, Weapon weapon, WeaponMode mode)
        {
            switch (firingUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    return 0;
                default:
                    if (firingUnit.FiringSolution.Distance <= weapon.RangeMinimum?[mode])
                    {
                        return weapon.RangeMinimum[mode] - firingUnit.FiringSolution.Distance + 1;
                    }
                    return 0;
            }
        }
    }
}