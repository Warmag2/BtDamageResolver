using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Abstract logic class for large aerospace units (small craft, capital, dropship).
    /// </summary>
    public abstract class LogicUnitAerospaceLarge : LogicUnitAerospace
    {
        /// <inheritdoc />
        public LogicUnitAerospaceLarge(ILogger<LogicUnitAerospaceLarge> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.AerospaceCapital;
        }

        /// <inheritdoc />
        public override Task<int> TransformDamage(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damageAmount));
        }
    }
}
