using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations
{
    /// <summary>
    /// Abstract logic class for large aerospace units (small craft, capital, dropship).
    /// </summary>
    public abstract class LogicUnitAerospaceLarge : LogicUnitAerospace
    {
        /// <inheritdoc />
        public LogicUnitAerospaceLarge(ILogger<LogicUnitAerospaceLarge> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
        {
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.AerospaceCapital;
        }

        /// <inheritdoc />
        public override Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damageAmount));
        }
    }
}
