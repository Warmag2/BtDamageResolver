using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public abstract class LogicUnitAerospaceFighter : LogicUnitAerospace
    {
        public LogicUnitAerospaceFighter(ILogger<LogicUnitAerospaceFighter> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.AerospaceFighter;
        }

        /// <inheritdoc />
        public override void ResolveHeat(DamageReport targetDamageReport, CombatAction hit)
        {
            ResolveHeatInternal(targetDamageReport, hit);
        }
    }
}
