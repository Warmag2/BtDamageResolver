using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using System;
using System.Threading.Tasks;

using static Faemiyah.BtDamageResolver.Actors.Logic.Helpers.LogicCombatHelpers;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Partial unit logic class for clustering calculations.
    /// </summary>
    public partial class LogicUnit
    {
        protected int ResolveClusterBonus(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            var clusterBonus = combatAction.Weapon.Type == WeaponType.Missile ?
                ResolveClusterBonusMissile(damageReport, target, combatAction) :
                ResolveClusterBonusProjectile(damageReport, combatAction);

            if (target.IsGlancingBlow(combatAction))
            {
                var clusterBonusGlancing = -4;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster bonus from glancing blow", Number = clusterBonusGlancing });
                clusterBonus += clusterBonusGlancing;
            }

            return clusterBonus;
        }

        private int ResolveClusterBonusMissile(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            var clusterBonus = 0;

            clusterBonus += combatAction.Weapon.ClusterBonus[combatAction.WeaponMode];
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from weapon", Number = combatAction.Weapon.ClusterBonus[combatAction.WeaponMode] });

            if (target.GetUnit().HasFeature(UnitFeature.Ams))
            {
                if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.AmsImmune, out _))
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

            if (target.GetUnit().HasFeature(UnitFeature.Ecm) && !Unit.HasFeature(UnitFeature.Bap))
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender ECM", Number = -2 });
                clusterBonus -= 2;
            }

            if (target.GetUnit().Narced)
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender being NARCed", Number = 2 });
                clusterBonus += 2;
            }

            return clusterBonus;
        }

        private static int ResolveClusterBonusProjectile(DamageReport damageReport, CombatAction combatAction)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Hag, out _))
            {
                switch (combatAction.RangeBracket) // Only HAG has cluster bonus for projectile weapons and it is treated differently depending on range
                {
                    case RangeBracket.PointBlank:
                    case RangeBracket.Short:
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "HAG cluster modifier from short range", Number = 2 });
                        return 2;
                    case RangeBracket.Medium:
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "HAG cluster modifier from normal range", Number = 0 });
                        return 0;
                    case RangeBracket.Long:
                    case RangeBracket.Extreme:
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "HAG cluster modifier from long range", Number = -2 });
                        return -2;
                    default:
                        throw new InvalidOperationException($"RangeBracket of cluster damage calculation is invalid: {combatAction.RangeBracket}");
                }
            }

            return 0;
        }

        protected async Task<int> ResolveClusterValue(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damageValue, int clusterBonus)
        {
            int clusterRoll = ResolveClusterRoll(damageReport, combatAction);

            clusterRoll = Math.Clamp(clusterRoll + clusterBonus, 2, 12);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Modified cluster", Number = clusterRoll });

            var damageTable = await LogicHelper.GrainFactory.GetClusterTableRepository().Get(Names.DefaultClusterTableName);

            var clusterDamage = damageTable.GetDamage(damageValue, clusterRoll);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster result", Number = clusterDamage });

            return clusterDamage;
        }

        private int ResolveClusterRoll(DamageReport damageReport, CombatAction combatAction)
        {
            int clusterRoll;

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Streak, out _))
            {
                clusterRoll = 11;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Static cluster roll value by a streak weapon", Number = clusterRoll });
            }
            else
            {
                clusterRoll = LogicHelper.Random.D26();
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Cluster", Number = clusterRoll });
            }

            return clusterRoll;
        }
    }
}
