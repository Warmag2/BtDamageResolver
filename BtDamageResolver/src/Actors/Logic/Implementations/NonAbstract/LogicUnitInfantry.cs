using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
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
/// Logic class for infantry units.
/// </summary>
public class LogicUnitInfantry : LogicUnitTrooper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitInfantry"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    public LogicUnitInfantry(ILogger<LogicUnitInfantry> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.Trooper;
    }

    /// <inheritdoc />
    public override int GetStanceModifier()
    {
        switch (Unit.Stance)
        {
            case Stance.DugIn:
                return 2;
            case Stance.Prone:
            case Stance.Light:
            case Stance.Hardened:
            case Stance.Heavy:
                return 1;
            default:
                return 0;
        }
    }

    /// <inheritdoc />
    public override async Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        // Battle armor units have special rules when damaging infantry.
        // Typically infantry damage does not care about the number of hits a weapon does, but battle armor unit attacks are resolved individually.
        if (combatAction.UnitType == UnitType.BattleArmor)
        {
            if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Burst, out var battleArmorBurstFeatureEntry))
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Troopers with burst weapons attack infantry individually" });
                var hits = await ResolveClusterValue(damageReport, this, combatAction, combatAction.Troopers, 0);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Troopers hit count with AP weapons against infantry", Number = hits });

                var burstDamage = 0;
                for (int ii = 0; ii < hits; ii++)
                {
                    var addDamage = MathExpression.Parse(battleArmorBurstFeatureEntry.Data);
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Bonus damage to infantry", Number = addDamage });
                    burstDamage += addDamage;
                }

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total bonus damage to infantry", Number = burstDamage });

                return burstDamage;
            }

            return damage;
        }

        // Infantry units have special rules when damaging other infantry.
        if (combatAction.UnitType == UnitType.Infantry)
        {
            if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Burst, out var infantryBurstFeatureEntry))
            {
                var burstDamage = MathExpression.Parse(infantryBurstFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Burst weapon bonus damage to infantry", Number = burstDamage });

                return damage + burstDamage;
            }

            return damage;
        }

        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Burst, out var burstFeatureEntry))
        {
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Burst fire weapon overrides infantry damage.", });
            var burstDamage = MathExpression.Parse(burstFeatureEntry.Data);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Burst fire weapon damage to infantry", Number = burstDamage });
            return burstDamage;
        }

        if (combatAction.Weapon.Type == WeaponType.Missile)
        {
            var missileDamage = (int)Math.Ceiling(damage / 5m);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Missile damage to infantry", Number = missileDamage });
            return missileDamage;
        }

        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Pulse, out _))
        {
            var pulseDamage = (int)Math.Ceiling(damage / 10m) + 2;
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Pulse weapon damage to infantry", Number = pulseDamage });
            return pulseDamage;
        }

        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Cluster, out _))
        {
            var clusterDamage = (int)Math.Ceiling(damage / 10m) + 1;
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Cluster weapon damage to infantry", Number = clusterDamage });
            return clusterDamage;
        }

        var transformedDamage = (int)Math.Ceiling(damage / 10m);
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed regular weapon damage to infantry", Number = transformedDamage });

        return transformedDamage;
    }

    /// <inheritdoc />
    protected override int GetRangeModifierPointBlank()
    {
        return -2;
    }

    /// <inheritdoc />
    protected override List<DamagePacket> ResolveDamagePackets(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damage)
    {
        return Clusterize(2, damage, combatAction.Weapon.SpecialDamage);
    }

    /// <inheritdoc />
    protected override async Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        return await RapidFireWrapper(damageReport, target, combatAction, ResolveTotalOutgoingDamageInternalInfantry(damageReport, target, combatAction));
    }

    private async Task<int> ResolveTotalOutgoingDamageInternalInfantry(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        var clusterTable = await GrainFactory.GetClusterTableRepository().Get(combatAction.Weapon.ClusterTable);
        var damage = clusterTable.GetDamage(Unit.Troopers);
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = $"Cluster table reference for {Unit.Troopers} troopers", Number = damage });

        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Cluster, out _))
        {
            var clusterBonus = ResolveClusterBonus(damageReport, target, combatAction);

            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });
            return await ResolveClusterValue(damageReport, target, combatAction, damage, clusterBonus);
        }

        throw new ArgumentOutOfRangeException(combatAction.Weapon.Name, "All infantry weapons should be cluster weapons.");
    }
}