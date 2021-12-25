using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public partial class LogicUnit
    {
        /// <summary>
        /// Resolves the ammo usage of a combat action.
        /// </summary>
        /// <param name="targetDamageReport">The damage report of the target.</param>
        /// <param name="combatAction">The combat action to process the heat for.</param>
        /// <returns>Nothing.</returns>
        protected void ResolveAmmo(DamageReport damageReport, CombatAction combatAction)
        {
            // All units may expend ammo when firing, so an unit type check is skipped
            // Bail out if ammo is not used by this weapon or the combat action was canceled
            if (!combatAction.Weapon.UsesAmmo || !combatAction.ActionHappened)
            {
                return;
            }

            int ammoUsed;

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                ammoUsed = LogicHelper.MathExpression.Parse(rapidFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} rate of fire multiplier for ammo usage", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
            }
            else
            {
                ammoUsed = 1;
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Streak, out _))
            {
                if (!combatAction.ActionHappened)
                {
                    damageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} does not obtain lock and expends no ammo", Type = AttackLogEntryType.Information });
                    ammoUsed = 0;
                }
            }

            if (ammoUsed > 0)
            {
                damageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} number of rounds of ammunition expended", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
                damageReport.SpendAmmoAttacker(combatAction.Weapon.Name, ammoUsed);
            }
        }
    }
}
