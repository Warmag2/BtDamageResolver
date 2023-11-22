using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations;

/// <summary>
/// Abstract logic class for all ground vechicles and VTOLs.
/// </summary>
public abstract class LogicUnitVehicle : LogicUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitVehicle"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    protected LogicUnitVehicle(ILogger<LogicUnitVehicle> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override bool CanTakeMotiveHits()
    {
        return true;
    }

    /// <inheritdoc />
    public override Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damage));
    }

    /// <summary>
    /// Gets the modifier to motive hits for this unit type.
    /// </summary>
    /// <returns>The modifier to motive hit application.</returns>
    protected abstract int GetMotiveHitModifier();

    /// <inheritdoc />
    protected override int GetOwnMovementModifier()
    {
        switch (Unit.MovementClass)
        {
            case MovementClass.Normal:
                return 1;
            case MovementClass.Fast:
            case MovementClass.Masc:
                return Unit.HasFeature(UnitFeature.StabilizedWeapons) ? 1 : 2;
            default:
                return 0;
        }
    }

    /// <inheritdoc />
    protected override async Task ResolveCriticalHit(DamageReport damageReport, Location location, int criticalThreatRoll, int inducingDamage, int transformedDamage, CriticalDamageTableType criticalDamageTableType)
    {
        var criticalDamageTable = await GetCriticalDamageTable(criticalDamageTableType, location);

        if (criticalDamageTableType == CriticalDamageTableType.Motive)
        {
            criticalThreatRoll += GetMotiveHitModifier();

            // Let's not overflow
            if (criticalThreatRoll > 12)
            {
                criticalThreatRoll = 12;
            }

            damageReport.Log(new AttackLogEntry
            {
                Context = "Motive Hit threat roll modified by unit type",
                Number = criticalThreatRoll,
                Type = AttackLogEntryType.Calculation
            });
        }

        if (criticalDamageTable.Mapping[criticalThreatRoll].Exists(c => c != CriticalDamageType.None))
        {
            damageReport.DamagePaperDoll.RecordCriticalDamage(location, inducingDamage, CriticalThreatType.Normal, criticalDamageTable.Mapping[criticalThreatRoll]);
            damageReport.Log(new AttackLogEntry
            {
                Context = string.Join(", ", criticalDamageTable.Mapping[criticalThreatRoll].Select(c => c.ToString())),
                Number = transformedDamage,
                Location = location,
                Type = AttackLogEntryType.Critical
            });
        }
        else
        {
            if (criticalDamageTableType == CriticalDamageTableType.Motive)
            {
                damageReport.Log(new AttackLogEntry
                {
                    Context = "Threat roll does not result in a motive hit",
                    Type = AttackLogEntryType.Information
                });
            }
            else
            {
                damageReport.Log(new AttackLogEntry
                {
                    Context = "Threat roll does not result in a critical hit",
                    Type = AttackLogEntryType.Information
                });
            }
        }
    }
}
