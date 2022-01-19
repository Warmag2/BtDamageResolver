using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for capital-scale aerospace units.
    /// </summary>
    public class LogicUnitAerospaceCapital : LogicUnitAerospaceLarge
    {
        /// <inheritdoc />
        public LogicUnitAerospaceCapital(ILogger<LogicUnitAerospaceCapital> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        protected override RangeBracket GetRangeBracket(Weapon weapon)
        {
            return GetRangeBracketAerospace(weapon, Unit.FiringSolution.Distance, 2);
        }
    }
}
