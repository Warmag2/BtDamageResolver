using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Logic class for damage calculations and hit generation.
    /// </summary>
    public partial class LogicUnit
    {
        #region Public methods

        /// <inheritdoc />
        public async Task<DamageReport> ResolveDamageInstance(DamageReport damageReport, DamageInstance damageInstance, GameOptions options)
        {
            damageReport.Log(new AttackLogEntry { Context = "Damage request total damage", Number = damageInstance.Damage, Type = AttackLogEntryType.Calculation });

            damageReport.Log(new AttackLogEntry { Context = "Damage request cluster size", Number = damageInstance.ClusterSize, Type = AttackLogEntryType.Calculation });

            var damagePackets = Clusterize(damageInstance.Damage, damageInstance.ClusterSize, 1, new SpecialDamageEntry { Type = SpecialDamageType.None });

            await ApplyDamagePackets(damageReport, damagePackets, new FiringSolution { Cover = damageInstance.Cover, Direction = damageInstance.Direction, TargetUnit = damageInstance.UnitId }, 0);

            return damageReport;
        }

        /// <inheritdoc />
        public virtual Task<int> TransformDamage(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            return Task.FromResult(damageAmount);
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Calculate total damage amount changes from multiple shots when firing rapid-fire weapons.
        /// </summary>
        /// <param name="damageReport">The damage report.</param>
        /// <param name="target">The target unit logic.</param>
        /// <param name="combatAction">The combat action.</param>
        /// <param name="singleFireDamageCalculation">A task that calculates the damage of a single fire instance.</param>
        /// <returns>The total damage by rapid fire action.</returns>
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

        /// <summary>
        /// Resolves the effects of a combat action and returns a corresponding damage report.
        /// </summary>
        /// <param name="target">The target unit logic.</param>
        /// <param name="combatAction">The combat action to resolve.</param>
        /// <returns>The damage report caused by the combat action, if any. Returns null for no action.</returns>
        protected async Task<DamageReport> ResolveCombatAction(ILogicUnit target, CombatAction combatAction)
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
            damageAmount = TransformDamageAmountBasedOnTargetFeatures(damageReport, target, combatAction, damageAmount);

            // Then we make packets of the damage, as per clustering and rapid fire rules
            var damagePackets = ResolveDamagePackets(damageReport, target, combatAction, damageAmount);

            // Special weapon features which modify or add damage types
            damagePackets = TransformDamagePacketsBasedOnWeaponFeatures(damageReport, damagePackets, target, combatAction);

            damagePackets = TransformDamagePacketsBasedOnTargetType(damageReport, damagePackets, target, combatAction);

            await target.ApplyDamagePackets(damageReport, damagePackets, Unit.FiringSolution, combatAction.MarginOfSuccess);

            return damageReport;
        }

        /// <summary>
        /// Resolves the effects of a combat action on the attacking unit and returns a corresponding damage report.
        /// </summary>
        /// <param name="target">The target unit logic.</param>
        /// <param name="combatAction">The combat action to resolve.</param>
        /// <returns>The damage report caused by the combat action on the attacking unit, if any. Returns null for no action.</returns>
        protected async Task<DamageReport> ResolveCombatActionSelf(ILogicUnit target, CombatAction combatAction)
        {
            // Generate the paperdoll for the attack
            var selfPaperDoll = await GetPaperDoll(this, combatAction.Weapon.AttackType, Unit.FiringSolution.Direction, Options);

            var damageReport = new DamageReport
            {
                DamagePaperDoll = selfPaperDoll.GetDamagePaperDoll(),
                FiringUnitId = Unit.Id,
                FiringUnitName = Unit.Name,
                TargetUnitId = Unit.Id,
                TargetUnitName = Unit.Name,
                InitialTroopers = Unit.Troopers
            };

            // Only certain melee weapons have this for now, go through them one by one
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.MeleeCharge, out _))
            {
                if (!combatAction.HitHappened)
                {
                    return null;
                }

                damageReport.Log(new AttackLogEntry { Context = "Attacker is damaged by its charge attack", Type = AttackLogEntryType.Information });
                string attackerDamageStringCharge;
                switch (target.GetUnit().Type)
                {
                    case UnitType.Building:
                    case UnitType.BattleArmor:
                    case UnitType.AerospaceCapital:
                    case UnitType.AerospaceDropship:
                    case UnitType.Infantry:
                        attackerDamageStringCharge = $"{Unit.Tonnage}/10";
                        break;
                    default:
                        attackerDamageStringCharge = $"{target.GetUnit().Tonnage}/10";
                        break;
                }

                var attackerDamageCharge = LogicHelper.MathExpression.Parse(attackerDamageStringCharge);

                return await ResolveDamageInstance(damageReport, new DamageInstance
                {
                    AttackType = AttackType.Normal,
                    ClusterSize = 5,
                    Cover = Cover.None,
                    Damage = attackerDamageCharge,
                    Direction = Direction.Front,
                    TimeStamp = DateTime.UtcNow,
                    UnitId = Unit.Id
                },
                    Options);
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.MeleeDfa, out _))
            {
                if (combatAction.HitHappened)
                {
                    damageReport.Log(new AttackLogEntry { Context = "Attacker is damaged by its DFA attack", Type = AttackLogEntryType.Information });
                    var attackerDamageDfa = Unit.HasFeature(UnitFeature.ReinforcedLegs) ?
                        LogicHelper.MathExpression.Parse($"{Unit.Tonnage}/10") :
                        LogicHelper.MathExpression.Parse($"{Unit.Tonnage}/5");
                    return await ResolveDamageInstance(damageReport, new DamageInstance
                    {
                        AttackType = AttackType.Kick,
                        ClusterSize = 5,
                        Cover = Cover.None,
                        Damage = attackerDamageDfa,
                        Direction = Direction.Front,
                        TimeStamp = DateTime.UtcNow,
                        UnitId = Unit.Id
                    },
                        Options);
                }
                else
                {
                    damageReport.Log(new AttackLogEntry { Context = "Attacker falls onto its back due to a failed DFA attack", Type = AttackLogEntryType.Information });
                    var attackerDamageDfa = LogicHelper.MathExpression.Parse($"2*{Unit.Tonnage}/5");
                    return await ResolveDamageInstance(damageReport, new DamageInstance
                    {
                        AttackType = AttackType.Normal,
                        ClusterSize = 5,
                        Cover = Cover.None,
                        Damage = attackerDamageDfa,
                        Direction = Direction.Rear,
                        TimeStamp = DateTime.UtcNow,
                        UnitId = Unit.Id
                    },
                        Options);
                }
            }

            return null;
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

        /// <summary>
        /// Resolve total outgoing damage.
        /// </summary>
        /// <param name="damageReport">The damagereport to append to.</param>
        /// <param name="target">The target unit logic.</param>
        /// <param name="combatAction">The combat action.</param>
        /// <returns></returns>
        protected virtual async Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            // Most units can charge, and mech units can make other melee attacks. Resolve their damage here.
            if (combatAction.Weapon.Type == WeaponType.Melee)
            {
                return await RapidFireWrapper(damageReport, target, combatAction, ResolveTotalOutgoingDamageMelee(damageReport, combatAction));
            }
            else
            {
                return await RapidFireWrapper(damageReport, target, combatAction, ResolveTotalOutgoingDamageInternal(damageReport, target, combatAction));
            }
        }

        #endregion

        #region Private methods

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

        private Task<int> ResolveTotalOutgoingDamageMelee(DamageReport damageReport, CombatAction combatAction)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Melee, out var meleeFeatureEntry))
            {
                var meleeDamage = LogicHelper.MathExpression.Parse(meleeFeatureEntry.Data.InsertVariables(Unit));
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Melee damage", Number = meleeDamage });

                return Task.FromResult(meleeDamage);
            }

            throw new InvalidOperationException($"Weapon {combatAction.Weapon.Name} does not have a melee special feature even though it is of type melee.");
        }

        /// <summary>
        /// Handles damage transformation based on the features of the target unit.
        /// </summary>
        /// <param name="damageReport">The damage report to write into.</param>
        /// <param name="marginOfSuccess">The margin of success.</param>
        /// <param name="damageAmount">The damage before transformation.</param>
        /// <returns>The transformed damage amount.</returns>
        private int TransformDamageAmountBasedOnTargetFeatures(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damageAmount)
        {
            // Cluster weapons have been affected earlier, so they will not be affected again
            if (!combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _) && target.IsGlancingBlow(combatAction.MarginOfSuccess))
            {
                // Round down, but minimum is still 1
                var transformedDamage = Math.Max(damageAmount / 2, 1);
                damageReport.Log(new AttackLogEntry { Context = $"Quirk {UnitFeature.NarrowLowProfile} modifies received damage. New damage", Number = transformedDamage, Type = AttackLogEntryType.Calculation });

                return transformedDamage;
            }

            return damageAmount;
        }

        private List<DamagePacket> TransformDamagePacketsBasedOnTargetType(DamageReport damageReport, List<DamagePacket> damagePackets, ILogicUnit target, CombatAction combatAction)
        {
            foreach (var damagePacket in damagePackets)
            {
                foreach (var entry in damagePacket.SpecialDamageEntries)
                {
                    if(!target.CanTakeEmpHits())
                    {
                        damageReport.Log(new AttackLogEntry { Context = "Target unit cannot receive EMP damage, removing special damage entry", Type = AttackLogEntryType.Information });
                        entry.Clear();
                    }

                    if (!target.IsHeatTracking())
                    {
                        damageReport.Log(new AttackLogEntry { Context = "Target unit cannot receive Heat damage, removing special damage entry", Type = AttackLogEntryType.Information });
                        entry.Clear();
                    }
                }
            }

            return damagePackets;
        }

        private List<DamagePacket> TransformDamagePacketsBasedOnWeaponFeatures(DamageReport damageReport, List<DamagePacket> damagePackets, ILogicUnit target, CombatAction combatAction)
        {
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.ArmorPiercing, out var armorPiercingEntry))
            {
                damagePackets[0].SpecialDamageEntries.Add(new SpecialDamageEntry { Data = armorPiercingEntry.Data, Type = SpecialDamageType.Critical });
                damageReport.Log(new AttackLogEntry { Context = "Armor Piercing weapon feature adds a potential critical hit", Type = AttackLogEntryType.Information });
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.MeleeCharge, out var chargeEntry) && target.CanTakeMotiveHits())
            {
                damagePackets[0].SpecialDamageEntries.Add(new SpecialDamageEntry { Data = chargeEntry.Data, Type = SpecialDamageType.Motive });
                damageReport.Log(new AttackLogEntry { Context = "Melee charge adds a potential motive hit", Type = AttackLogEntryType.Information });
            }

            return damagePackets;
        }

        #endregion
    }

    internal static class VariableExtensions
    {
        public static string InsertVariables(this string input, UnitEntry firingUnit)
        {
            return input
                .Replace(Names.ExpressionVariableNameDistance, firingUnit.FiringSolution.Distance.ToString())
                .Replace(Names.ExpressionVariableNameTonnage, firingUnit.Tonnage.ToString());
        }
    }
}