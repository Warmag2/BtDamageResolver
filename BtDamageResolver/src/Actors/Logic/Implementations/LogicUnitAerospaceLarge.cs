using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations;

/// <summary>
/// Abstract logic class for large aerospace units (small craft, capital, dropship).
/// </summary>
public abstract class LogicUnitAerospaceLarge : LogicUnitAerospace
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicUnitAerospaceLarge"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="mathExpression">The math expression parser.</param>
    /// <param name="random">The random number generator.</param>
    /// <param name="unit">The unit.</param>
    protected LogicUnitAerospaceLarge(ILogger<LogicUnitAerospaceLarge> logger, GameOptions gameOptions, IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random, UnitEntry unit) : base(logger, gameOptions, grainFactory, mathExpression, random, unit)
    {
    }

    /// <inheritdoc />
    public override int GetEvasionModifier()
    {
        return 2;
    }

    /// <inheritdoc />
    public override Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage)
    {
        return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damage));
    }

    /// <inheritdoc />
    protected override async Task<List<Weapon>> GetActiveWeaponsFromBay(WeaponBay weaponBay)
    {
        var weapons = await base.GetActiveWeaponsFromBay(weaponBay);

        var (successful, weapon) = Weapon.CreateWeaponBayWeapon(weapons);

        if (!successful)
        {
            throw new InvalidOperationException("Malformed weapon bay. Cannot resolve combat.");
        }
        else
        {
            return new List<Weapon> { weapon };
        }
    }

    /// <inheritdoc />
    protected override async Task ResolveAmmo(DamageReport hitCalculationDamageReport, CombatAction combatAction)
    {
        if (!combatAction.ActionHappened)
        {
            hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"Combat action was canceled for {combatAction.Weapon.Name} and no ammo will be expended", Type = AttackLogEntryType.Information });
            return;
        }

        // All units may expend ammo when firing, so an unit type check is skipped
        // Bail out if ammo is not used by this weapon or the combat action was canceled
        if (!combatAction.Weapon.UsesAmmo)
        {
            return;
        }

        // In large aerospace craft, all weapons in the bay are combined, so it is not possible to only fire a part of them.
        // I.e. when applying ammo usage, calculate it for all weapons in the bay.
        foreach (var weaponEntry in combatAction.WeaponBay.Weapons)
        {
            var weapon = await FormWeapon(weaponEntry);
            SpendAmmo(hitCalculationDamageReport, weapon);
        }
    }
}