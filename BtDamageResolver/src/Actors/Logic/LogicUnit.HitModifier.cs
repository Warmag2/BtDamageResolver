using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Top-level abstract logic class for hit modifier resolution.
/// </summary>
public abstract partial class LogicUnit
{
    private static readonly int[] MovementModifierArray = [0, 0, 0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 6];

    /// <inheritdoc/>
    public async Task<(int TargetNumber, RangeBracket RangeBracket)> ResolveHitModifier(AttackLog attackLog, ILogicUnit target, WeaponEntry weaponEntry, WeaponBay weaponBay, bool isPrimaryTarget)
    {
        return ResolveHitModifier(attackLog, target, await FormWeapon(weaponEntry), weaponBay, isPrimaryTarget);
    }

    /// <inheritdoc />
    public (int TargetNumber, RangeBracket RangeBracket) ResolveHitModifier(AttackLog attackLog, ILogicUnit target, Weapon weapon, WeaponBay weaponBay, bool isPrimaryTarget)
    {
        var modifierBase = weapon.AttackType == AttackType.Normal ? Unit.Gunnery : Unit.Piloting;

        // NOTE: In this method, the damageReport may be null, as the target number calculation does not need a damage log. Everywhere else, it must exist.
        attackLog.Append(new AttackLogEntry { Context = "Base hit modifier", Type = AttackLogEntryType.Calculation, Number = modifierBase });

        var modifierMultiTarget = GetMultiTargetModifier(weaponBay, isPrimaryTarget);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from multiple targets", Type = AttackLogEntryType.Calculation, Number = modifierMultiTarget });

        var modifierHeat = Unit.GetHeatAttackPenalty();

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from heat effects", Type = AttackLogEntryType.Calculation, Number = modifierHeat });

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from EMP effects", Type = AttackLogEntryType.Calculation, Number = Unit.Penalty });

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from firing solution", Type = AttackLogEntryType.Calculation, Number = weaponBay.FiringSolution.AttackModifier });

        var rangeBracket = GetRangeBracket(weapon, weaponBay);
        var modifierRange = GetRangeModifier(rangeBracket);
        modifierRange += GetMinimumRangeModifier(weapon, weaponBay);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from range", Type = AttackLogEntryType.Calculation, Number = modifierRange });

        var modifierArmor = GetArmorModifier(rangeBracket, target);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from target armor", Type = AttackLogEntryType.Calculation, Number = modifierArmor });

        var modifierWeapon = GetWeaponModifier(weapon);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from weapon properties", Type = AttackLogEntryType.Calculation, Number = modifierWeapon });

        var modifierUnitType = target.GetUnitTypeModifier();

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from unit type", Type = AttackLogEntryType.Calculation, Number = modifierUnitType });

        var modifierCover = target.GetCoverModifier(weaponBay.FiringSolution.Cover);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from cover", Type = AttackLogEntryType.Calculation, Number = modifierCover });

        var modifierStance = target.GetStanceModifier();

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from target stance", Type = AttackLogEntryType.Calculation, Number = modifierStance });

        var modifierMovementDirection = target.GetMovementDirectionModifier(weaponBay.FiringSolution.Direction);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from movement direction", Type = AttackLogEntryType.Calculation, Number = modifierMovementDirection });

        var modifierMovementClass = GetMovementClassModifier(target, weapon);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from target movement class", Type = AttackLogEntryType.Calculation, Number = modifierMovementClass });

        var modifierMovement = GetMovementModifierBase(target, weapon);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from target movement amount", Type = AttackLogEntryType.Calculation, Number = modifierMovement });

        var modifierEvasion = target.GetEvasionModifier();

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from target evasion", Type = AttackLogEntryType.Calculation, Number = modifierEvasion });

        var modifierOwnMovement = GetOwnMovementModifier();

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from own movement", Type = AttackLogEntryType.Calculation, Number = modifierOwnMovement });

        var modifierOwnEvasion = GetOwnEvasionModifier(weapon);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from own evasion", Type = AttackLogEntryType.Calculation, Number = modifierOwnEvasion });

        var modifierFeatures = target.GetFeatureModifier(weapon);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from weapon features", Type = AttackLogEntryType.Calculation, Number = modifierFeatures });

        var modifierWeather = GetWeatherModifier(weapon);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from weather effects", Type = AttackLogEntryType.Calculation, Number = modifierWeather });

        var modifierQuirks = GetQuirkModifier(target, weapon);

        attackLog.Append(new AttackLogEntry { Context = "Hit modifier from unit quirks", Type = AttackLogEntryType.Calculation, Number = modifierQuirks });

        var modifierTotal =
            modifierBase +
            modifierMultiTarget +
            modifierHeat +
            Unit.Penalty +
            weaponBay.FiringSolution.AttackModifier +
            modifierRange +
            modifierArmor +
            modifierWeapon +
            modifierUnitType +
            modifierCover +
            modifierStance +
            modifierMovementDirection +
            modifierMovementClass +
            modifierMovement +
            modifierEvasion +
            modifierOwnMovement +
            modifierOwnEvasion +
            modifierFeatures +
            modifierWeather +
            modifierQuirks;

        attackLog.Append(new AttackLogEntry { Context = "Target number", Type = AttackLogEntryType.Calculation, Number = modifierTotal });

        return (modifierTotal, rangeBracket);
    }

