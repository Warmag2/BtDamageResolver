using System;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract;

/// <summary>
/// Logic class for capital-scale aerospace units.
/// </summary>
public class LogicUnitAerospaceCapital : LogicUnitAerospaceLarge
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitAerospaceCapital"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="repositoryProvider">The repository provider.</param>
    /// <param name="resolverRandom">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    public LogicUnitAerospaceCapital(ILogger<LogicUnitAerospaceCapital> logger, GameOptions gameOptions, IMathExpression mathExpression, RepositoryProvider repositoryProvider, IResolverRandom resolverRandom, UnitEntry unit) : base(logger, gameOptions, mathExpression, repositoryProvider, resolverRandom, unit)
    {
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.AerospaceCapital;
    }

    /// <inheritdoc />
    protected override Location TransformLocation(Location location)
    {
        switch (location)
        {
            case Location.Front:
                return Location.Front;
            case Location.Rear:
                return Location.Rear;
            case Location.FrontLeft:
                return Location.Left;
            case Location.FrontRight:
                return Location.Right;
            case Location.RearLeft:
                return Location.Left;
            case Location.RearRight:
                return Location.Right;
            default:
                throw new InvalidOperationException($"Unable to transform critical hit location for capital ship: {location}");
        }
    }
}