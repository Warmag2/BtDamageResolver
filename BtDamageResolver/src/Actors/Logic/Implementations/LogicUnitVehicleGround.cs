using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations;

/// <summary>
/// Abstract logic class for all ground vechicles and VTOLs.
/// </summary>
public abstract class LogicUnitVehicleGround : LogicUnitVehicle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitVehicleGround"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    protected LogicUnitVehicleGround(ILogger<LogicUnitVehicleGround> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
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