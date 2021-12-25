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
        /// <summary>
        /// Resolves the heat generation of a combat action.
        /// </summary>
        /// <param name="targetDamageReport">The damage report of the target.</param>
        /// <param name="combatAction">The combat action to process the heat for.</param>
        /// <returns>Nothing.</returns>
        protected virtual void ResolveHeat(DamageReport targetDamageReport, CombatAction combatAction)
        {
            targetDamageReport.Log(new AttackLogEntry { Context = $"Units of type {Unit.Type} do not track heat, so {combatAction.Weapon.Name} causes no heat.", Type = AttackLogEntryType.Information });
        }

        protected void ResolveHeatInternal(DamageReport targetDamageReport, CombatAction hit)
        {
            int calculatedSingleHitheat = hit.Weapon.Heat;

            if (hit.Weapon.SpecialFeatures[hit.WeaponMode].HasFeature(WeaponFeature.PpcCapacitor, out _))
            {
                calculatedSingleHitheat += 5;
                targetDamageReport.Log(new AttackLogEntry { Context = $"{hit.Weapon.Name} additional heat load from active PPC capacitor", Number = 5, Type = AttackLogEntryType.Calculation });
            }

            if (hit.Weapon.SpecialFeatures[hit.WeaponMode].HasFeature(WeaponFeature.PpcInhibitorOverride, out _))
            {
                calculatedSingleHitheat += 2;
                targetDamageReport.Log(new AttackLogEntry { Context = $"{hit.Weapon.Name} additional heat load from overriding PPC inhibitor", Number = 5, Type = AttackLogEntryType.Calculation });
            }

            int heat;

            if (hit.Weapon.SpecialFeatures[hit.WeaponMode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                var multiplier = LogicHelper.MathExpression.Parse(rapidFeatureEntry.Data);
                heat = calculatedSingleHitheat * multiplier;
                targetDamageReport.Log(new AttackLogEntry { Context = $"{hit.Weapon.Name} rate of fire multiplier for heat", Number = multiplier, Type = AttackLogEntryType.Calculation });
            }
            else
            {
                heat = calculatedSingleHitheat;
            }

            targetDamageReport.Log(new AttackLogEntry { Context = hit.Weapon.Name, Type = AttackLogEntryType.Heat, Number = heat });

            targetDamageReport.AttackerHeat += heat;
        }
    }
}
