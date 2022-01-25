﻿using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for capital-scale aerospace units.
    /// </summary>
    public class LogicUnitAerospaceCapital : LogicUnitAerospaceLarge
    {
        /// <inheritdoc />
        public LogicUnitAerospaceCapital(ILogger<LogicUnitAerospaceCapital> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
        {
        }

        /// <inheritdoc />
        protected override RangeBracket GetRangeBracket(Weapon weapon)
        {
            return GetRangeBracketAerospace(weapon, Unit.FiringSolution.Distance, 2);
        }
    }
}
