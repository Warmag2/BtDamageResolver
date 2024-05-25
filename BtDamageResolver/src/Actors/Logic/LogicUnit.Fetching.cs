using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
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
    public async Task<CriticalDamageTable> GetCriticalDamageTable(CriticalDamageTableType criticalDamageTableType, Location location)
    {
        var transformedTargetType = GetPaperDollType();

        Location transformedLocation;

        // Not self-evident, but all mech crit tables are actually just the default crit-ot-not table.
        if (Unit.Type == UnitType.Mech || Unit.Type == UnitType.Building)
        {
            transformedLocation = Location.Front;
        }
        else
        {
            transformedLocation = location;
        }

        var criticalDamageTableId = CriticalDamageTable.GetIdFromProperties(transformedTargetType, criticalDamageTableType, transformedLocation);

        return await GrainFactory.GetCriticalDamageTableRepository().Get(criticalDamageTableId);
    }

    /// <inheritdoc />
    public async Task<DamagePaperDoll> GetDamagePaperDoll(ILogicUnit target, AttackType attackType, Direction direction, List<WeaponFeature> weaponFeatures)
    {
        var paperDollName = GetPaperDollNameFromAttackParameters(target, attackType, direction, GameOptions, weaponFeatures);
        var paperDoll = await GrainFactory.GetPaperDollRepository().Get(paperDollName);

        return paperDoll.GetDamagePaperDoll();
    }

    /// <summary>
    /// Apply ammo and multiplication to a weapon.
    /// </summary>
    /// <param name="weaponEntry">The weapon entry to base the applied weapon on.</param>
    /// <returns>The <see cref="Weapon"/> with the chosen ammo in the weapon entry applied, if possible.</returns>
    protected async Task<Weapon> FormWeapon(WeaponEntry weaponEntry)
    {
        var weapon = await GrainFactory.GetWeaponRepository().Get(weaponEntry.WeaponName);

        if (!string.IsNullOrWhiteSpace(weaponEntry.Ammo) && weapon.Ammo.TryGetValue(weaponEntry.Ammo, out var value))
        {
            weapon = weapon.ApplyAmmo(await GrainFactory.GetAmmoRepository().Get(value));
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
        // Melee attacks that are not kicks or punches use normal attack tables
        var transformedAttackType = attackType == AttackType.Melee ? AttackType.Normal : attackType;

        var targetType = target.Unit.Type;

        // Punch and kick tables only exist for mechs, revert to normal for all other target types
        switch (targetType)
        {
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                // Punches and kicks to prone or crouched mechs can hit anywhere
                if (target.Unit.Stance == Stance.Prone || target.Unit.Stance == Stance.Crouch)
                {
                    transformedAttackType = AttackType.Normal;
                }

                break;
            default:
                transformedAttackType = AttackType.Normal;
                break;
        }

        return transformedAttackType;
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

        Direction transformedDirection;

        if (target.Unit.Type == UnitType.Infantry || target.Unit.Type == UnitType.BattleArmor || target.Unit.Type == UnitType.Building)
        {
            transformedDirection = Direction.Front;
        }
        else
        {
            transformedDirection = direction;
        }

        // Get alterative paperdolls based on rules
        var transformedRules = new List<Rule>();

        // Floating critical may only apply to mechs
        if (gameOptions.Rules[Rule.FloatingCritical] && transformedAttackType == AttackType.Normal)
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

        return PaperDoll.GetIdFromProperties(transformedTargetType, transformedAttackType, transformedDirection, transformedRules);
    }
}