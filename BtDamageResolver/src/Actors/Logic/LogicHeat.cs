using System;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public class LogicHeat : ILogicHeat
    {
        private readonly IMathExpression _mathExpression;

        /// <summary>
        /// Constructor for heat calculation logic.
        /// </summary>
        /// <param name="mathExpression">The expression solver.</param>
        public LogicHeat(IMathExpression mathExpression)
        {
            _mathExpression = mathExpression;
        }

        public void ResolveAttackerHeat(DamageReport damageReport, bool hitHappened, UnitEntry firingUnit, Weapon weapon, WeaponMode mode)
        {
            switch (firingUnit.Type)
            {
                case UnitType.AerospaceFighter:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    CalculateHeat(damageReport, hitHappened, weapon, mode);
                    break;
                case UnitType.Building: // This and the following units do not generate heat when firing. The method does nothing.
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleVtol:
                case UnitType.VehicleWheeled:
                    return;
                default:
                    throw new NotImplementedException($"Heat resolution not implemented for unit type {firingUnit.Type}.");
            }
        }

        private void CalculateHeat(DamageReport damageReport, bool hitHappened, Weapon weapon, WeaponMode mode)
        {
            int heat;

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                var multiplier = _mathExpression.Parse(rapidFeatureEntry.Data);
                heat = weapon.Heat * multiplier;
                damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} rate of fire multiplier for heat", Number = multiplier, Type = AttackLogEntryType.Calculation });
            }
            else
            {
                heat = weapon.Heat;
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.PpcCapacitor, out _))
            {
                heat += 5;
                damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} additional heat load from active PPC capacitor", Number = 5, Type = AttackLogEntryType.Calculation });
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.PpcInhibitorOverride, out _))
            {
                heat += 2;
                damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} additional heat load from overriding PPC inhibitor", Number = 5, Type = AttackLogEntryType.Calculation });
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Streak, out _))
            {
                if (!hitHappened)
                {
                    damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} does not obtain lock and causes no heat", Type = AttackLogEntryType.Information });
                    heat = 0;
                }
            }

            damageReport.Log(new AttackLogEntry { Context = weapon.Name, Type = AttackLogEntryType.Heat, Number = heat});

            damageReport.AttackerHeat += heat;
        }
    }
}