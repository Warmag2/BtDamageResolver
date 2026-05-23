using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Abstract base logic class.
/// </summary>
public abstract partial class LogicUnit : ILogicUnit
{
    /// <summary>
    /// The game options.
    /// </summary>
    protected readonly GameOptions GameOptions;

    /// <summary>
    /// The logging interface.
    /// </summary>
    protected readonly ILogger<LogicUnit> Logger;

    /// <summary>
    /// The math expression solver.
    /// </summary>
    protected readonly IMathExpression MathExpression;

    /// <summary>
    /// The random number generator.
    /// </summary>
    protected readonly IResolverRandom ResolverRandom;

    /// <summary>
    /// The repository provider.
    /// </summary>
    protected readonly RepositoryProvider RepositoryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnit"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameoptions">The game options.</param>
    /// <param name="mathExpression">The mathematical expression solver.</param>
    /// <param name="repositoryProvider">The repository provider.</param>
    /// <param name="resolverRandom">The random number generator.</param>
    /// <param name="unit">The unit entry to construct this logic for.</param>
    /// 
    protected LogicUnit(ILogger<LogicUnit> logger, GameOptions gameoptions, IMathExpression mathExpression, RepositoryProvider repositoryProvider, IResolverRandom resolverRandom, UnitEntry unit)
    {
        Logger = logger;
        GameOptions = gameoptions;
        MathExpression = mathExpression;
        RepositoryProvider = repositoryProvider;
        ResolverRandom = resolverRandom;
        Unit = unit;
    }

    /// <inheritdoc />
    public UnitEntry Unit { get; }
}