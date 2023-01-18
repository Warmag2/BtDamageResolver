using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Partial unit logic class concerning ammo.
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc/>
        public async Task<(double Estimate, int Max)> ProjectAmmo(int targetNumber, WeaponEntry weaponEntry)
        {
            var hitChance = GetHitChanceForTargetNumber(targetNumber);

            int ammoUsed = 0;

            var weapon = await FormWeapon(weaponEntry);

            if (!weapon.UsesAmmo)
            {
                return (0d, 0);
            }

            if (weapon.SpecialFeatures.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                ammoUsed = MathExpression.Parse(rapidFeatureEntry.Data);
            }

            if (weapon.SpecialFeatures.HasFeature(WeaponFeature.Streak, out var _))
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
        /// <param name="combatAction">The combat action to process the heat for.</param>
        protected void ResolveAmmo(DamageReport hitCalculationDamageReport, CombatAction combatAction)
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

            int ammoUsed;

            if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                ammoUsed = MathExpression.Parse(rapidFeatureEntry.Data);
                hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} rate of fire multiplier for ammo usage", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
            }
            else
            {
                ammoUsed = 1;
            }

            hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} number of rounds of ammunition expended", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
            hitCalculationDamageReport.SpendAmmoAttacker(combatAction.Weapon.Name, ammoUsed);
        }
    }
}
