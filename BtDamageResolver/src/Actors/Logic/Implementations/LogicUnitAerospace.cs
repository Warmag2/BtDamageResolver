using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations;

/// <summary>
/// Abstract base logic class for all aerospace units.
/// </summary>
public abstract class LogicUnitAerospace : LogicUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitAerospace"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="repositoryProvider">The repository provider.</param>
    /// <param name="resolverRandom">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    protected LogicUnitAerospace(ILogger<LogicUnitAerospace> logger, GameOptions gameOptions, IMathExpression mathExpression, RepositoryProvider repositoryProvider, IResolverRandom resolverRandom, UnitEntry unit) : base(logger, gameOptions, mathExpression, repositoryProvider, resolverRandom, unit)
    {
    }

    /// <inheritdoc />
    public override int GetFeatureModifier(Weapon weapon)
    {
        if (weapon.HasFeature(WeaponFeature.Flak, out var flakFeatureEntry))
        {
            return MathExpression.Parse(flakFeatureEntry.Data);
        }

        return 0;
    }

    /// <inheritdoc />
    public override int GetMovementDirectionModifier(Direction direction)
    {
        switch (direction)
        {
            case Direction.Rear:
                return 0;
            case Direction.Front:
                return 1;
            case Direction.Left:
            case Direction.Right:
            case Direction.Bottom:
            case Direction.Top:
                return 2;
            default:
                throw new InvalidOperationException($"Unexpected direction for movement direction modifier: {direction}");
        }
    }

    /// <inheritdoc />
    public override int GetMovementModifier()
    {
        return 0;
    }

    /// <inheritdoc />
    protected override int GetMinimumRangeModifier(Weapon weapon, WeaponBay weaponBay)
    {
        return 0;
    }

    /// <inheritdoc />
    protected override int GetOwnMovementModifier()
    {
        return Unit.MovementClass is MovementClass.OutOfControl or MovementClass.Fast ? 2 : 0;
    }

    /// <inheritdoc />
    protected override RangeBracket GetRangeBracket(Weapon weapon, WeaponBay weaponBay)
    {
        return GetRangeBracketAerospace(weapon, weaponBay.FiringSolution.Distance);
    }

    /// <inheritdoc />
    protected override async Task ResolveCriticalHit(DamageReport damageReport, Guid damageOwnerId, Location location, int criticalThreatRoll, int inducingDamage, int transformedDamage, CriticalDamageTableType criticalDamageTableType)
    {
        var criticalDamageTable = GetCriticalDamageTable(criticalDamageTableType, location);

        if (criticalThreatRoll > 7)
        {
            var aerospaceCriticalHitRoll = ResolverRandom.D26();
            damageReport.DamagePaperDoll.RecordCriticalDamage(location, damageOwnerId, inducingDamage, CriticalThreatType.DamageThreshold, criticalDamageTable.Mapping[aerospaceCriticalHitRoll]);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.DiceRoll, damageOwnerId, "Aerospace critical hit roll", aerospaceCriticalHitRoll));
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Critical, damageOwnerId, string.Join(", ", criticalDamageTable.Mapping[aerospaceCriticalHitRoll].Select(c => c.ToString())), transformedDamage, location));
        }
        else
        {
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, damageOwnerId, "Threat roll does not result in a critical hit"));
        }
    }

    /// <inheritdoc />
    protected override List<DamagePacket> ResolveDamagePackets(DamageReport damageReport, Guid damageOwnerId, ILogicUnit target, CombatAction combatAction, int damage)
    {
        // Missile weapons which do 0 damage have been shot down. Return an empty list.
        if (combatAction.Weapon.Type == WeaponType.Missile && damage == 0)
        {
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, damageOwnerId, "Missile weapon has been shot down and does no damage"));
            return [];
        }

        if (combatAction.Weapon.HasFeature(WeaponFeature.Cluster, out _))
        {
            return Clusterize(5, damage, combatAction.Weapon.SpecialDamage);
        }

        if (combatAction.Weapon.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
        {
            return Clusterize((int)Math.Ceiling((decimal)damage / MathExpression.Parse(rapidFeatureEntry.Data)), damage, combatAction.Weapon.SpecialDamage);
        }

        return Clusterize(damage, damage, combatAction.Weapon.SpecialDamage);
    }

    /// <summary>
    /// Resolve heat for a single hit instance.
    /// </summary>
    /// <remarks>
    /// Calculation for capital ships.
    /// </remarks>
    /// <param name="weapon">The combat action to resolve heat for.</param>
    /// <param name="rangeBracket">The range bracket to resolve heat for.</param>
    /// <returns>The heat produced.</returns>
    protected override int ResolveHeatForSingleHit(Weapon weapon, RangeBracket rangeBracket)
    {
        return weapon.Heat[rangeBracket];
    }

    /// <inheritdoc />
    protected override Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
    {
        var damageValue = 0;

        switch (combatAction.Weapon.Type)
        {
            case WeaponType.Missile:
                if (target.Unit.HasFeature(UnitFeature.Ams))
                {
                    if (combatAction.Weapon.HasFeature(WeaponFeature.AmsImmune, out _))
                    {
                        damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, Unit.Id, "Missile is immune to AMS defenses"));
                    }
                    else
                    {
                        var amsPenalty = ResolverRandom.Next(6);
                        damageReport.Log(new AttackLogEntry(AttackLogEntryType.DiceRoll, Unit.Id, "Defender AMS roll for cluster damage reduction", amsPenalty));
                        damageValue -= amsPenalty;

                        damageReport.SpendAmmoDefender("AMS", 1);
                    }
                }

                if (target.Unit.HasFeature(UnitFeature.Ecm))
                {
                    var ecmPenalty = ResolverRandom.Next(3);
                    damageReport.Log(new AttackLogEntry(AttackLogEntryType.DiceRoll, Unit.Id, "Defender ECM roll for cluster damage reduction", ecmPenalty));
                    damageValue -= ecmPenalty;
                }

                break;
        }

        // Glancing blow for cluster aerospace weapons (improvised rule, since aerospace units do not normally use clustering)
        if (combatAction.Weapon.HasFeature(WeaponFeature.Cluster, out _) && target.IsGlancingBlow(combatAction.MarginOfSuccess))
        {
            var glancingBlowPenalty = ResolverRandom.Next(6);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.DiceRoll, Unit.Id, "Defender roll for cluster damage reduction from glancing blow", glancingBlowPenalty));
            damageValue -= glancingBlowPenalty;
        }

        damageValue += Math.Clamp(combatAction.Weapon.DamageAerospace[combatAction.RangeBracket], 0, int.MaxValue);
        damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, Unit.Id, "Total damage value", damageValue));

        return Task.FromResult(damageValue);
    }
}