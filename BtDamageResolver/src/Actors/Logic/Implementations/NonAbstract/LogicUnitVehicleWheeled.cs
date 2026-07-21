using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract;

/// <summary>
/// Logic class for wheeled vehicles.
/// </summary>
public class LogicUnitVehicleWheeled : LogicUnitVehicleGround
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitVehicleWheeled"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="repositoryProvider">The repository provider.</param>
    /// <param name="resolverRandom">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    public LogicUnitVehicleWheeled(ILogger<LogicUnitVehicleWheeled> logger, GameOptions gameOptions, IMathExpression mathExpression, RepositoryProvider repositoryProvider, IResolverRandom resolverRandom, UnitEntry unit) : base(logger, gameOptions, mathExpression, repositoryProvider, resolverRandom, unit)
    {
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.Vehicle;
    }

    /// <inheritdoc />
    protected override int GetMotiveHitModifier()
    {
        if (GameOptions.Rules[Rule.ImprovedVehicleSurvivability])
        {
            return 1;
        }

        return 2;
    }
}