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
/// Abstract logic class for large aerospace units (small craft, capital, dropship).
/// </summary>
public abstract class LogicUnitAerospaceLarge : LogicUnitAerospace
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitAerospaceLarge"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    protected LogicUnitAerospaceLarge(ILogger<LogicUnitAerospaceLarge> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.AerospaceCapital;
    }

    /// <inheritdoc />
    public override Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damage));
    }
}