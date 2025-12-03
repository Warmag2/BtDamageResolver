using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Microsoft.CodeAnalysis;

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
            DamagePaperDoll = await GetDamagePaperDoll(this, AttackType.Normal, damageInstance.Direction, []),
            FiringUnitIds = selfDamage ? [Unit.Id] : [Guid.Empty],
            FiringUnitNames = selfDamage ? new() { { Unit.Id, Unit.Name } } : new() { { Guid.Empty, null } },
            TargetUnitId = Unit.Id,
            TargetUnitName = Unit.Name,
            InitialTroopers = Unit.Troopers
        };

        damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Guid.Empty, "Damage request total damage", damageInstance.Damage));

        damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Guid.Empty, "Damage request cluster size", damageInstance.ClusterSize));

        var transformedDamage = TransformDamageBasedOnStance(damageReport, Guid.Empty, damageInstance.Damage);

        var damagePackets = Clusterize(damageInstance.ClusterSize, transformedDamage, [new() { Type = SpecialDamageType.None }]);

        await ApplyDamagePackets(damageReport, Guid.Empty, damagePackets, new FiringSolution { Cover = damageInstance.Cover, Direction = damageInstance.Direction, Target = damageInstance.UnitId }, 0);

        return damageReport;
    }

    /// <inheritdoc />
    public virtual int TransformDamageBasedOnStance(DamageReport damageReport, Guid damageOwnerId, int damageAmount)
    {
        return damageAmount;
    }

    /// <inheritdoc />
    public virtual Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, Guid damageOwnerId, CombatAction combatAction, int damage)
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
        if (combatAction.Weapon.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
        {
            var maxHits = MathExpression.Parse(rapidFeatureEntry.Data);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, "Rapid fire weapon potential maximum number of hits", maxHits));
            var hits = await ResolveClusterValue(damageReport, target, combatAction, maxHits, 0);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, "Rapid fire weapon number of hits", hits));

            var damage = 0;
            for (var ii = 0; ii < hits; ii++)
            {
                damage += await singleFireDamageCalculation;
            }

            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, "Total damage after calculating all hits", damage));

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
            DamagePaperDoll = await GetDamagePaperDoll(target, combatAction.Weapon.AttackType, combatAction.WeaponBay.FiringSolution.Direction, combatAction.Weapon.SpecialFeatures.Select(w => w.Type).ToList()),
            FiringUnitIds = [Unit.Id],
            FiringUnitNames = new() { { Unit.Id, Unit.Name } },
            TargetUnitId = target.Unit.Id,
            TargetUnitName = target.Unit.Name,
            InitialTroopers = target.Unit.Troopers
        };

        // First, we must determine the total amount of damage dealt
        var damageAmount = await ResolveTotalOutgoingDamage(damageReport, target, combatAction);

        // Then we transform the damage based on the target unit type
        damageAmount = await target.TransformDamageBasedOnUnitType(damageReport, Unit.Id, combatAction, damageAmount);

        // Then we transform the damage based on the target cover
        damageAmount = target.TransformDamageBasedOnStance(damageReport, Unit.Id, damageAmount);

        // Finally, transform damage based on quirks
        damageAmount = TransformDamageAmountBasedOnTargetFeatures(damageReport, target, combatAction, damageAmount);

        // Then we make packets of the damage, as per clustering and rapid fire rules
        var damagePackets = ResolveDamagePackets(damageReport, Unit.Id, target, combatAction, damageAmount);

        // Special weapon features which modify or add damage types
        damagePackets = TransformDamagePacketsBasedOnWeaponFeatures(damageReport, damagePackets, target, combatAction);

        // Target type may yet induce transformations on damage packets
        damagePackets = TransformDamagePacketsBasedOnTargetType(damageReport, damagePackets, target);

        // Finally, apply damage packets
        await target.ApplyDamagePackets(damageReport, Unit.Id, damagePackets, combatAction.WeaponBay.FiringSolution, combatAction.MarginOfSuccess);

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
            DamagePaperDoll = await GetDamagePaperDoll(this, combatAction.Weapon.AttackType, combatAction.WeaponBay.FiringSolution.Direction, []),
            FiringUnitIds = [Unit.Id],
            FiringUnitNames = new() { { Unit.Id, Unit.Name } },
            TargetUnitId = Unit.Id,
            TargetUnitName = Unit.Name,
            InitialTroopers = Unit.Troopers
        };

        // Only certain melee weapons have this for now, go through them one by one
        if (combatAction.Weapon.HasFeature(WeaponFeature.MeleeCharge, out _))
        {
            if (!combatAction.HitHappened)
            {
                damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Attacker must make a piloting skill roll because of a missed charge attack"));
                damageReport.DamagePaperDoll.RecordSpecialDamage(
                    Faemiyah.BtDamageResolver.Api.Enums.Location.Front,
                    Unit.Id,
                    new SpecialDamageEntry
                    {
                        Data = "2",
                        Type = SpecialDamageType.PilotingSkillRoll
                    });
            }
            else
            {
                damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Attacker is damaged by its charge attack"));
                var attackerDamageStringCharge = target.Unit.Type switch
                {
                    UnitType.Building or
                    UnitType.BattleArmor or
                    UnitType.AerospaceCapital or
                    UnitType.AerospaceDropshipAerodyne or
                    UnitType.AerospaceDropshipSpheroid or
                    UnitType.Infantry
                    => $"{Unit.Tonnage}/10",
                    _ => $"{target.Unit.Tonnage}/10",
                };
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
            }

            return damageReport;
        }

        if (combatAction.Weapon.HasFeature(WeaponFeature.MeleeDfa, out _))
        {
            if (combatAction.HitHappened)
            {
                damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Attacker is damaged by its DFA attack"));
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
                damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Attacker falls onto its back due to a failed DFA attack"));
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

            damageReport.Merge(await CheckWeakLegs(combatAction));

            return damageReport;
        }

        if (combatAction.Weapon.HasFeature(WeaponFeature.MeleeKick, out _))
        {
            damageReport.Merge(await CheckWeakLegs(combatAction));

            if (!combatAction.HitHappened)
            {
                damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Attacker must make a piloting skill roll because of a missed kick attack"));
                damageReport.DamagePaperDoll.RecordSpecialDamage(
                    Api.Enums.Location.Front,
                    Unit.Id,
                    new SpecialDamageEntry
                    {
                        Data = "0",
                        Type = SpecialDamageType.PilotingSkillRoll
                    });
            }

            return damageReport;
        }

        return null;
    }

    /// <summary>
    /// Resolve extra normal damage to unit from heat.
    /// </summary>
    /// <param name="damageReport">The damage report to apply to.</param>
    /// <param name="damageOwnerId">The ID of the instigator of the damage.</param>
    /// <param name="combatAction">The combat action.</param>
    /// <param name="damage">The damage.</param>
    /// <returns>Total damage with heat effects applied.</returns>
    protected int ResolveHeatExtraDamage(DamageReport damageReport, Guid damageOwnerId, CombatAction combatAction, int damage)
    {
        foreach (var heatDamageEntry in combatAction.Weapon.SpecialDamage.Where(s => s.Type == SpecialDamageType.HeatConverted))
        {
            var addDamage = MathExpression.Parse(heatDamageEntry.Data);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, damageOwnerId, "Bonus damage from heat-inflicting weapon", addDamage));

            damage += addDamage;
        }

        return damage;
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
    private int TransformDamageAmountBasedOnTargetFeatures(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damageAmount)
    {
        // Cluster weapons have been affected in their damage calculation, so they will not be affected again
        if (!combatAction.Weapon.HasFeature(WeaponFeature.Cluster, out _) && target.IsGlancingBlow(combatAction.MarginOfSuccess))
        {
            // Round down, but minimum is still 1
            var transformedDamage = Math.Max(damageAmount / 2, 1);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, $"Unit feature {UnitFeature.NarrowLowProfile} modifies received damage. New damage", transformedDamage));

            return transformedDamage;
        }

        return damageAmount;
    }

    private List<DamagePacket> TransformDamagePacketsBasedOnTargetType(DamageReport damageReport, List<DamagePacket> damagePackets, ILogicUnit target)
    {
        foreach (var damagePacket in damagePackets)
        {
            foreach (var entry in damagePacket.SpecialDamageEntries)
            {
                if (entry.Type == SpecialDamageType.Critical && !target.CanTakeCriticalHits())
                {
                    damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Target unit cannot receive critical hits, removing special damage entry"));
                    entry.Clear();
                }

                if (entry.Type == SpecialDamageType.Emp && !target.CanTakeEmpHits())
                {
                    damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Target unit cannot receive EMP damage, removing special damage entry"));
                    entry.Clear();
                }

                if (entry.Type == SpecialDamageType.Heat && !target.IsHeatTracking())
                {
                    damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Target unit cannot receive Heat damage, removing special damage entry"));
                    entry.Clear();
                }

                if (entry.Type == SpecialDamageType.Motive && !target.CanTakeMotiveHits())
                {
                    damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Target unit cannot receive motive hits, removing special damage entry"));
                    entry.Clear();
                }

                // Handled earlier and transformed to regular damage, can be removed at this point.
                if (entry.Type is SpecialDamageType.HeatConverted or SpecialDamageType.Burst)
                {
                    entry.Clear();
                }
            }
        }

        return damagePackets;
    }

    private List<DamagePacket> TransformDamagePacketsBasedOnWeaponFeatures(DamageReport damageReport, List<DamagePacket> damagePackets, ILogicUnit target, CombatAction combatAction)
    {
        if (combatAction.Weapon.HasFeature(WeaponFeature.ArmorPiercing, out var armorPiercingEntry) && target.CanTakeCriticalHits())
        {
            damagePackets[0].SpecialDamageEntries.Add(new SpecialDamageEntry { Data = armorPiercingEntry.Data, Type = SpecialDamageType.Critical });
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Armor Piercing weapon feature adds a potential critical hit"));
        }

        if (combatAction.Weapon.HasFeature(WeaponFeature.MeleeCharge, out var chargeEntry) && target.CanTakeMotiveHits())
        {
            damagePackets[0].SpecialDamageEntries.Add(new SpecialDamageEntry { Data = chargeEntry.Data, Type = SpecialDamageType.Motive });
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Melee charge adds a potential motive hit"));
        }

        return damagePackets;
    }

    private async Task<DamageReport> CheckWeakLegs(CombatAction combatAction)
    {
        if (Unit.HasFeature(UnitFeature.WeakLegs))
        {
            var damageReport = new DamageReport
            {
                Phase = combatAction.Weapon.Type == WeaponType.Melee ? Phase.Melee : Phase.Weapon,
                DamagePaperDoll = await GetDamagePaperDoll(this, AttackType.Kick, Direction.Front, []),
                FiringUnitIds = [Unit.Id],
                FiringUnitNames = new() { { Unit.Id, Unit.Name } },
                TargetUnitId = Unit.Id,
                TargetUnitName = Unit.Name,
                InitialTroopers = Unit.Troopers
            };

            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Attacker has weak legs and its attack forces a critical threat check."));
            await ApplyDamagePackets(
                damageReport,
                Unit.Id,
                [new() { Damage = 0, SpecialDamageEntries = [new() { Data = "0", Type = SpecialDamageType.Critical }] }],
                new FiringSolution
                {
                    Cover = Cover.None, Direction = Direction.Front, Target = Unit.Id
                },
                0);

            return damageReport;
        }

        return null;
    }

    private async Task<int> ResolveTotalOutgoingDamageInternal(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        if (combatAction.Weapon.HasFeature(WeaponFeature.Cluster, out _))
        {
            var clusterBonus = ResolveClusterBonus(damageReport, Unit.Id, target, combatAction);

            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, "Total cluster modifier", clusterBonus));

            return combatAction.Weapon.ClusterDamage * await ResolveClusterValue(damageReport, target, combatAction, combatAction.Weapon.Damage[combatAction.RangeBracket], clusterBonus);
        }

        var damage = combatAction.Weapon.Damage[combatAction.RangeBracket];
        damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, "Weapon damage value", damage));
        return damage;
    }

    private Task<int> ResolveTotalOutgoingDamageMelee(DamageReport damageReport, CombatAction combatAction)
    {
        if (combatAction.Weapon.HasFeature(WeaponFeature.Melee, out var meleeFeatureEntry))
        {
            var meleeDamage = MathExpression.Parse(meleeFeatureEntry.Data.InsertVariables(Unit, combatAction.WeaponBay));
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, "Melee damage", meleeDamage));

            return Task.FromResult(meleeDamage);
        }

        throw new InvalidOperationException($"Weapon {combatAction.Weapon.Name} does not have a melee special feature even though it is of type melee.");
    }
}