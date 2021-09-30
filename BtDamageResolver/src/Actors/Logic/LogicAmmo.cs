using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public class LogicAmmo : ILogicAmmo
    {
        private readonly IMathExpression _mathExpression;

        /// <summary>
        /// Constructor for heat calculation logic.
        /// </summary>
        /// <param name="mathExpression">The expression solver.</param>
        public LogicAmmo(IMathExpression mathExpression)
        {
            _mathExpression = mathExpression;
        }

        public void ResolveAttackerAmmo(DamageReport damageReport, bool hitHappened, Weapon weapon, WeaponMode mode)
        {
            // All units may expend ammo when firing, so an unit type check is skipped
            // Bail out if ammo is not used by this weapon
            if (!weapon.UsesAmmo)
            {
                return;
            }

            int ammoUsed;

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                ammoUsed = _mathExpression.Parse(rapidFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} rate of fire multiplier for ammo usage", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
            }
            else
            {
                ammoUsed = 1;
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Streak, out _))
            {
                if (!hitHappened)
                {
                    damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} does not obtain lock and expends no ammo", Type = AttackLogEntryType.Information });
                    ammoUsed = 0;
                }
            }

            if (ammoUsed > 0)
            {
                damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} number of rounds of ammunition expended", Number = ammoUsed, Type = AttackLogEntryType.Calculation });
                damageReport.SpendAmmoAttacker(weapon.Name, ammoUsed);
            }
        }
    }
}