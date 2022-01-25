﻿using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public partial class LogicUnit
    {
        private CombatAction ResolveHit(DamageReport hitCalculationDamageReport, ILogicUnit target, Weapon weapon, WeaponMode weaponMode)
        {
            var (targetNumber, rangeBracket) = ResolveHitModifier(hitCalculationDamageReport.AttackLog, target, weapon, weaponMode);

            // Weapons with target numbers above 12 cannot hit
            // However, at this point, we will always fire the weapon
            if (targetNumber > 12)
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = $"{weapon.Name} cannot hit" });
            }

            hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Fire, Context = $"{weapon.Name}" });

            var hitRoll = Random.D26();
            hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "To-hit roll", Number = hitRoll });

            var hitHappened = hitRoll >= targetNumber;

            if (hitHappened)
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Hit, Context = weapon.Name });
            }
            else
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Miss, Context = weapon.Name });
            }

            var combatAction = new CombatAction
            {
                ActionHappened = true,
                HitHappened = hitHappened,
                MarginOfSuccess = hitRoll - targetNumber,
                RangeBracket = rangeBracket,
                UnitType = Unit.Type,
                Troopers = Unit.Troopers,
                Weapon = weapon,
                WeaponMode = weaponMode
            };

            // Transform combat action if necessary
            target.TransformCombatAction(hitCalculationDamageReport, combatAction);

            // Calculate heat
            ResolveHeat(hitCalculationDamageReport, combatAction);

            // Calculate ammo
            ResolveAmmo(hitCalculationDamageReport, combatAction);

            return combatAction;
        }

        /// <inheritdoc />
        public void TransformCombatAction(DamageReport targetDamageReport, CombatAction combatAction)
        {
            // Streak hits do not actually fire if they would not hit.
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Streak, out _))
            {
                if (!combatAction.HitHappened)
                {
                    combatAction.ActionHappened = false;
                    targetDamageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} does not obtain lock and does not fire", Type = AttackLogEntryType.Information });
                }
            }

            // Single missile handling. If AMS destroys the only missile, this causes a total miss.
            // In this type of a case, streak missiles such as a hypothetical SRM-1 would still have fired.
            if (combatAction.Weapon.Type == WeaponType.Missile && Unit.HasFeature(UnitFeature.Ams) && combatAction.HitHappened && (combatAction.Weapon.Damage[combatAction.RangeBracket] == 1 || !combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _)))
            {
                if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.AmsImmune, out _))
                {
                    targetDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is immune to AMS defenses" });
                }
                else
                {
                    var singleMissileDefenseRoll = Random.NextPlusOne(6);
                    targetDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Ams defense roll against single missile", Number = singleMissileDefenseRoll });
                    if (singleMissileDefenseRoll >= 4)
                    {
                        combatAction.HitHappened = false;
                        targetDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is destroyed by AMS" });
                    }
                    else
                    {
                        targetDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is not destroyed by AMS" });
                    }

                    targetDamageReport.SpendAmmoDefender("AMS", 1);
                }
            }
        }
    }
}
