using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Heat calculation parts of unit logic.
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc/>
        public async Task<(double Estimate, int Max)> ProjectHeat(int targetNumber, WeaponEntry weaponEntry)
        {
            if (!IsHeatTracking())
            {
                return (0d, 0);
            }

            var hitChance = GetHitChanceForTargetNumber(targetNumber);

            int heatGenerated = 0;

            var weapon = await FormWeapon(weaponEntry);

            if (weapon.SpecialFeatures.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                heatGenerated = MathExpression.Parse(rapidFeatureEntry.Data) * weapon.Heat;
            }

            if (weapon.SpecialFeatures.HasFeature(WeaponFeature.Streak, out var _))
            {
                return (hitChance * heatGenerated, heatGenerated);
            }
            else
            {
                return (heatGenerated, heatGenerated);
            }
        }

        /// <summary>
        /// Resolves the heat generation of a combat action.
        /// </summary>
        /// <param name="hitCalculationDamageReport">The damage report for hit calculation.</param>
        /// <param name="combatAction">The combat action to process the heat for.</param>
        protected void ResolveHeat(DamageReport hitCalculationDamageReport, CombatAction combatAction)
        {
            if (!combatAction.ActionHappened)
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"Combat action was canceled for {combatAction.Weapon.Name} and no heat will be generated", Type = AttackLogEntryType.Information });
                return;
            }

            if (!IsHeatTracking())
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"Units of type {Unit.Type} do not track heat, so {combatAction.Weapon.Name} causes no heat.", Type = AttackLogEntryType.Information });
                return;
            }

            int calculatedSingleHitheat = combatAction.Weapon.Heat;
            int heat;

            if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                var multiplier = MathExpression.Parse(rapidFeatureEntry.Data);
                heat = calculatedSingleHitheat * multiplier;
                hitCalculationDamageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} rate of fire multiplier for heat", Number = multiplier, Type = AttackLogEntryType.Calculation });
            }
            else
            {
                heat = calculatedSingleHitheat;
            }

            hitCalculationDamageReport.Log(new AttackLogEntry { Context = combatAction.Weapon.Name, Type = AttackLogEntryType.Heat, Number = heat });
            hitCalculationDamageReport.AttackerHeat += heat;
        }
    }
}
