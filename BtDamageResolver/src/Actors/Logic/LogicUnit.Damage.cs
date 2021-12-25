using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Logic class for damage calculations and hit generation.
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc />
        public async Task<DamageReport> ResolveDamageInstance(DamageInstance damageInstance)
        {
            var paperDoll = await GetPaperDoll(this, AttackType.Normal, damageInstance.Direction, Options);

            var damageReport = new DamageReport
            {
                DamagePaperDoll = paperDoll.GetDamagePaperDoll(),
                FiringUnitId = Guid.Empty,
                TargetUnitId = Unit.Id,
                TargetUnitName = Unit.Name,
                InitialTroopers = Unit.Troopers
            };

            damageReport.Log(new AttackLogEntry { Context = "Damage request total damage", Number = damageInstance.Damage, Type = AttackLogEntryType.Calculation });

            damageReport.Log(new AttackLogEntry { Context = "Damage request cluster size", Number = damageInstance.ClusterSize, Type = AttackLogEntryType.Calculation });

            var damagePackets = Clusterize(damageInstance.Damage, damageInstance.ClusterSize, 1, new SpecialDamageEntry { Type = SpecialDamageType.None });

            await ResolveDamagePackets(damageReport, Options.Rules, new FiringSolution { Cover = damageInstance.Cover, Direction = damageInstance.Direction, TargetUnit = damageInstance.UnitId }, 0, WeaponMode.Normal, damagePackets);

            return damageReport;
        }

        /// <summary>
        /// Resolves the effects of a combat action and returns a corresponding damage report.
        /// </summary>
        /// <param name="combatAction">The combat action to resolve.</param>
        /// <returns>The damage report caused by the combat action, if any. Returns null for no action.</returns>
        protected async Task<DamageReport> ResolveCombatAction(CombatAction combatAction, ILogicUnit target)
        {
            // This is a two-phase process. Firstly, the amount of damage done is determined by the firing unit
            // Different units treat the same weapons differently and use them differently

            // Generate the paperdoll for the attack
            var targetPaperDoll = await GetPaperDoll(target, combatAction.Weapon.AttackType, Unit.FiringSolution.Direction, Options);

            var damageReport = new DamageReport
            {
                DamagePaperDoll = targetPaperDoll.GetDamagePaperDoll(),
                FiringUnitId = Unit.Id,
                FiringUnitName = Unit.Name,
                TargetUnitId = target.GetUnit().Id,
                TargetUnitName = target.GetUnit().Name,
                InitialTroopers = target.GetUnit().Troopers
            };

            // First, we must determine the total amount of damage dealt
            var damageAmount = await ResolveTotalOutgoingDamage(damageReport, target, combatAction);

            // Then we transform the damage based on the target unit type
            damageAmount = await target.TransformDamage(damageReport, combatAction, damageAmount);

            // Finally, transform damage based on quirks
            damageAmount = TransformDamageAmountBasedOnTargetQuirks(damageReport, target, combatAction, damageAmount);

            // Then we make packets of the damage, as per clustering and rapid fire rules
            var damagePackets = MakeDamagePackets(damageReport, firingUnit, rangeBracket, targetUnit, weapon, mode, damageAmount);

            // Special weapon features which modify or add damage types
            ModifyDamagePacketsBasedOnWeaponFeatures(damageReport, damagePackets, targetUnit, weapon, mode);

            ModifyDamagePacketsBasedOnTargetType(damageReport, damagePackets, targetUnit);

            return damagePackets;
        }

        /// <summary>
        /// Resolves the effects of a combat action on the attacking unit and returns a corresponding damage report.
        /// </summary>
        /// <param name="combatAction">The combat action to resolve.</param>
        /// <returns>The damage report caused by the combat action on the attacking unit, if any. Returns null for no action.</returns>
        protected Task<DamageReport> ResolveCombatActionSelf(CombatAction combatAction, ILogicUnit target)
        {
            // Only certain melee weapons have this for now
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.MeleeCharge, out _))
            {
                if (!hit)
                {
                    return null;
                }

                damageReport.Log(new AttackLogEntry { Context = "Attacker is damaged by its charge attack", Type = AttackLogEntryType.Information });
                string attackerDamageStringCharge;
                switch (targetUnit.Type)
                {
                    case UnitType.Building:
                    case UnitType.BattleArmor:
                    case UnitType.AerospaceCapital:
                    case UnitType.AerospaceDropship:
                    case UnitType.Infantry:
                        attackerDamageStringCharge = $"{firingUnit.Tonnage}/10";
                        break;
                    default:
                        attackerDamageStringCharge = $"{targetUnit.Tonnage}/10";
                        break;
                }

                var attackerDamageCharge = _mathExpression.Parse(attackerDamageStringCharge);

                return await ResolveDamageInstance(new DamageInstance
                {
                    AttackType = AttackType.Normal,
                    ClusterSize = 5,
                    Cover = Cover.None,
                    Damage = attackerDamageCharge,
                    Direction = Direction.Front,
                    TimeStamp = DateTime.UtcNow,
                    UnitId = firingUnit.Id
                },
                    gameOptions);
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.MeleeDfa, out _))
            {
                if (hit)
                {
                    damageReport.Log(new AttackLogEntry { Context = "Attacker is damaged by its DFA attack", Type = AttackLogEntryType.Information });
                    var attackerDamageDfa = firingUnit.HasFeature(UnitFeature.ReinforcedLegs) ?
                        _mathExpression.Parse($"{firingUnit.Tonnage}/10") :
                        _mathExpression.Parse($"{firingUnit.Tonnage}/5");
                    return await ResolveDamageInstance(new DamageInstance
                    {
                        AttackType = AttackType.Kick,
                        ClusterSize = 5,
                        Cover = Cover.None,
                        Damage = attackerDamageDfa,
                        Direction = Direction.Front,
                        TimeStamp = DateTime.UtcNow,
                        UnitId = firingUnit.Id
                    },
                        gameOptions);
                }
                else
                {
                    damageReport.Log(new AttackLogEntry { Context = "Attacker falls onto its back due to a failed DFA attack", Type = AttackLogEntryType.Information });
                    var attackerDamageDfa = _mathExpression.Parse($"2*{firingUnit.Tonnage}/5");
                    return await ResolveDamageInstance(new DamageInstance
                    {
                        AttackType = AttackType.Normal,
                        ClusterSize = 5,
                        Cover = Cover.None,
                        Damage = attackerDamageDfa,
                        Direction = Direction.Rear,
                        TimeStamp = DateTime.UtcNow,
                        UnitId = firingUnit.Id
                    },
                        gameOptions);
                }
            }

            return null;
        }

        protected virtual async Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            return await RapidFireWrapper(damageReport, target, combatAction, ResolveTotalOutgoingDamageInternal(damageReport, target, combatAction));
        }

        private async Task<int> ResolveTotalOutgoingDamageInternal(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterBonus = ResolveClusterBonus(damageReport, target, combatAction);

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });

                return await ResolveClusterValue(damageReport, target, combatAction, combatAction.Weapon.Damage[combatAction.RangeBracket], clusterBonus);
            }

            var damage = combatAction.Weapon.Damage[combatAction.RangeBracket];
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Weapon damage value", Number = damage });
            return damage;
        }

        /// <summary>
        /// Handles damage transformation based on quirks.
        /// </summary>
        /// <param name="damageReport">The damage report to write into.</param>
        /// <param name="marginOfSuccess">The margin of success.</param>
        /// <param name="damageAmount">The damage before transformation.</param>
        /// <returns>The transformed damage amount.</returns>
        private int TransformDamageAmountBasedOnTargetQuirks(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damageAmount)
        {
            // Cluster weapons have been affected earlier, so they will not be affected again
            if (!combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _) && target.IsGlancingBlow(combatAction))
            {
                // Round down, but minimum is still 1
                var transformedDamage = Math.Max(damageAmount / 2, 1);
                damageReport.Log(new AttackLogEntry { Context = $"Quirk {UnitFeature.NarrowLowProfile} modifies received damage. New damage", Number = transformedDamage, Type = AttackLogEntryType.Calculation });

                return transformedDamage;
            }

            return damageAmount;
        }

        /// <inheritdoc />
        public virtual Task<int> TransformDamage(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            return Task.FromResult(damageAmount);
        }

        #region Support methods

        protected async Task<int> RapidFireWrapper(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, Task<int> singleFireDamageCalculation)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                var maxHits = LogicHelper.MathExpression.Parse(rapidFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Rapid fire weapon potential maximum number of hits", Number = maxHits });
                var hits = await ResolveClusterValue(damageReport, target, combatAction, maxHits, 0);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Rapid fire weapon number of hits", Number = hits });

                var damage = 0;
                for (var ii = 0; ii < hits; ii++)
                {
                    damage += await singleFireDamageCalculation;
                }

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total damage after calculating all hits", Number = damage });

                return damage;
            }

            return await singleFireDamageCalculation;
        }

        /// <inheritdoc />
        protected int ResolveHeatExtraDamage(DamageReport damageReport, CombatAction combatAction, int damage)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Heat, out var heatFeatureEntry))
            {
                var addDamage = LogicHelper.MathExpression.Parse(heatFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Bonus damage from heat-inflicting weapon", Number = addDamage });
                return damage += addDamage;
            }

            return damage;
        }

        #endregion
    }
}