using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Logic class for damage calculations and hit generation.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc />
    public async Task<DamageReport> ResolveDamageInstance(DamageInstance damageInstance, Phase phase, bool selfDamage)
    {
        var damageReport = new DamageReport
        {
            Phase = phase,
            DamagePaperDoll = await GetDamagePaperDoll(this, AttackType.Normal, damageInstance.Direction, new List<WeaponFeature>()),
            FiringUnitId = selfDamage ? Unit.Id : Guid.Empty,
            FiringUnitName = selfDamage ? Unit.Name : null,
            TargetUnitId = Unit.Id,
            TargetUnitName = Unit.Name,
            InitialTroopers = Unit.Troopers
        };

        damageReport.Log(new AttackLogEntry { Context = "Damage request total damage", Number = damageInstance.Damage, Type = AttackLogEntryType.Calculation });

        damageReport.Log(new AttackLogEntry { Context = "Damage request cluster size", Number = damageInstance.ClusterSize, Type = AttackLogEntryType.Calculation });

        var transformedDamage = TransformDamageBasedOnStance(damageReport, damageInstance.Damage);

        var damagePackets = Clusterize(damageInstance.ClusterSize, transformedDamage, new SpecialDamageEntry { Type = SpecialDamageType.None });

        await ApplyDamagePackets(damageReport, damagePackets, new FiringSolution { Cover = damageInstance.Cover, Direction = damageInstance.Direction, TargetUnit = damageInstance.UnitId }, false, 0);

        return damageReport;
    }

    /// <inheritdoc />
    public virtual int TransformDamageBasedOnStance(DamageReport damageReport, int damageAmount)
    {
        return damageAmount;
    }

    /// <inheritdoc />
    public virtual Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        return Task.FromResult(damage);
    }

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
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
        {
            var maxHits = MathExpression.Parse(rapidFeatureEntry.Data);
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
        var damageReport = new DamageReport
        {
            Phase = combatAction.Weapon.Type == WeaponType.Melee ? Phase.Melee : Phase.Weapon,
            DamagePaperDoll = await GetDamagePaperDoll(target, combatAction.Weapon.AttackType, Unit.FiringSolution.Direction, combatAction.Weapon.SpecialFeatures.Select(w => w.Type).ToList()),
            FiringUnitId = Unit.Id,
            FiringUnitName = Unit.Name,
            TargetUnitId = target.Unit.Id,
            TargetUnitName = target.Unit.Name,
            InitialTroopers = target.Unit.Troopers
        };

        // First, we must determine the total amount of damage dealt
        var damageAmount = await ResolveTotalOutgoingDamage(damageReport, target, combatAction);

        // Then we transform the damage based on the target unit type
        damageAmount = await target.TransformDamageBasedOnUnitType(damageReport, combatAction, damageAmount);

        // Then we transform the damage based on the target cover
        damageAmount = target.TransformDamageBasedOnStance(damageReport, damageAmount);

        // Finally, transform damage based on quirks
        damageAmount = TransformDamageAmountBasedOnTargetFeatures(damageReport, target, combatAction, damageAmount);

        // Then we make packets of the damage, as per clustering and rapid fire rules
        var damagePackets = ResolveDamagePackets(damageReport, target, combatAction, damageAmount);

        // Special weapon features which modify or add damage types
        damagePackets = TransformDamagePacketsBasedOnWeaponFeatures(damageReport, damagePackets, target, combatAction);

        // Target type may yet induce transformations on damage packets
        damagePackets = TransformDamagePacketsBasedOnTargetType(damageReport, damagePackets, target);

        // Finally, apply damage packets
        await target.ApplyDamagePackets(damageReport, damagePackets, Unit.FiringSolution, false, combatAction.MarginOfSuccess);

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
        var damageReport = new DamageReport
        {
            Phase = combatAction.Weapon.Type == WeaponType.Melee ? Phase.Melee : Phase.Weapon,
            DamagePaperDoll = await GetDamagePaperDoll(this, combatAction.Weapon.AttackType, Unit.FiringSolution.Direction, new List<WeaponFeature>()),
            FiringUnitId = Unit.Id,
            FiringUnitName = Unit.Name,
            TargetUnitId = Unit.Id,
            TargetUnitName = Unit.Name,
            InitialTroopers = Unit.Troopers
        };

        // Only certain melee weapons have this for now, go through them one by one
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.MeleeCharge, out _))
        {
            if (!combatAction.HitHappened)
            {
                return null;
            }

            damageReport.Log(new AttackLogEntry { Context = "Attacker is damaged by its charge attack", Type = AttackLogEntryType.Information });
            string attackerDamageStringCharge;
            switch (target.Unit.Type)
            {
                case UnitType.Building:
                case UnitType.BattleArmor:
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.Infantry:
                    attackerDamageStringCharge = $"{Unit.Tonnage}/10";
                    break;
                default:
                    attackerDamageStringCharge = $"{target.Unit.Tonnage}/10";
                    break;
            }

            var attackerDamageCharge = MathExpression.Parse(attackerDamageStringCharge);

            damageReport.Merge(
                await ResolveDamageInstance(
                    new DamageInstance
                    {
                        AttackType = AttackType.Normal,
                        ClusterSize = 5,
                        Cover = Cover.None,
                        Damage = attackerDamageCharge,
                        Direction = Direction.Front,
                        TimeStamp = DateTime.UtcNow,
                        UnitId = Unit.Id
                    },
                    Phase.Melee,
                    true));

            return damageReport;
        }

        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.MeleeDfa, out _))
        {
            if (combatAction.HitHappened)
            {
                damageReport.Log(new AttackLogEntry { Context = "Attacker is damaged by its DFA attack", Type = AttackLogEntryType.Information });
                var attackerDamageDfa = Unit.HasFeature(UnitFeature.ReinforcedLegs) ?
                    MathExpression.Parse($"{Unit.Tonnage}/10") :
                    MathExpression.Parse($"{Unit.Tonnage}/5");
                damageReport.Merge(
                    await ResolveDamageInstance(
                        new DamageInstance
                        {
                            AttackType = AttackType.Kick,
                            ClusterSize = 5,
                            Cover = Cover.None,
                            Damage = attackerDamageDfa,
                            Direction = Direction.Front,
                            TimeStamp = DateTime.UtcNow,
                            UnitId = Unit.Id
                        },
                        Phase.Melee,
                        true));
            }
            else
            {
                damageReport.Log(new AttackLogEntry { Context = "Attacker falls onto its back due to a failed DFA attack", Type = AttackLogEntryType.Information });
                var attackerDamageDfa = MathExpression.Parse($"2*{Unit.Tonnage}/5");
                damageReport.Merge(
                    await ResolveDamageInstance(
                        new DamageInstance
                        {
                            AttackType = AttackType.Normal,
                            ClusterSize = 5,
                            Cover = Cover.None,
                            Damage = attackerDamageDfa,
                            Direction = Direction.Rear,
                            TimeStamp = DateTime.UtcNow,
                            UnitId = Unit.Id
                        },
                        Phase.Melee,
                        true));
            }

            await CheckWeakLegs(damageReport);

            return damageReport;
        }

        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.MeleeKick, out _))
        {
            await CheckWeakLegs(damageReport);

            return damageReport;
        }

        return null;
    }

    /// <summary>
    /// Resolve extra normal damage to unit from heat.
    /// </summary>
    /// <param name="damageReport">The damage report to apply to.</param>
    /// <param name="combatAction">The combat action.</param>
    /// <param name="damage">The damage.</param>
    /// <returns>Total damage with heat effects applied.</returns>
    protected int ResolveHeatExtraDamage(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Heat, out var heatFeatureEntry))
        {
            var addDamage = MathExpression.Parse(heatFeatureEntry.Data);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Bonus damage from heat-inflicting weapon", Number = addDamage });
            return damage + addDamage;
        }

        return damage;
    }

    private async Task CheckWeakLegs(DamageReport damageReport)
    {
        if (Unit.HasFeature(UnitFeature.WeakLegs))
        {
            damageReport.Log(new AttackLogEntry
            {
                Context = "Attacker has weak legs and its attack forces a critical threat check.",
                Type = AttackLogEntryType.Information
            });
            await ApplyDamagePackets(
                damageReport,
                new List<DamagePacket>
                {
                    new()
                    {
                        Damage = 0,
                        SpecialDamageEntries = new List<SpecialDamageEntry>
                        {
                            new()
                            {
                                Data = "0",
                                Type = SpecialDamageType.Critical
                            }
                        }
                    }
                },
                new FiringSolution
                {
                    Cover = Cover.Upper, Direction = Direction.Front, TargetUnit = Unit.Id
                },
                true,
                0);
        }
    }

    /// <summary>
    /// Resolve total outgoing damage.
    /// </summary>
    /// <param name="damageReport">The damagereport to append to.</param>
    /// <param name="target">The target unit logic.</param>
    /// <param name="combatAction">The combat action.</param>
    /// <returns>The total outgoing damage.</returns>
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

    /// <summary>
    /// Handles damage transformation based on the features of the target unit.
    /// </summary>
    /// <param name="damageReport">The damage report to write into.</param>
    /// <param name="target">The target unit logic.</param>
    /// <param name="combatAction">The combat action.</param>
    /// <param name="damageAmount">The damage before transformation.</param>
    /// <returns>The transformed damage amount.</returns>
    private static int TransformDamageAmountBasedOnTargetFeatures(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damageAmount)
    {
        // Cluster weapons have been affected in their damage calculation, so they will not be affected again
        if (!combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Cluster, out _) && target.IsGlancingBlow(combatAction.MarginOfSuccess))
        {
            // Round down, but minimum is still 1
            var transformedDamage = Math.Max(damageAmount / 2, 1);
            damageReport.Log(new AttackLogEntry { Context = $"Unit feature {UnitFeature.NarrowLowProfile} modifies received damage. New damage", Number = transformedDamage, Type = AttackLogEntryType.Calculation });

            return transformedDamage;
        }

        return damageAmount;
    }

    private static List<DamagePacket> TransformDamagePacketsBasedOnTargetType(DamageReport damageReport, List<DamagePacket> damagePackets, ILogicUnit target)
    {
        foreach (var damagePacket in damagePackets)
        {
            foreach (var entry in damagePacket.SpecialDamageEntries)
            {
                if (entry.Type == SpecialDamageType.Emp && !target.CanTakeEmpHits())
                {
                    damageReport.Log(new AttackLogEntry { Context = "Target unit cannot receive EMP damage, removing special damage entry", Type = AttackLogEntryType.Information });
                    entry.Clear();
                }

                if (entry.Type == SpecialDamageType.Heat && !target.IsHeatTracking())
                {
                    damageReport.Log(new AttackLogEntry { Context = "Target unit cannot receive Heat damage, removing special damage entry", Type = AttackLogEntryType.Information });
                    entry.Clear();
                }
            }
        }

        return damagePackets;
    }

    private static List<DamagePacket> TransformDamagePacketsBasedOnWeaponFeatures(DamageReport damageReport, List<DamagePacket> damagePackets, ILogicUnit target, CombatAction combatAction)
    {
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.ArmorPiercing, out var armorPiercingEntry) && target.CanTakeCriticalHits())
        {
            damagePackets[0].SpecialDamageEntries.Add(new SpecialDamageEntry { Data = armorPiercingEntry.Data, Type = SpecialDamageType.Critical });
            damageReport.Log(new AttackLogEntry { Context = "Armor Piercing weapon feature adds a potential critical hit", Type = AttackLogEntryType.Information });
        }

        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.MeleeCharge, out var chargeEntry) && target.CanTakeMotiveHits())
        {
            damagePackets[0].SpecialDamageEntries.Add(new SpecialDamageEntry { Data = chargeEntry.Data, Type = SpecialDamageType.Motive });
            damageReport.Log(new AttackLogEntry { Context = "Melee charge adds a potential motive hit", Type = AttackLogEntryType.Information });
        }

        return damagePackets;
    }

    private async Task<int> ResolveTotalOutgoingDamageInternal(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Cluster, out _))
        {
            var clusterBonus = ResolveClusterBonus(damageReport, target, combatAction);

            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });

            return combatAction.Weapon.ClusterDamage * (await ResolveClusterValue(damageReport, target, combatAction, combatAction.Weapon.Damage[combatAction.RangeBracket], clusterBonus));
        }

        var damage = combatAction.Weapon.Damage[combatAction.RangeBracket];
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Weapon damage value", Number = damage });
        return damage;
    }

    private Task<int> ResolveTotalOutgoingDamageMelee(DamageReport damageReport, CombatAction combatAction)
    {
        if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Melee, out var meleeFeatureEntry))
        {
            var meleeDamage = MathExpression.Parse(meleeFeatureEntry.Data.InsertVariables(Unit));
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Melee damage", Number = meleeDamage });

            return Task.FromResult(meleeDamage);
        }

        throw new InvalidOperationException($"Weapon {combatAction.Weapon.Name} does not have a melee special feature even though it is of type melee.");
    }
}