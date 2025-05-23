using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Partial unit logic class concerning ammo.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc/>
    public async Task<(decimal Estimate, int Max)> ProjectAmmo(int targetNumber, RangeBracket rangeBracket, WeaponEntry weaponEntry)
    {
        var hitChance = GetHitChanceForTargetNumber(targetNumber);

        int ammoUsed;

        // This can only be a single weapon or a multiplied weapon.
        // No need to loop through different ammo usages in the bay.
        var weapon = await FormWeapon(weaponEntry);

        if (!weapon.UsesAmmo || hitChance == 0m)
        {
            return (0m, 0);
        }

        ammoUsed = weapon.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry) ? MathExpression.Parse(rapidFeatureEntry.Data) : 1;

        // If this is a multiplied weapon, multiply the ammo usage
        ammoUsed *= weaponEntry.Amount;

        if (weapon.HasFeature(WeaponFeature.Streak, out var _))
        {
            return (hitChance * ammoUsed, ammoUsed);
        }
        else
        {
            return (ammoUsed, ammoUsed);
        }
    }

    /// <summary>
    /// Resolves the ammo usage of a combat action.
    /// </summary>
    /// <param name="hitCalculationDamageReport">The damage report for hit calculation.</param>
    /// <param name="combatAction">The combat action to process the ammo for.</param>
    /// <returns>A task which finishes when ammo has been resolved.</returns>
    protected virtual Task ResolveAmmo(DamageReport hitCalculationDamageReport, CombatAction combatAction)
    {
        if (!combatAction.ActionHappened)
        {
            hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"Combat action was canceled for {combatAction.Weapon.Name} and no ammo will be expended", Type = AttackLogEntryType.Information });
            return Task.CompletedTask;
        }

        // All units may expend ammo when firing, so an unit type check is skipped
        // Bail out if ammo is not used by this weapon or the combat action was canceled
        if (!combatAction.Weapon.UsesAmmo)
        {
            return Task.CompletedTask;
        }

        SpendAmmo(hitCalculationDamageReport, combatAction.Weapon);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Spends ammo based on rapid fire and other multipliers.
    /// </summary>
    /// <param name="damageReport">The damage report to apply ammo usage to.</param>
    /// <param name="weapon">The weapon to apply ammo usage from.</param>
    protected void SpendAmmo(DamageReport damageReport, Weapon weapon)
    {
        var ammoName = $"{weapon.Name} {weapon.AppliedAmmo}";
        var ammoUsed = 1;

        if (weapon.Instances > 1)
        {
            ammoUsed *= weapon.Instances;
            damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} is a multiplied weapon. Ammo usage multiplier for ammo {ammoName}", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
        }

        if (weapon.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
        {
            ammoUsed *= MathExpression.Parse(rapidFeatureEntry.Data);
            damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} rate of fire multiplier for ammo usage", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
        }

        damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} number of rounds of ammunition expended", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
        damageReport.SpendAmmoAttacker(ammoName, ammoUsed);
    }
}