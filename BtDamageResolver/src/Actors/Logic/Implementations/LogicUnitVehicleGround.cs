using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations
{
    /// <summary>
    /// Abstract logic class for all ground vechicles and VTOLs.
    /// </summary>
    public abstract class LogicUnitVehicleGround : LogicUnitVehicle
    {
        /// <inheritdoc />
        public LogicUnitVehicleGround(ILogger<LogicUnitVehicleGround> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
        {
        }

        /// <inheritdoc />
        protected override AttackType TransformAttackType(ILogicUnit target, AttackType attackType, List<WeaponFeature> weaponFeatures)
        {
            // Ground vehicles doing charge attacks on standing mechs hit the legs
            if (weaponFeatures.Contains(WeaponFeature.MeleeCharge) && target.Unit.Stance == Stance.Normal)
            {
                switch (target.Unit.Type)
                {
                    case UnitType.Mech:
                    case UnitType.MechTripod:
                    case UnitType.MechQuad:
                        return AttackType.Kick;
                }
            }

            return AttackType.Normal;
        }
    }
}
