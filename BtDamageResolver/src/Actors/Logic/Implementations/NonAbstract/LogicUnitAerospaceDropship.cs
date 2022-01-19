using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for dropship-scale aerospace units (so far dropships and small craft use identical logic).
    /// </summary>
    public class LogicUnitAerospaceDropship : LogicUnitAerospaceLarge
    {
        /// <inheritdoc />
        public LogicUnitAerospaceDropship(ILogger<LogicUnitAerospaceDropship> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }
    }
}
