using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract;

/// <summary>
/// Logic class for battle armor.
/// </summary>
public class LogicUnitBattleArmor : LogicUnitTrooper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitBattleArmor"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    public LogicUnitBattleArmor(ILogger<LogicUnitBattleArmor> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.BattleArmor;
    }

    /// <inheritdoc />
    public override int GetStanceModifier()
    {
        switch (Unit.Stance)
        {
            case Stance.Light:
            case Stance.Hardened:
            case Stance.Heavy:
                return 1;
            default:
                return 0;
        }
    }

    /// <inheritdoc />
    public override Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        if (Unit.HasFeature(UnitFeature.ArmorHeatResistant))
        {
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Heat-inflicting weapon bonus damage negated by heat-resistant armor" });
            return Task.FromResult(damage);
        }

        return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damage));
    }

    /// <inheritdoc />
    protected override int GetRangeModifierPointBlank()
    {
        return 0;
    }

    /// <inheritdoc />
    protected override List<DamagePacket> ResolveDamagePackets(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damage)
    {
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Cluster, out _))
        {
            // The total missile or clusterized damage accounting for trooper amount has been calculated earlier
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total damage value modified by BA trooper amount", Number = damage });
            return Clusterize(combatAction.Weapon.ClusterSize, damage, combatAction.Weapon.SpecialDamage);
        }

        // If we did not have a cluster weapon, the weapon still may have hit any amount of times due to possible rapid fire and trooper amount
        // Clusterize to hits which match the actual damage value of the weapon
        return Clusterize(combatAction.Weapon.Damage[combatAction.RangeBracket], damage, combatAction.Weapon.SpecialDamage);
    }

    /// <inheritdoc />
    protected override async Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        return await RapidFireWrapper(damageReport, target, combatAction, ResolveTotalOutgoingDamageInternalBattleArmor(damageReport, target, combatAction));
    }

    private async Task<int> ResolveTotalOutgoingDamageInternalBattleArmor(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Cluster, out _))
        {
            var clusterBonus = ResolveClusterBonus(damageReport, target, combatAction);

            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });

            // The cluster damage reference value is the cluster value of all the troopers combined
            var clusterDamage = combatAction.Weapon.Damage[combatAction.RangeBracket] * Unit.Troopers;
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster value from all troopers", Number = clusterDamage });
            return combatAction.Weapon.ClusterDamage * (await ResolveClusterValue(damageReport, target, combatAction, clusterDamage, clusterBonus));
        }

        // Default damage calculation path if we did not have a cluster weapon
        // Calculate the number of hits because not all troopers necessarily hit when the squad hits
        var hits = await ResolveClusterValue(damageReport, target, combatAction, Unit.Troopers, 0);
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Troopers hit count", Number = hits });
        var damage = combatAction.Weapon.Damage[combatAction.RangeBracket] * hits;
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total attack damage value", Number = damage });
        return damage;
    }
}