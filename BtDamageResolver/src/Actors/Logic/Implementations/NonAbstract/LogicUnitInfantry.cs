using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for infantry units.
    /// </summary>
    public class LogicUnitInfantry : LogicUnit
    {
        public LogicUnitInfantry(ILogger<LogicUnitInfantry> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override bool CanTakeEmpHits()
        {
            return false;
        }

        /// <inheritdoc />
        public override int GetMovementClassModifier()
        {
            return GetMovementClassJumpCapable();
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.Trooper;
        }

        /// <inheritdoc />
        protected override int GetRangeModifierPointBlank()
        {
            return -2;
        }

        /// <inheritdoc />
        protected override List<DamagePacket> ResolveDamagePackets(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damage)
        {
            return Clusterize(1, 2, damage, combatAction.Weapon.SpecialDamage[combatAction.WeaponMode]);
        }

        /// <inheritdoc />
        protected override async Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            return await RapidFireWrapper(damageReport, target, combatAction, ResolveTotalOutgoingDamageInternalInfantry(damageReport, target, combatAction));
        }

        private async Task<int> ResolveTotalOutgoingDamageInternalInfantry(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            var clusterTable = await LogicHelper.GrainFactory.GetClusterTableRepository().Get(combatAction.Weapon.ClusterTable);
            var damage = clusterTable.GetDamage(Unit.Troopers);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = $"Cluster table reference for {Unit.Troopers} troopers", Number = damage });

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterBonus = ResolveClusterBonus(damageReport, target, combatAction);

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });
                return await ResolveClusterValue(damageReport, target, combatAction, damage, clusterBonus);
            }

            throw new ArgumentOutOfRangeException(combatAction.Weapon.Name, "All infantry weapons should be cluster weapons.");
        }

        /// <inheritdoc />
        public override async Task<int> TransformDamage(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            // Battle armor units have special rules when damaging infantry.
            // Typically infantry damage does not care about the number of hits a weapon does, but battle armor unit attacks are resolved individually.
            if (combatAction.UnitType == UnitType.BattleArmor)
            {
                if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Burst, out var battleArmorBurstFeatureEntry))
                {
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Troopers with burst weapons attack infantry individually" });
                    var hits = await ResolveClusterValue(damageReport, this, combatAction, combatAction.Troopers, 0);
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Troopers hit count with AP weapons against infantry", Number = hits });

                    var burstDamage = 0;
                    for (int ii = 0; ii < hits; ii++)
                    {
                        var addDamage = LogicHelper.MathExpression.Parse(battleArmorBurstFeatureEntry.Data);
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Bonus damage to infantry", Number = addDamage });
                        burstDamage += addDamage;
                    }

                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total bonus damage to infantry", Number = burstDamage });

                    return burstDamage;
                }

                return damageAmount;
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Burst, out var burstFeatureEntry))
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Burst fire weapon overrides infantry damage.", });
                var burstDamage = LogicHelper.MathExpression.Parse(burstFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Burst fire weapon damage to infantry", Number = burstDamage });
                return burstDamage;
            }

            if (combatAction.Weapon.Type == WeaponType.Missile)
            {
                var missileDamage = (int)Math.Ceiling(damageAmount / 5m);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Missile damage to infantry", Number = missileDamage });
                return missileDamage;
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Pulse, out _))
            {
                var pulseDamage = (int)Math.Ceiling(damageAmount / 10m) + 2;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Pulse weapon damage to infantry", Number = pulseDamage });
                return pulseDamage;
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterDamage = (int)Math.Ceiling(damageAmount / 10m) + 1;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Cluster weapon damage to infantry", Number = clusterDamage });
                return clusterDamage;
            }

            var transformedDamage = (int)Math.Ceiling(damageAmount / 10m);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed regular weapon damage to infantry", Number = transformedDamage });

            return transformedDamage;
        }
    }
}
