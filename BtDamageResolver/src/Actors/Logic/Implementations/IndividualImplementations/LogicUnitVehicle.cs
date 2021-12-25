using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Abstract logic class for all ground vechicles and VTOLs.
    /// </summary>
    public abstract class LogicUnitVehicle : LogicUnit
    {
        /// <inheritdoc />
        public LogicUnitVehicle(ILogger<LogicUnitVehicle> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        protected override int GetOwnMovementModifier()
        {
            switch (Unit.MovementClass)
            {
                case MovementClass.Normal:
                    return 1;
                case MovementClass.Fast:
                case MovementClass.Masc:
                    return Unit.HasFeature(UnitFeature.StabilizedWeapons) ? 1 : 2;
                default:
                    return 0;
            }
        }

        /// <inheritdoc />
        public override Task<int> TransformDamage(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damageAmount));
        }
    }
}
