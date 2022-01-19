using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for bipedal mech-class units.
    /// </summary>
    public class LogicUnitMech : LogicUnitMechBase
    {
        /// <inheritdoc />
        public LogicUnitMech(ILogger<LogicUnitMechBase> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.Mech;
        }
    }
}
