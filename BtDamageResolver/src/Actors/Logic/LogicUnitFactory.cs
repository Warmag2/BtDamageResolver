using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;
using System;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Factory for unit logics.
    /// </summary>
    public class LogicUnitFactory : ILogicUnitFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IGrainFactory _grainFactory;
        private readonly IMathExpression _mathExpression;
        private readonly IResolverRandom _random;

        /// <summary>
        /// Constructor for unit logic factory.
        /// </summary>
        public LogicUnitFactory(ILoggerFactory loggerFactory, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random)
        {
            _loggerFactory = loggerFactory;
            _grainFactory = grainFactory;
            _mathExpression = mathExpression;
            _random = random;
        }
               
        /// <inheritdoc />
        public ILogicUnit CreateFrom(GameOptions gameOptions, UnitEntry unit)
        {
            switch (unit.Type)
            {
                case Api.Enums.UnitType.Building:
                    return new LogicUnitBuilding(_loggerFactory.CreateLogger<LogicUnitBuilding>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.AerospaceCapital:
                    return new LogicUnitAerospaceCapital(_loggerFactory.CreateLogger<LogicUnitAerospaceCapital>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.AerospaceDropship:
                    return new LogicUnitAerospaceDropship(_loggerFactory.CreateLogger<LogicUnitAerospaceDropship>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.AerospaceFighter:
                    return new LogicUnitAerospaceFighter(_loggerFactory.CreateLogger<LogicUnitAerospaceFighter>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.BattleArmor:
                    return new LogicUnitBattleArmor(_loggerFactory.CreateLogger<LogicUnitBattleArmor>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.Infantry:
                    return new LogicUnitInfantry(_loggerFactory.CreateLogger<LogicUnitInfantry>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.Mech:
                    return new LogicUnitMech(_loggerFactory.CreateLogger<LogicUnitMech>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.MechTripod:
                    return new LogicUnitMechTripod(_loggerFactory.CreateLogger<LogicUnitMechTripod>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.MechQuad:
                    return new LogicUnitMechQuad(_loggerFactory.CreateLogger<LogicUnitMechQuad>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.VehicleHover:
                    return new LogicUnitVehicleHover(_loggerFactory.CreateLogger<LogicUnitVehicleHover>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.VehicleTracked:
                    return new LogicUnitVehicleTracked(_loggerFactory.CreateLogger<LogicUnitVehicleTracked>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.VehicleVtol:
                    return new LogicUnitVehicleVtol(_loggerFactory.CreateLogger<LogicUnitVehicleVtol>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                case Api.Enums.UnitType.VehicleWheeled:
                    return new LogicUnitVehicleWheeled(_loggerFactory.CreateLogger<LogicUnitVehicleWheeled>(), gameOptions, _grainFactory, _mathExpression, _random, unit);
                default:
                    throw new NotImplementedException($"Factory for type {unit.Type} is not implemented.");
            }
        }
    }
}
