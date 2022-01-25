using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for tracked vehicles.
    /// </summary>
    public class LogicUnitVehicleTracked : LogicUnitVehicleGround
    {
        /// <inheritdoc />
        public LogicUnitVehicleTracked(ILogger<LogicUnitVehicleTracked> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
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
            return 0;
        }
    }
}
