using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
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
        protected readonly ILogger<LogicUnit> Logger;
        protected readonly GameOptions GameOptions;
        protected readonly IGrainFactory GrainFactory;
        protected readonly IMathExpression MathExpression;
        protected readonly IResolverRandom Random;
        protected readonly UnitEntry Unit;

        /// <summary>
        /// General logic constructor.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="gameoptions">The game options.</param>
        /// <param name="grainFactory">The grain factory.</param>
        /// <param name="mathExpression">The mathematical expression solver.</param>
        /// <param name="random">The random number generator.</param>
        /// <param name="unit">The unit entry to construct this logic for.</param>
        public LogicUnit(ILogger<LogicUnit> logger, GameOptions gameoptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit)
        {
            Logger = logger;
            GameOptions = gameoptions;
            GrainFactory = grainFactory;
            MathExpression = mathExpression;
            Random = random;
            Unit = unit;
        }
    }
}
