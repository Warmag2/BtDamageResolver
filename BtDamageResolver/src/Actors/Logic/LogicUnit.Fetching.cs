using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Data fetching helper methods for unit logic.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc />
    public CriticalDamageTable GetCriticalDamageTable(CriticalDamageTableType criticalDamageTableType, Location location)
    {
        var transformedTargetType = GetPaperDollType();
        var transformedLocation = TransformLocation(location);
        var criticalDamageTableId = CriticalDamageTable.GetIdFromProperties(transformedTargetType, criticalDamageTableType, transformedLocation);

        return RepositoryProvider.CriticalDamageTableRepository.Get(criticalDamageTableId);
    }

    /// <inheritdoc />
    public DamagePaperDoll GetDamagePaperDoll(ILogicUnit target, AttackType attackType, Direction direction, List<WeaponFeature> weaponFeatures)
    {
        var paperDollName = GetPaperDollNameFromAttackParameters(target, attackType, direction, GameOptions, weaponFeatures);
        var paperDoll = RepositoryProvider.PaperDollRepository.Get(paperDollName);

        return paperDoll.ToDamagePaperDoll();
    }

    /// <summary>
    /// Apply ammo and multiplication to a weapon.
    /// </summary>
    /// <param name="weaponEntry">The weapon entry to base the applied weapon on.</param>
    /// <returns>The <see cref="Weapon"/> with the chosen ammo in the weapon entry applied, if possible.</returns>
    protected Weapon FormWeapon(WeaponEntry weaponEntry)
    {
        var weapon = RepositoryProvider.WeaponRepository.Get(weaponEntry.WeaponName);

        if (!string.IsNullOrWhiteSpace(weaponEntry.Ammo) && weapon.Ammo.TryGetValue(weaponEntry.Ammo, out var value))
        {
            weapon = weapon.ApplyAmmo(RepositoryProvider.AmmoRepository.Get(value));
        }

        if (weaponEntry.Amount > 1)
        {
            return weapon.Multiply(weaponEntry.Amount);
        }

        return weapon;
    }

    /// <summary>
    /// Transform attack type based on target properties and weapon features.
    /// </summary>
    /// <param name="target">The target unit logic.</param>
    /// <param name="attackType">The attack type.</param>
    /// <param name="weaponFeatures">The weapon features.</param>
    /// <returns>The transformed attack type.</returns>
    protected virtual AttackType TransformAttackType(ILogicUnit target, AttackType attackType, List<WeaponFeature> weaponFeatures)
    {
        var targetType = target.Unit.Type;
        var transformedAttackType = attackType;

        // Punch and kick tables only exist for mechs, revert to normal for all other target types
        // Special handling for mechs
        switch (targetType)
        {
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                // Melee attacks against crouched mechs hit punch table
                if ((attackType == AttackType.Melee || attackType == AttackType.Kick) && target.Unit.Stance is Stance.Crouch)
                {
                    transformedAttackType = AttackType.Punch;
                }

                if (weaponFeatures.Contains(WeaponFeature.MeleeDfa) && target.Unit.Stance is Stance.Prone)
                {
                    transformedAttackType = AttackType.Normal;
                }

                break;
            default:
                transformedAttackType = AttackType.Normal;
                break;
        }

        // Melee attacks that are not kicks or punches use normal attack tables
        if (transformedAttackType == AttackType.Melee)
        {
            transformedAttackType = AttackType.Normal;
        }

        return transformedAttackType;
    }

    /// <summary>
    /// Transform location based on unit type.
    /// </summary>
    /// <remarks>
    /// Required because sometimes the number of critical damage tables for an unit does not match the number
    /// of hit locations for an unit and the location needs to be transformed to match the available tables.
    /// </remarks>
    /// <param name="location">The location to transform.</param>
    /// <returns>The transformed location.</returns>
    protected virtual Location TransformLocation(Location location)
    {
        return location;
    }

    /// <summary>
    /// Helper method for paper doll selection, based on attack parameters and target type.
    /// Needed because not all units have their individual paperdoll.
    /// </summary>
    /// <param name="target">The target unit logic.</param>
    /// <param name="attackType">The type of the attack.</param>
    /// <param name="direction">The direction the attack is coming from.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="weaponFeatures">The weapon features, if any.</param>
    /// <returns>The correct paper doll name.</returns>
    private string GetPaperDollNameFromAttackParameters(ILogicUnit target, AttackType attackType, Direction direction, GameOptions gameOptions, List<WeaponFeature> weaponFeatures)
    {
        var transformedTargetType = target.GetPaperDollType();

        var transformedAttackType = TransformAttackType(target, attackType, weaponFeatures);

        var transformedDirection = TransformDirection(target, weaponFeatures, direction);

        List<Rule> transformedRules = TransformRules(target, gameOptions, transformedAttackType);

        return PaperDoll.GetIdFromProperties(transformedTargetType, transformedAttackType, transformedDirection, transformedRules);
    }

    /// <summary>
    /// Transform direction based on target properties and weapon features.
    /// </summary>
    /// <param name="target">The target unit.</param>
    /// <param name="weaponFeatures">The weapon features.</param>
    /// <param name="direction">The direction of the attack.</param>
    /// <returns>The transformed direction.</returns>
    private static Direction TransformDirection(ILogicUnit target, List<WeaponFeature> weaponFeatures, Direction direction)
    {
        switch (target.Unit.Type)
        {
            case UnitType.BattleArmor:
            case UnitType.Building:
            case UnitType.Infantry:
                direction = Direction.Front;
                break;
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                // Melee DFA against prone mechs hits the back of the mech
                if (weaponFeatures.Contains(WeaponFeature.MeleeDfa) && target.Unit.Stance is Stance.Prone)
                {
                    direction = Direction.Rear;
                }
                break;
        }

        return direction;
    }

    /// <summary>
    /// Transform rules based on target properties and attack type.
    /// </summary>
    /// <param name="target">The target unit logic.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="attackType">The attack type.</param>
    /// <returns>The transformed rules.</returns>
    private static List<Rule> TransformRules(ILogicUnit target, GameOptions gameOptions, AttackType attackType)
    {
        // Get alterative paperdolls based on rules
        var transformedRules = new List<Rule>();

        // Floating critical may only apply to mechs
        if (gameOptions.Rules[Rule.FloatingCritical] && attackType == AttackType.Normal)
        {
            switch (target.Unit.Type)
            {
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    transformedRules.Add(Rule.FloatingCritical);
                    break;
            }
        }

        // Improved vehicle survivability may only apply to vehicles
        if (gameOptions.Rules[Rule.ImprovedVehicleSurvivability])
        {
            switch (target.Unit.Type)
            {
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleWheeled:
                case UnitType.VehicleVtol:
                    transformedRules.Add(Rule.ImprovedVehicleSurvivability);
                    break;
            }
        }

        return transformedRules;
    }
}