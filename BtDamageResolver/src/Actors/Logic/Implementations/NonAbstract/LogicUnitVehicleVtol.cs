﻿using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for VTOL vehicles.
    /// </summary>
    public class LogicUnitVehicleVtol : LogicUnitVehicle
    {
        /// <inheritdoc />
        public LogicUnitVehicleVtol(ILogger<LogicUnitVehicleVtol> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override int GetFeatureModifier(Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Flak, out var flakFeatureEntry))
            {
                return LogicHelper.MathExpression.Parse(flakFeatureEntry.Data);
            }

            return 0;
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.VehicleVtol;
        }

        /// <inheritdoc />
        public override int GetUnitTypeModifier()
        {
            return 1;
        }

        /// <inheritdoc />
        protected override int GetMotiveHitModifier()
        {
            return 0;
        }

        /// <inheritdoc />
        protected override int TransformDamageAmountBasedOnLocation(DamageReport damageReport, Location location, int damage)
        {
            // Only one case for now
            if (location == Location.Propulsion)
            {
                var returndedDamage = decimal.ToInt32(Math.Ceiling(damage / 10m));
                damageReport.Log(new AttackLogEntry { Context = "Damage after transformation into VTOL propulsion damage", Number = returndedDamage, Type = AttackLogEntryType.Calculation });
                return returndedDamage;
            }

            return damage;
        }
    }
}