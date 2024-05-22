using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Heat calculation parts of unit logic.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc/>
    public async Task<(decimal Estimate, int Max)> ProjectHeat(int targetNumber, RangeBracket rangeBracket, WeaponEntry weaponEntry)
    {
        var hitChance = GetHitChanceForTargetNumber(targetNumber);

        if (!IsHeatTracking() || hitChance == 0m)
        {
            return (0m, 0);
        }

        int heatGenerated;

        var weapon = await FormWeapon(weaponEntry);

        if (weapon.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
        {
            heatGenerated = MathExpression.Parse(rapidFeatureEntry.Data) * weapon.Heat[rangeBracket];
        }
        else
        {
            heatGenerated = weapon.Heat[rangeBracket];
        }

        if (weapon.HasFeature(WeaponFeature.Streak, out var _))
        {
            return (hitChance * heatGenerated, heatGenerated);
        }
        else
        {
            return (heatGenerated, heatGenerated);
        }
    }

    /// <inheritdoc/>
    public async Task<DamageReport> ResolveNonWeaponHeat()
    {
        // Create a movement damage report
        var nonWeaponDamageReport = new DamageReport
        {
            Phase = Phase.Movement,
            DamagePaperDoll = await GetDamagePaperDoll(this, AttackType.Normal, Direction.Front, new List<WeaponFeature>()),
            FiringUnitId = Unit.Id,
            FiringUnitName = Unit.Name,
            TargetUnitId = Unit.Id,
            TargetUnitName = Unit.Name,
            InitialTroopers = Unit.Troopers
        };

        switch (Unit.MovementClass)
        {
            case MovementClass.Immobile:
            case MovementClass.Stationary:
                nonWeaponDamageReport.AttackerHeat += 0;
                break;
            case MovementClass.Normal:
                nonWeaponDamageReport.AttackerHeat += 1;
                break;
            case MovementClass.Fast:
                nonWeaponDamageReport.AttackerHeat += 2;
                break;
            case MovementClass.Masc:
                nonWeaponDamageReport.AttackerHeat += 5;
                break;
            case MovementClass.OutOfControl:
                nonWeaponDamageReport.AttackerHeat += 2;
                break;
            case MovementClass.Jump:
                nonWeaponDamageReport.AttackerHeat += Math.Max(3, Unit.Movement);
                break;
            default:
                throw new ArgumentOutOfRangeException($"The movement class {Unit.MovementClass} is not handled.");
        }

        nonWeaponDamageReport.Log(new AttackLogEntry { Context = $"Unit movement class {Unit.MovementClass}", Number = nonWeaponDamageReport.AttackerHeat, Type = AttackLogEntryType.Heat });

        // Combat computer sinks 4 heat by itself
        if (Unit.HasFeature(UnitFeature.CombatComputer))
        {
            nonWeaponDamageReport.Log(new AttackLogEntry { Context = $"Combat computer", Number = -4, Type = AttackLogEntryType.Heat });
            nonWeaponDamageReport.AttackerHeat -= 4;
        }

        return nonWeaponDamageReport;
    }

    /// <summary>
    /// Resolves the heat generation of a combat action.
    /// </summary>
    /// <param name="hitCalculationDamageReport">The damage report for hit calculation.</param>
    /// <param name="combatAction">The combat action to process the heat for.</param>
    protected void ResolveHeat(DamageReport hitCalculationDamageReport, CombatAction combatAction)
    {
        if (!combatAction.ActionHappened)
        {
            hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"Combat action was canceled for {combatAction.Weapon.Name} and no heat will be generated", Type = AttackLogEntryType.Information });
            return;
        }

        if (!IsHeatTracking())
        {
            hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"Units of type {Unit.Type} do not track heat, so {combatAction.Weapon.Name} causes no heat.", Type = AttackLogEntryType.Information });
            return;
        }

        int calculatedSingleHitheat = ResolveHeatForSingleHit(combatAction);
        int heat;

        if (combatAction.Weapon.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
        {
            var multiplier = MathExpression.Parse(rapidFeatureEntry.Data);
            heat = calculatedSingleHitheat * multiplier;
            hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} rate of fire multiplier for heat", Number = multiplier, Type = AttackLogEntryType.Calculation });
        }
        else
        {
            heat = calculatedSingleHitheat;
        }

        hitCalculationDamageReport.Log(new AttackLogEntry { Context = combatAction.Weapon.Name, Type = AttackLogEntryType.Heat, Number = heat });
        hitCalculationDamageReport.AttackerHeat += heat;
    }

    /// <summary>
    /// Resolve heat for a single hit instance.
    /// </summary>
    /// <remarks>
    /// Version for normal units, which do not have weapons that have varying heat values for different ranges.
    /// Aerospace units may have weapon bays where certain versions of weapons fire for different ranges and produce different amounts of heat.
    /// </remarks>
    /// <param name="combatAction">The combat action to resolve heat for.</param>
    /// <returns>The heat produced.</returns>
    protected virtual int ResolveHeatForSingleHit(CombatAction combatAction)
    {
        return combatAction.Weapon.Heat[RangeBracket.Short];
    }
}