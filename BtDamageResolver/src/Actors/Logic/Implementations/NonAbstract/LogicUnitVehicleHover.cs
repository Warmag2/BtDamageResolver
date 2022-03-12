﻿using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for hover vehicles.
    /// </summary>
    public class LogicUnitVehicleHover : LogicUnitVehicleGround
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicUnitVehicleHover"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="gameOptions">The game options.</param>
        /// <param name="grainFactory">The grain factory.</param>
        /// <param name="mathExpression">The math expression parser.</param>
        /// <param name="random">The random number generator.</param>
        /// <param name="unit">The unit.</param>
        public LogicUnitVehicleHover(ILogger<LogicUnitVehicleHover> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
        {
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.Vehicle;
        }

        /// <inheritdoc />
        protected override int GetMotiveHitModifier()
        {
            if (GameOptions.Rules[Rule.ImprovedVehicleSurvivability])
            {
                return 2;
            }

            return 3;
        }
    }
}
