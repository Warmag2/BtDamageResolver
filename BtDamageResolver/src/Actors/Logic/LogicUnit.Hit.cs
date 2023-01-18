using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Partial class for unit logic concerning combat action resolution.
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc />
        public void TransformCombatAction(DamageReport targetDamageReport, CombatAction combatAction)
        {
            // Streak hits do not actually fire if they would not hit.
            if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Streak, out _) && !combatAction.HitHappened)
            {
                combatAction.ActionHappened = false;
                targetDamageReport.Log(new AttackLogEntry { Context = $"{combatAction.Weapon.Name} does not obtain lock and does not fire", Type = AttackLogEntryType.Information });
            }
            else
            {
                targetDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Fire, Context = $"{combatAction.Weapon.Name}" });
            }

            // Single missile handling. If AMS destroys the only missile, this causes a total miss.
            // In this type of a case, streak missiles such as a hypothetical SRM-1 would still have fired.
            if (combatAction.Weapon.Type == WeaponType.Missile && Unit.HasFeature(UnitFeature.Ams) && combatAction.HitHappened && (combatAction.Weapon.Damage[combatAction.RangeBracket] == 1 || !combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.Cluster, out _)))
            {
                if (combatAction.Weapon.SpecialFeatures.HasFeature(WeaponFeature.AmsImmune, out _))
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

        /// <summary>
        /// Gets an estimated hit chance for the given target number.
        /// </summary>
        /// <param name="targetNumber">The target number.</param>
        /// <returns>The hit chance.</returns>
        protected static double GetHitChanceForTargetNumber(int targetNumber)
        {
            if (targetNumber > 12)
            {
                return 0d;
            }

            switch (targetNumber)
            {
                case 3: return 35d / 36d;
                case 4: return 33d / 36d;
                case 5: return 30d / 36d;
                case 6: return 26d / 36d;
                case 7: return 21d / 36d;
                case 8: return 15d / 36d;
                case 9: return 10d / 36d;
                case 10: return 6d / 36d;
                case 11: return 3d / 36d;
                case 12: return 1d / 36d;
                default: return 1d;
            }
        }

        private CombatAction ResolveHit(DamageReport hitCalculationDamageReport, ILogicUnit target, Weapon weapon)
        {
            hitCalculationDamageReport.Log(new AttackLogEntry { Context = weapon.Name, Type = AttackLogEntryType.FiringSolution });

            var (targetNumber, rangeBracket) = ResolveHitModifier(hitCalculationDamageReport.AttackLog, target, weapon);

            // Weapons with target numbers above 12 cannot hit
            if (targetNumber > 12)
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = $"{weapon.Name} cannot hit and will not fire" });

                return new CombatAction
                {
                    ActionHappened = false,
                    HitHappened = false,
                    MarginOfSuccess = 0,
                    RangeBracket = rangeBracket,
                    UnitType = Unit.Type,
                    Troopers = Unit.Troopers,
                    Weapon = weapon
                };
            }

            var hitRoll = Random.D26();
            hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "To-hit roll", Number = hitRoll });

            var hitHappened = hitRoll >= targetNumber;

            var combatAction = new CombatAction
            {
                ActionHappened = true,
                HitHappened = hitHappened,
                MarginOfSuccess = hitRoll - targetNumber,
                RangeBracket = rangeBracket,
                UnitType = Unit.Type,
                Troopers = Unit.Troopers,
                Weapon = weapon
            };

            // Transform combat action if necessary
            target.TransformCombatAction(hitCalculationDamageReport, combatAction);

            if (hitHappened)
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Hit, Context = weapon.Name });
            }
            else
            {
                hitCalculationDamageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Miss, Context = weapon.Name });
            }

            // Calculate heat based on whether action happened / hit happened
            ResolveHeat(hitCalculationDamageReport, combatAction);

            // Calculate ammo based on whether action happened / hit happened
            ResolveAmmo(hitCalculationDamageReport, combatAction);

            return combatAction;
        }
    }
}
