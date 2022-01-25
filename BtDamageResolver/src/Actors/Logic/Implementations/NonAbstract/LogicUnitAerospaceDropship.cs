using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for dropship-scale aerospace units (so far dropships and small craft use identical logic).
    /// </summary>
    public class LogicUnitAerospaceDropship : LogicUnitAerospaceLarge
    {
        /// <inheritdoc />
        public LogicUnitAerospaceDropship(ILogger<LogicUnitAerospaceDropship> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
        {
        }
    }
}
