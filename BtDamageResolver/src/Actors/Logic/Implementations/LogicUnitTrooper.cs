using System;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations;

/// <summary>
/// Logic class for all units which contain multiple troopers. The rest inherit from here.
/// </summary>
public abstract class LogicUnitTrooper : LogicUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitTrooper"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    protected LogicUnitTrooper(ILogger<LogicUnitTrooper> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override bool CanTakeCriticalHits()
    {
        return false;
    }

    /// <inheritdoc />
    public override bool CanTakeEmpHits()
    {
        return false;
    }

    /// <inheritdoc />
    public override int GetMovementClassModifierBasedOnUnitType()
    {
        return GetMovementClassJumpCapable();
    }

    /// <inheritdoc />
    public override int TransformDamageBasedOnStance(DamageReport damageReport, Guid damageOwnerId, int damageAmount)
    {
        int transformedDamage;

        switch (Unit.Stance)
        {
            case Stance.Hardened:
                transformedDamage = (int)Math.Ceiling(damageAmount / 2.0m);
                damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, damageOwnerId, "Damage transformed by hardened cover", transformedDamage));
                return (int)Math.Ceiling(damageAmount / 2.0m);
            case Stance.Heavy:
                transformedDamage = (int)Math.Ceiling(damageAmount / 4.0m);
                damageReport.Log(new AttackLogEntry(AttackLogEntryType.Calculation, damageOwnerId, "Damage transformed by heavy cover", transformedDamage));
                return (int)Math.Ceiling(damageAmount / 4.0m);
            default:
                return damageAmount;
        }
    }
}