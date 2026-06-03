using System;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations;

/// <summary>
/// Logic class for all mech-class units. The rest inherit from here.
/// </summary>
public abstract class LogicUnitMechBase : LogicUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitMechBase"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="repositoryProvider">The repository provider.</param>
    /// <param name="resolverRandom">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    protected LogicUnitMechBase(ILogger<LogicUnitMechBase> logger, GameOptions gameOptions, IMathExpression mathExpression, RepositoryProvider repositoryProvider, IResolverRandom resolverRandom, UnitEntry unit) : base(logger, gameOptions, mathExpression, repositoryProvider, resolverRandom, unit)
    {
    }

    /// <inheritdoc />
    public override int GetCoverModifier(Cover cover)
    {
        switch (cover)
        {
            case Cover.Lower:
            case Cover.Left:
            case Cover.Right:
            case Cover.Upper:
                return 1;
            default:
                return 0;
        }
    }

    /// <inheritdoc />
    public override int GetMovementClassModifierBasedOnUnitType()
    {
        return GetMovementClassJumpCapable();
    }

    /// <inheritdoc />
    public override PaperDollType GetPaperDollType()
    {
        return PaperDollType.Mech;
    }

    /// <inheritdoc />
    public override int GetStanceModifier(int distance)
    {
        switch (Unit.Stance)
        {
            // Prone mechs are easier to hit at point-blank range and harder to hit at any other range
            case Stance.Prone:
                if (distance <= 1)
                {
                    return -1;
                }

                return 1;
            default:
                return 0;
        }
    }

    /// <inheritdoc />
    public override bool IsBlockedByCover(Cover cover, Location location)
    {
        switch (cover)
        {
            case Cover.HullDown:
            case Cover.Lower:
                switch (location)
                {
                    case Location.LeftLeg:
                    case Location.RightLeg:
                    case Location.RearLeftLeg:
                    case Location.RearRightLeg:
                    case Location.CenterLeg:
                        return true;
                    default:
                        return false;
                }

            case Cover.Left:
                switch (location)
                {
                    case Location.LeftTorso:
                    case Location.RearLeftTorso:
                    case Location.LeftArm:
                    case Location.LeftLeg:
                    case Location.RearLeftLeg:
                        return true;
                    default:
                        return false;
                }

            case Cover.Right:
                switch (location)
                {
                    case Location.RightTorso:
                    case Location.RearRightTorso:
                    case Location.RightArm:
                    case Location.RightLeg:
                    case Location.RearRightLeg:
                        return true;
                    default:
                        return false;
                }

            case Cover.Upper:
                switch (location)
                {
                    case Location.Head:
                    case Location.LeftTorso:
                    case Location.RightTorso:
                    case Location.CenterTorso:
                    case Location.RearLeftTorso:
                    case Location.RearRightTorso:
                    case Location.RearCenterTorso:
                    case Location.LeftArm:
                    case Location.RightArm:
                        return true;
                    default:
                        return false;
                }

            default:
                return false;
        }
    }

    /// <inheritdoc />
    public override bool IsHeatTracking()
    {
        return true;
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
    protected override async Task ResolveCriticalHit(DamageReport damageReport, Guid damageOwnerId, Location location, int criticalThreatRoll, int inducingDamage, int transformedDamage, CriticalDamageTableType criticalDamageTableType)
    {
        var criticalDamageTable = GetCriticalDamageTable(criticalDamageTableType, location);

        // Simulate arms and legs being able to be blown off
        if (criticalThreatRoll == 12 &&
            (location == Location.LeftArm || location == Location.LeftLeg ||
             location == Location.RightArm || location == Location.RightLeg))
        {
            damageReport.DamagePaperDoll.RecordCriticalDamage(location, damageOwnerId, inducingDamage, CriticalThreatType.Normal, CriticalDamageType.BlownOff);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Critical, damageOwnerId, string.Join(", ", criticalDamageTable.Mapping[criticalThreatRoll].Select(c => c.ToString())), transformedDamage, location));
        }
        else if (criticalDamageTable.Mapping[criticalThreatRoll].Exists(c => c != CriticalDamageType.None))
        {
            damageReport.DamagePaperDoll.RecordCriticalDamage(location, damageOwnerId, inducingDamage, CriticalThreatType.Normal, criticalDamageTable.Mapping[criticalThreatRoll]);
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Critical, damageOwnerId, transformedDamage, location));
        }
        else
        {
            damageReport.Log(new AttackLogEntry(AttackLogEntryType.Information, damageOwnerId, "Threat roll does not result in a critical hit"));
        }
    }
}