    /// <inheritdoc />
    public virtual int GetCoverModifier(Cover cover)
    {
        return 0;
    }

    /// <inheritdoc />
    public virtual int GetEvasionModifier()
    {
        return 0;
    }

    /// <inheritdoc />
    public virtual int GetFeatureModifier(Weapon weapon)
    {
        return 0;
    }

    /// <inheritdoc />
    public virtual int GetMovementClassModifierBasedOnUnitType()
    {
        return GetMovementClassModifierInternal();
    }

    /// <inheritdoc />
    public virtual int GetMovementDirectionModifier(Direction direction)
    {
        return 0;
    }

    /// <inheritdoc />
    public virtual int GetMovementModifier()
    {
        return MovementModifierArray[Math.Clamp(Unit.Movement, 0, 25)];
    }

    /// <inheritdoc />
    public virtual int GetStanceModifier()
    {
        return 0;
    }

    /// <inheritdoc />
    public virtual int GetUnitTypeModifier()
    {
        return 0;
    }

    /// <summary>
    /// Determine the range bracket for an aerospace weapon.
    /// </summary>
    /// <param name="weapon">The weapon in use.</param>
    /// <param name="distance">Distance to target.</param>
    /// <remarks>Yes, this looks like hardcoded shit, but it's because the ranges for aerospace are hardcoded in the rules.</remarks>
    /// <returns>The Range bracket that the target is in.</returns>
    protected static RangeBracket GetRangeBracketAerospace(Weapon weapon, int distance)
    {
        int outOfRange;

        var rangeMultiplier = weapon.CapitalScale ? 2 : 1;

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

        return RangeBracket.Extreme;
    }

    /// <summary>
    /// Get penalty caused by minimum range.
    /// </summary>
    /// <param name="weapon">The weapon used.</param>
    /// <param name="weaponBay">The weapon bay the weapon is in.</param>
    /// <returns>The modifier for firing below weapon minimum range.</returns>
    protected virtual int GetMinimumRangeModifier(Weapon weapon, WeaponBay weaponBay)
    {
        if (weaponBay.FiringSolution.Distance <= weapon.RangeMinimum)
        {
            return weapon.RangeMinimum - weaponBay.FiringSolution.Distance + 1;
        }

        return 0;
    }

    /// <summary>
    /// Gets the movement class modifier for a jump-capable mech.
    /// </summary>
    /// <returns>The movement class modifier for a jump-capable mech.</returns>
    protected int GetMovementClassJumpCapable()
    {
        if (Unit.MovementClass == MovementClass.Jump)
        {
            return Unit.HasFeature(UnitFeature.NimbleJumper) ? 2 : 1;
        }

        return GetMovementClassModifierInternal();
    }

    /// <summary>
    /// Gets the hit modifier to movement from the units own movement.
    /// </summary>
    /// <returns>The movement modifier from the units own movement.</returns>
    protected virtual int GetOwnMovementModifier()
    {
        return 0;
    }

    /// <summary>
    /// Gets the range bracket for a weapon in the context of this unit and a specific weapon bay.
    /// </summary>
    /// <param name="weapon">The weapon to get the range bracket for.</param>
    /// <param name="weaponBay">The weapon bay to get the range bracket for.</param>
    /// <returns>The range bracket for a weapon in the context of this unit.</returns>
    protected virtual RangeBracket GetRangeBracket(Weapon weapon, WeaponBay weaponBay)
    {
        return GetRangeBracketGround(weapon, weaponBay.FiringSolution.Distance);
    }

    /// <summary>
    /// Gets the modifier to hit from firing point-blank.
    /// </summary>
    /// <returns>The modifier to hit from firing point-blank.</returns>
    protected virtual int GetRangeModifierPointBlank()
    {
        // Point blank weapon attacks are only allowed for Infantry and Battle Armor.
        // Therefore, default to invalid attack.
        return LogicConstants.InvalidTargetNumber;
    }

