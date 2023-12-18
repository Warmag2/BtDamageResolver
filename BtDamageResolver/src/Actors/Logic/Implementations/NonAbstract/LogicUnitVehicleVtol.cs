using System;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract;

/// <summary>
/// Logic class for VTOL vehicles.
/// </summary>
public class LogicUnitVehicleVtol : LogicUnitVehicle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitVehicleVtol"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    public LogicUnitVehicleVtol(ILogger<LogicUnitVehicleVtol> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override int GetFeatureModifier(Weapon weapon)
    {
        if (weapon.HasFeature(WeaponFeature.Flak, out var flakFeatureEntry))
        {
            return MathExpression.Parse(flakFeatureEntry.Data);
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