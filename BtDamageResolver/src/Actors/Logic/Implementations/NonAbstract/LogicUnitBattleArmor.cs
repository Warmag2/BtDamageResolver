using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for battle armor.
    /// </summary>
    public class LogicUnitBattleArmor : LogicUnit
    {
        /// <inheritdoc />
        public LogicUnitBattleArmor(ILogger<LogicUnitBattleArmor> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
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
            return PaperDollType.BattleArmor;
        }

        /// <inheritdoc />
        protected override int GetRangeModifierPointBlank()
        {
            return 0;
        }

        /// <inheritdoc />
        protected override List<DamagePacket> ResolveDamagePackets(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damage)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _))
            {
                // The total missile or clusterized damage accounting for trooper amount has been calculated earlier
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total damage value modified by BA trooper amount", Number = damage });
                return Clusterize(combatAction.Weapon.ClusterDamage, combatAction.Weapon.ClusterSize, damage, combatAction.Weapon.SpecialDamage[combatAction.WeaponMode]);
            }

            // If we did not have a cluster weapon, the weapon still may have hit any amount of times due to possible rapid fire and trooper amount
            // Clusterize to hits which match the actual damage value of the weapon
            return Clusterize(damage, combatAction.Weapon.Damage[combatAction.RangeBracket], 1, combatAction.Weapon.SpecialDamage[combatAction.WeaponMode]);
        }

        /// <inheritdoc />
        protected override async Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            return await RapidFireWrapper(damageReport, target, combatAction, ResolveTotalOutgoingDamageInternalBattleArmor(damageReport, target, combatAction));
        }

        private async Task<int> ResolveTotalOutgoingDamageInternalBattleArmor(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterBonus = ResolveClusterBonus(damageReport, target, combatAction);

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });
                // The cluster damage reference value is the cluster value of all the troopers combined
                var clusterDamage = combatAction.Weapon.Damage[combatAction.RangeBracket] * Unit.Troopers;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster value from all troopers", Number = clusterDamage });
                return await ResolveClusterValue(damageReport, target, combatAction, clusterDamage, clusterBonus);
            }

            // Default damage calculation path if we did not have a cluster weapon
            // Calculate the number of hits because not all troopers necessarily hit when the squad hits
            var hits = await ResolveClusterValue(damageReport, target, combatAction, Unit.Troopers, 0);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Troopers hit count", Number = hits });
            var damage = combatAction.Weapon.Damage[combatAction.RangeBracket] * hits;
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total attack damage value", Number = damage });
            return damage;
        }

        /// <inheritdoc />
        public override Task<int> TransformDamage(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            if (Unit.HasFeature(UnitFeature.ArmorHeatResistant))
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Heat-inflicting weapon bonus damage negated by heat-resistant armor" });
                return Task.FromResult(damageAmount);
            }

            return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damageAmount));
        }
    }
}
