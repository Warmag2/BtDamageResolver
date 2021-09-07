using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Events;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Interfaces.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

using static Faemiyah.BtDamageResolver.Actors.Logic.LogicCombatHelpers;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Logic class for all combat logic.
    /// </summary>
    public class LogicCombat : ILogicCombat
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IMathExpression _mathExpression;
        private readonly IResolverRandom _random;
        private readonly ILogicAmmo _logicAmmo;
        private readonly ILogicDamage _logicDamage;
        private readonly ILogicHeat _logicHeat;

        private readonly ILogicHitModifier _logicHitModifier;
        private readonly ILogicHits _logicHits;

        /// <summary>
        /// Constructor for the combat logic class.
        /// </summary>
        /// <param name="grainFactory">The grain factory.</param>
        /// <param name="random">The random number generator.</param>
        /// <param name="mathExpression">The expression solver.</param>
        /// <param name="logicAmmo">The logic for ammo expenditure.</param>
        /// <param name="logicDamage">The logic for damage amount calculation.</param>
        /// <param name="logicHeat">The logic for attacker heat calculation.</param>
        /// <param name="logicHitModifier">The interface handling hit modifier calculation.</param>
        /// <param name="logicHits">The logic for hit resolution.</param>
        public LogicCombat(IGrainFactory grainFactory, IResolverRandom random, IMathExpression mathExpression, ILogicAmmo logicAmmo, ILogicDamage logicDamage, ILogicHeat logicHeat, ILogicHitModifier logicHitModifier, ILogicHits logicHits)
        {
            _grainFactory = grainFactory;
            _mathExpression = mathExpression;
            _random = random;
            _logicAmmo = logicAmmo;
            _logicDamage = logicDamage;
            _logicHeat = logicHeat;
            _logicHitModifier = logicHitModifier;
            _logicHits = logicHits;
        }

        public async Task<List<DamageReport>> Fire(GameOptions gameOptions, UnitEntry firingUnit)
        {
            var targetUnit = await _grainFactory.GetGrain<IUnitActor>(firingUnit.FiringSolution.TargetUnit).GetUnitState();

            var defenderWeaponDamageReports = new List<DamageReport>();
            var attackerWeaponDamageReports = new List<DamageReport>();
            var defenderMeleeDamageReports = new List<DamageReport>();
            var attackerMeleeDamageReports = new List<DamageReport>();

            // Only attempt to fire any active weapons which can hit
            foreach (var weaponEntry in firingUnit.Weapons.Where(w => w.State == WeaponState.Active))
            {
                var weapon = await _grainFactory.GetWeaponRepository().Get(weaponEntry.WeaponName);
                var (defenderDamageReport, attackerDamageReport) = await FireWeapon(gameOptions, firingUnit, targetUnit, weapon, weaponEntry.Mode, weaponEntry.State);
                switch (weapon.AttackType)
                {
                    case AttackType.Normal:
                        defenderWeaponDamageReports.Add(defenderDamageReport);
                        attackerWeaponDamageReports.Add(attackerDamageReport);
                        break;
                    case AttackType.Kick:
                    case AttackType.Punch:
                    case AttackType.Melee:
                        defenderMeleeDamageReports.Add(defenderDamageReport);
                        attackerMeleeDamageReports.Add(attackerDamageReport);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected weapon attack type: {weapon.AttackType}");
                }
            }

            var weaponPhase = new List<DamageReport> { defenderWeaponDamageReports.Where(d => d != null).ToList().Merge(), attackerWeaponDamageReports.Where(d => d != null).ToList().Merge() };
            var meleePhase = new List<DamageReport> { defenderMeleeDamageReports.Where(d => d != null).ToList().Merge(), attackerMeleeDamageReports.Where(d => d != null).ToList().Merge() };
            weaponPhase = weaponPhase.Where(d => d != null).ToList();
            weaponPhase.ForEach(d => d.Phase = Phase.Weapon);
            meleePhase = meleePhase.Where(d => d != null).ToList();
            meleePhase.ForEach(d => d.Phase = Phase.Melee);

            // Combine lists before returning them
            weaponPhase.AddRange(meleePhase);
            
            return weaponPhase;
        }

        public async Task<DamageReport> ResolveDamageRequest(DamageRequest damageRequest, GameOptions gameOptions)
        {
            var targetUnit = await _grainFactory.GetGrain<IUnitActor>(damageRequest.UnitId).GetUnitState();

            var paperDollName = GetPaperDollNameFromAttackParameters(targetUnit.Type, AttackType.Normal, damageRequest.Direction, gameOptions);
            var paperDoll = await _grainFactory.GetPaperDollRepository().Get(paperDollName);

            var damageReport = new DamageReport
            {
                DamagePaperDoll = paperDoll.GetDamagePaperDoll(),
                FiringUnitId = Guid.Empty,
                TargetUnitId = targetUnit.Id,
                TargetUnitName = targetUnit.Name,
                InitialTroopers = targetUnit.Troopers
            };

            damageReport.Log(new AttackLogEntry { Context = "Damage request total damage", Number = damageRequest.Damage, Type = AttackLogEntryType.Calculation });

            damageReport.Log(new AttackLogEntry { Context = "Damage request cluster size", Number = damageRequest.ClusterSize, Type = AttackLogEntryType.Calculation });

            var damagePackets = _logicDamage.ResolveDamageRequest(damageRequest);
            
            await _logicHits.ResolveHits(damageReport, gameOptions.Rules, new FiringSolution {AttackModifier = 0, Cover = damageRequest.Cover, Direction = damageRequest.Direction, TargetUnit = damageRequest.UnitId}, 999, targetUnit, null, WeaponMode.Normal, damagePackets);

            return damageReport;
        }

        private async Task<(DamageReport defenderDamageReport, DamageReport attackerDamageReport)> FireWeapon(GameOptions gameOptions, UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, WeaponState state)
        {
            var paperDollName = GetPaperDollNameFromAttackParameters(targetUnit.Type, weapon.AttackType, firingUnit.FiringSolution.Direction, gameOptions);
            var paperDoll = await _grainFactory.GetPaperDollRepository().Get(paperDollName);

            var damageReport = new DamageReport
            {
                DamagePaperDoll = paperDoll.GetDamagePaperDoll(),
                FiringUnitId = firingUnit.Id,
                FiringUnitName = firingUnit.Name,
                TargetUnitId = targetUnit.Id,
                TargetUnitName = targetUnit.Name,
                InitialTroopers = targetUnit.Troopers
            };

            // Weapons which are inactive, won't fire. Early exit here.
            if (state != WeaponState.Active)
            {
                //damageReport.Log(new AttackLogEntry { Context = $"{weapon.Name} is inactive and does not fire", Type = AttackLogEntryType.Information });
                return (null, null);
            }

            damageReport.Log(new AttackLogEntry { Context = weapon.Name, Type = AttackLogEntryType.Fire });

            var (targetNumber, rangeBracket) = _logicHitModifier.ResolveHitModifier(damageReport.AttackLog, gameOptions, firingUnit, targetUnit, weapon, mode);

            // Weapons which cannot hit at all, do not fire. Another early exit.
            if (targetNumber > 12)
            {
                //damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.Information, Context = $"{weapon.Name} cannot hit and does not fire"});
                return (null, null);
            }

            var hitRoll = _random.D26();
            damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.DiceRoll, Context = "To-hit roll", Number = hitRoll});

            var hitHappened = hitRoll >= targetNumber;
            var marginOfSuccess = hitRoll - targetNumber;

            _logicHeat.ResolveAttackerHeat(damageReport, hitHappened, firingUnit, weapon, mode);
            _logicAmmo.ResolveAttackerAmmo(damageReport, hitHappened, weapon, mode);

            // Single missile handling. If AMS destroys the only missile, this causes a total miss.
            if (weapon.Type == WeaponType.Missile && targetUnit.HasFeature(UnitFeature.Ams) && hitHappened && (weapon.Damage[rangeBracket] == 1 || !weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _)))
            {
                if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.AmsImmune, out _))
                {
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is immune to AMS defenses" });
                }
                else
                {
                    var singleMissileDefenseRoll = _random.NextPlusOne(6);
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Ams defense roll against single missile", Number = singleMissileDefenseRoll });
                    if (singleMissileDefenseRoll >= 4)
                    {
                        hitHappened = false;
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is destroyed by AMS" });
                    }
                    else
                    {
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is not destroyed by AMS" });
                    }

                    damageReport.SpendAmmoDefender("AMS", 1);
                }
            }

            if (hitHappened)
            {
                damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.Hit, Context = weapon.Name});
                var damagePackets = await _logicDamage.ResolveDamageEntries(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode);
                await _logicHits.ResolveHits(damageReport, gameOptions.Rules, firingUnit.FiringSolution, marginOfSuccess, targetUnit, weapon, mode, damagePackets);

                // Resolve attacker damage for special melee attacks
                var attackerDamageReport = await ResolveAttackerDamage(damageReport, firingUnit, targetUnit, weapon, mode, true, gameOptions);
                return (damageReport, attackerDamageReport);
            }
            else
            {
                damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.Miss, Context = weapon.Name});

                // Resolve attacker damage for special melee attacks
                var attackerDamageReport = await ResolveAttackerDamage(damageReport, firingUnit, targetUnit, weapon, mode, true, gameOptions);
                return (damageReport, attackerDamageReport);
            }
        }

        private async Task<DamageReport> ResolveAttackerDamage(DamageReport damageReport, UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, bool hit, GameOptions gameOptions)
        {
            // Only certain melee weapons have this for now
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.MeleeCharge, out _))
            {
                if (!hit)
                {
                    return null;
                }

                damageReport.Log(new AttackLogEntry {Context = "Attacker is damaged by its charge attack", Type = AttackLogEntryType.Information});
                string attackerDamageStringCharge;
                switch (targetUnit.Type)
                {
                    case UnitType.Building:
                    case UnitType.BattleArmor:
                    case UnitType.AerospaceCapital:
                    case UnitType.AerospaceDropship:
                    case UnitType.Infantry:
                        attackerDamageStringCharge = $"{firingUnit.Tonnage}/10";
                        break;
                    default:
                        attackerDamageStringCharge = $"{targetUnit.Tonnage}/10";
                        break;
                }

                var attackerDamageCharge = _mathExpression.Parse(attackerDamageStringCharge);

                return await ResolveDamageRequest(new DamageRequest
                    {
                        AttackType = AttackType.Normal,
                        ClusterSize = 5,
                        Cover = Cover.None,
                        Damage = attackerDamageCharge,
                        Direction = Direction.Front,
                        TimeStamp = DateTime.UtcNow,
                        UnitId = firingUnit.Id
                    },
                    gameOptions);
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.MeleeDfa, out _))
            {
                if (hit)
                {
                    damageReport.Log(new AttackLogEntry {Context = "Attacker is damaged by its DFA attack", Type = AttackLogEntryType.Information});
                    var attackerDamageDfa = _mathExpression.Parse($"{firingUnit.Tonnage}/5");
                    return await ResolveDamageRequest(new DamageRequest
                        {
                            AttackType = AttackType.Kick,
                            ClusterSize = 5,
                            Cover = Cover.None,
                            Damage = attackerDamageDfa,
                            Direction = Direction.Front,
                            TimeStamp = DateTime.UtcNow,
                            UnitId = firingUnit.Id
                        },
                        gameOptions);
                }
                else
                {
                    damageReport.Log(new AttackLogEntry { Context = "Attacker falls onto its back due to a failed DFA attack", Type = AttackLogEntryType.Information });
                    var attackerDamageDfa = _mathExpression.Parse($"2*{firingUnit.Tonnage}/5");
                    return await ResolveDamageRequest(new DamageRequest
                        {
                            AttackType = AttackType.Normal,
                            ClusterSize = 5,
                            Cover = Cover.None,
                            Damage = attackerDamageDfa,
                            Direction = Direction.Rear,
                            TimeStamp = DateTime.UtcNow,
                            UnitId = firingUnit.Id
                        },
                        gameOptions);
                }
            }

            return null;
        }
    }
}