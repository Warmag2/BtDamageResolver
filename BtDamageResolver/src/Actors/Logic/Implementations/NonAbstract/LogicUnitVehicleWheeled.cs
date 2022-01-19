using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for wheeled vehicles.
    /// </summary>
    public class LogicUnitVehicleWheeled : LogicUnitVehicle
    {
        /// <inheritdoc />
        public LogicUnitVehicleWheeled(ILogger<LogicUnitVehicleWheeled> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
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
            return 1;
        }
    }
}
