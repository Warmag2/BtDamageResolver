using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract;

/// <summary>
/// Logic class for quad-type mechs.
/// </summary>
public class LogicUnitMechQuad : LogicUnitMechBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitMechQuad"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="repositoryProvider">The repository provider.</param>
    /// <param name="resolverRandom">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    public LogicUnitMechQuad(ILogger<LogicUnitMechQuad> logger, GameOptions gameOptions, IMathExpression mathExpression, RepositoryProvider repositoryProvider,  IResolverRandom resolverRandom, UnitEntry unit) : base(logger, gameOptions, mathExpression, repositoryProvider, resolverRandom, unit)
    {
    }

    /// <inheritdoc />
    public override int GetCoverModifier(Cover cover)
    {
        switch (cover)
        {
            case Cover.HullDown:
                return 3;
            case Cover.Lower:
            case Cover.Left:
            case Cover.Right:
            case Cover.Upper:
                return 1;
            default:
                return 0;
        }
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.MechQuad;
    }
}