    private static int GetArmorModifier(RangeBracket rangeBracket, ILogicUnit target)
    {
        if (target.Unit.HasFeature(UnitFeature.ArmorStealth))
        {
            switch (rangeBracket)
            {
                case RangeBracket.Medium:
                    return 1;
                case RangeBracket.Long:
                case RangeBracket.Extreme:
                    return 2;
                default:
                    return 0;
            }
        }

        return 0;
    }

    private static int GetMovementClassModifier(ILogicUnit target, Weapon weapon)
    {
        // Missile weapons ignore attacker movement modifier if the target is tagged and they have homing munitions
        if (weapon.HasFeature(WeaponFeature.Homing, out _) && (target.Unit.Tagged || target.Unit.Narced))
        {
            return 0;
        }

        return target.GetMovementClassModifierBasedOnUnitType();
    }

    private static int GetMovementModifierBase(ILogicUnit target, Weapon weapon)
    {
        // Missile weapons ignore defender movement modifier if the target is tagged
        if (weapon.HasFeature(WeaponFeature.Homing, out _) && (target.Unit.Tagged || target.Unit.Narced))
        {
            return 0;
        }

        return target.GetMovementModifier();
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

    private static int GetWeaponModifier(Weapon weapon)
    {
        return weapon.HitModifier;
    }

    private int GetMultiTargetModifier(WeaponBay weaponBay, bool isPrimaryTarget)
    {
        if (isPrimaryTarget)
        {
            return 0;
        }

        if (Unit.HasFeature(UnitFeature.MultiTarget))
        {
            return 0;
        }

        switch (Unit.Type)
        {
            case UnitType.AerospaceCapital:
            case UnitType.AerospaceDropshipAerodyne:
            case UnitType.AerospaceDropshipSpheroid:
            case UnitType.BattleArmor:
            case UnitType.Infantry:
                return 0;
            default:
                // Front arc for secondary targets is 1 penalty, all other arcs are 2 penalty.
                switch (weaponBay.FiringSolution.Arc)
                {
                    case Arc.Front:
                        return 1;
                    default:
                        return 2;
                }
        }
    }

    private int GetOwnEvasionModifier(Weapon weapon)
    {
        switch (Unit.Type)
        {
            case UnitType.AerospaceCapital:
            case UnitType.AerospaceDropshipAerodyne:
            case UnitType.AerospaceDropshipSpheroid:
                if (weapon.HasFeature(WeaponFeature.IgnoreOwnEvasion, out _))
                {
                    return 0;
                }

                return 2;
            case UnitType.AerospaceFighter:
                return LogicConstants.InvalidTargetNumber;
            default:
                return 0;
        }
    }

    private int GetQuirkModifier(ILogicUnit target, Weapon weapon)
    {
        if (Unit.HasFeature(UnitFeature.TargetingAntiAir))
        {
            switch (target.Unit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropshipAerodyne:
                case UnitType.AerospaceDropshipSpheroid:
                case UnitType.AerospaceFighter:
                case UnitType.VehicleVtol:
                    return -2;
            }
        }

        if (Unit.HasFeature(UnitFeature.BattleFists) && weapon.AttackType == AttackType.Punch)
        {
            return -1;
        }

        return 0;
    }

    private int GetWeatherModifier(Weapon weapon)
    {
        var penalty = GameOptions.PenaltyAll;

        switch (weapon.Type)
        {
            case WeaponType.Ballistic:
                penalty += GameOptions.PenaltyBallistic;
                break;
            case WeaponType.Energy:
                penalty += GameOptions.PenaltyEnergy;
                break;
            case WeaponType.Melee:
                break;
            case WeaponType.Missile:
                penalty += GameOptions.PenaltyMissile;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(weapon), "Invalid weapon type.");
        }

        return penalty;
    }

    private int GetMovementClassModifierInternal()
    {
        if (Unit.MovementClass == MovementClass.Immobile)
        {
            return -4;
        }

        return 0;
    }

    private int GetRangeModifier(RangeBracket rangeBracket)
    {
        switch (rangeBracket)
        {
            case RangeBracket.PointBlank:
                return GetRangeModifierPointBlank();
            case RangeBracket.Short:
                if (Unit.HasFeature(UnitFeature.TargetingShortRange))
                {
                    return -1;
                }

                return 0;
            case RangeBracket.Medium:
                if (Unit.HasFeature(UnitFeature.TargetingMediumRange))
                {
                    return 1;
                }

                return 2;
            case RangeBracket.Long:
                if (Unit.HasFeature(UnitFeature.TargetingLongRange))
                {
                    return 3;
                }

                return 4;
            case RangeBracket.Extreme:
                if (Unit.HasFeature(UnitFeature.TargetingExtremeRange))
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
}