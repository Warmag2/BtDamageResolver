using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Logic class for mech-class units. MechTripod and MechQuad inherit from here.
    /// </summary>
    public class LogicUnitMechBase : LogicUnit
    {
        /// <inheritdoc />
        public LogicUnitMechBase(ILogger<LogicUnitMechBase> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override int GetCoverModifier(Cover cover)
        {
            return cover != Cover.None ? 1 : 0;
        }

        /// <inheritdoc />
        public override int GetMovementClassModifier()
        {
            return GetMovementClassJumpCapable();
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
                case MovementClass.Jump:
                    return 3;
                default:
                    return 0;
            }
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.Mech;
        }

        /// <inheritdoc />
        public override void ResolveHeat(DamageReport targetDamageReport, CombatAction hit)
        {
            ResolveHeatInternal(targetDamageReport, hit);
        }
    }
}
