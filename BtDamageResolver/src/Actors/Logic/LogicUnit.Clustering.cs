using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Partial unit logic class for clustering calculations.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc />
    public virtual int TransformClusterRollBasedOnUnitType(DamageReport damageReport, int clusterRoll)
    {
        return clusterRoll;
    }

    /// <summary>
    /// Resolve the cluster bonus.
    /// </summary>
    /// <param name="damageReport">The damage report to apply to.</param>
    /// <param name="target">The target unit logic.</param>
    /// <param name="combatAction">The combat action.</param>
    /// <returns>The cluster bonus.</returns>
    protected int ResolveClusterBonus(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        var clusterBonus = combatAction.Weapon.Type == WeaponType.Missile ?
            ResolveClusterBonusMissile(damageReport, target, combatAction) :
            ResolveClusterBonusNonMissile(damageReport, combatAction);

        if (target.IsGlancingBlow(combatAction.MarginOfSuccess))
        {
            var clusterBonusGlancing = -4;
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = $"Unit feature {UnitFeature.NarrowLowProfile} modifies cluster bonus calculation. New bonus", Number = clusterBonusGlancing });
            clusterBonus += clusterBonusGlancing;
        }

        return clusterBonus;
    }

    /// <summary>
    /// Resolver final cluster value.
    /// </summary>
    /// <param name="damageReport">The damage report to apply to.</param>
    /// <param name="target">The target unit logic.</param>
    /// <param name="combatAction">The combat action.</param>
    /// <param name="damageValue">The damage value.</param>
    /// <param name="clusterBonus">The cluster bonus.</param>
    /// <returns>Result of the cluster calculation.</returns>
    protected async Task<int> ResolveClusterValue(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damageValue, int clusterBonus)
    {
        int clusterRoll = Random.D26();

        clusterRoll = TransformClusterRollBasedOnWeaponFeatures(damageReport, combatAction, clusterRoll);
        clusterRoll = target.TransformClusterRollBasedOnUnitType(damageReport, clusterRoll);

        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Cluster", Number = clusterRoll });

        clusterRoll = Math.Clamp(clusterRoll + clusterBonus, 2, 12);
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Modified cluster", Number = clusterRoll });

        var damageTable = await GrainFactory.GetClusterTableRepository().Get(Names.DefaultClusterTableName);

        var clusterDamage = damageTable.GetDamage(damageValue, clusterRoll);
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster result", Number = clusterDamage });

        return clusterDamage;
    }

    private static int ResolveClusterBonusNonMissile(DamageReport damageReport, CombatAction combatAction)
    {
        // Non-missile weapons do not care about AMS or ECM
        var clusterBonus = combatAction.Weapon.ClusterBonus[combatAction.RangeBracket];

        if (clusterBonus != 0)
        {
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = $"Cluster modifier for range bracket {combatAction.RangeBracket}", Number = clusterBonus });
        }

        return clusterBonus;
    }

    private static int TransformClusterRollBasedOnWeaponFeatures(DamageReport damageReport, CombatAction combatAction, int clusterRoll)
    {
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Streak, out _))
        {
            clusterRoll = 11;
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Static cluster roll value by a streak weapon", Number = clusterRoll });
        }

        return clusterRoll;
    }

    private int ResolveClusterBonusMissile(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        var clusterBonus = 0;

        clusterBonus += combatAction.Weapon.ClusterBonus[combatAction.RangeBracket];
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from weapon", Number = combatAction.Weapon.ClusterBonus[combatAction.RangeBracket] });

        if (target.Unit.HasFeature(UnitFeature.Ams))
        {
            if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.AmsImmune, out _))
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is immune to AMS defenses" });
            }
            else
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender AMS", Number = -4 });
                clusterBonus -= 4;

                damageReport.SpendAmmoDefender("AMS", 1);
            }
        }

        if (target.Unit.HasFeature(UnitFeature.Ecm) && !Unit.HasFeature(UnitFeature.Bap))
        {
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender ECM", Number = -2 });
            clusterBonus -= 2;
        }

        if (target.Unit.Narced)
        {
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender being NARCed", Number = 2 });
            clusterBonus += 2;
        }

        return clusterBonus;
    }
}