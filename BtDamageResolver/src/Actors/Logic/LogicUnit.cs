using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
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
        /// The grain factory.
        /// </summary>
        protected readonly IGrainFactory GrainFactory;

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
        protected readonly IResolverRandom Random;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicUnit"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="gameoptions">The game options.</param>
        /// <param name="grainFactory">The grain factory.</param>
        /// <param name="mathExpression">The mathematical expression solver.</param>
        /// <param name="random">The random number generator.</param>
        /// <param name="unit">The unit entry to construct this logic for.</param>
        protected LogicUnit(ILogger<LogicUnit> logger, GameOptions gameoptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit)
        {
            Logger = logger;
            GameOptions = gameoptions;
            GrainFactory = grainFactory;
            MathExpression = mathExpression;
            Random = random;
            Unit = unit;
        }

        /// <inheritdoc />
        public UnitEntry Unit { get; }
    }
}
