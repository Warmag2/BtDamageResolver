using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract;

/// <summary>
/// Logic class for buildings.
/// </summary>
public class LogicUnitBuilding : LogicUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitBuilding"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    public LogicUnitBuilding(ILogger<LogicUnitBuilding> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override int GetMovementClassModifierBasedOnUnitType()
    {
        return -4;
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.Building;
    }

    /// <inheritdoc />
    public override int TransformClusterRollBasedOnUnitType(DamageReport damageReport, int clusterRoll)
    {
        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Static cluster roll value against a building", Number = 12 });

        return 12;
    }

    /// <inheritdoc />
    public override Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damage));
    }

    /// <inheritdoc />
    protected override Location TransformLocation(Location location)
    {
        return Location.Structure;
    }
}