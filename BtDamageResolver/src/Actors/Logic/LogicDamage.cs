using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Orleans;

using static Faemiyah.BtDamageResolver.Actors.Logic.LogicCombatHelpers;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <inheritdoc />
    public class LogicDamage : ILogicDamage
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IResolverRandom _random;
        private readonly IMathExpression _mathExpression;

        /// <summary>
        /// Constructor for damage value calculation logic.
        /// </summary>
        /// <param name="grainFactory">The grain factory.</param>
        /// <param name="random">The random number generator.</param>
        /// <param name="mathExpression">The expression solver.</param>
        public LogicDamage(IGrainFactory grainFactory, IResolverRandom random, IMathExpression mathExpression)
        {
            _grainFactory = grainFactory;
            _random = random;
            _mathExpression = mathExpression;
        }

        /// <inheritdoc />
        public async Task<List<(int damage, List<SpecialDamageEntry> specialDamageEntries)>> ResolveDamageEntries(DamageReport damageReport, int marginOfSuccess, UnitEntry firingUnit, UnitEntry targetUnit, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            // This is a two-phase process. Firstly, the amount of damage done is determined by the firing unit
            // Different units treat the same weapons differently and use them differently
            
            // First, we must determine the total amount of damage dealt
            var damageAmount = await ResolveTotalOutgoingDamage(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode);

            // Then we transform the damage based on the target unit type
            damageAmount = await TransformDamageBasedOnUnitType(damageReport, firingUnit, targetUnit, weapon, mode, damageAmount);

            // Finally, transform damage based on quirks
            damageAmount = TransformDamageAmountBasedOnTargetQuirks(damageReport, marginOfSuccess, targetUnit, weapon, mode, damageAmount);

            // Then we make packets of the damage, as per clustering and rapid fire rules
            var damagePackets = MakeDamagePackets(damageReport, firingUnit, rangeBracket, targetUnit, weapon, mode, damageAmount);

            // Special weapon features which modify or add damage types
            ModifyDamagePacketsBasedOnWeaponFeatures(damageReport, damagePackets, targetUnit, weapon, mode);

            ModifyDamagePacketsBasedOnTargetType(damageReport, damagePackets, targetUnit);

            return damagePackets;
        }

        public List<(int damage, List<SpecialDamageEntry> specialDamageEntries)> ResolveDamageInstance(DamageInstance damageInstance)
        {
            return Clusterize(damageInstance.Damage, damageInstance.ClusterSize, 1, new SpecialDamageEntry { Type = SpecialDamageType.None});
        }

        #region Outgoing damage calculation

        private async Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, int marginOfSuccess, UnitEntry firingUnit, UnitEntry targetUnit, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            int damage;

            switch (firingUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    damage = ResolveOutgoingDamageAerospace(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode);
                    break;
                case UnitType.Infantry:
                    damage = await RapidFireWrapper(damageReport, targetUnit, weapon, mode,
                        async () => await ResolveOutgoingDamageInfantry(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode));
                    break;
                case UnitType.BattleArmor:
                    damage = await RapidFireWrapper(damageReport, targetUnit, weapon, mode,
                        async () => await ResolveOutgoingDamageBattleArmor(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode));
                    break;
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    if (weapon.AttackType != AttackType.Normal)
                    {
                        damage = ResolveOutgoingDamageMelee(damageReport, firingUnit, weapon, mode);
                    }
                    else
                    {
                        damage = await RapidFireWrapper(damageReport, targetUnit, weapon, mode,
                            async () => await ResolveOutgoingDamageNormal(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode));
                    }
                    break;
                case UnitType.Building:
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleVtol:
                case UnitType.VehicleWheeled:
                    damage = await RapidFireWrapper(damageReport, targetUnit, weapon, mode,
                        async () => await ResolveOutgoingDamageNormal(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(firingUnit), "Unexpected unit type.");
            }

            // Apply damage-modifying weapon features
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.PpcCapacitor, out var ppcCapacitorEntry))
            {
                const int addDamage = 5;
                damageReport.Log(new AttackLogEntry { Context = $"{ppcCapacitorEntry.Type} additional damage", Number = addDamage, Type = AttackLogEntryType.Calculation });
                damage += addDamage;
            }

            return damage;
        }

        private int ResolveOutgoingDamageAerospace(DamageReport damageReport, int marginOfSuccess, UnitEntry firingUnit, UnitEntry targetUnit, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            var damageValue = 0;

            switch (weapon.Type)
            {
                case WeaponType.Missile:
                    if (targetUnit.HasFeature(UnitFeature.Ams))
                    {
                        if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.AmsImmune, out _))
                        {
                            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is immune to AMS defenses" });
                        }
                        else
                        {
                            var amsPenalty = _random.Next(6);
                            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Defender AMS roll for cluster damage reduction", Number = amsPenalty });
                            damageValue -= amsPenalty;
                            
                            damageReport.SpendAmmoDefender("AMS", 1);
                        }
                    }

                    if (targetUnit.HasFeature(UnitFeature.Ecm) && !firingUnit.HasFeature(UnitFeature.Bap))
                    {
                        var ecmPenalty = _random.Next(3);
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Defender ECM roll for cluster damage reduction", Number = ecmPenalty });
                        damageValue -= ecmPenalty;
                    }
                    break;
            }

            // Glancing blow for cluster aerospace weapons (improvised rule, since aerospace units do not normally use clustering)
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _) && IsGlancingBlow(marginOfSuccess, targetUnit))
            {
                var glancingBlowPenalty = _random.Next(6);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Defender roll for cluster damage reduction from glancing blow", Number = glancingBlowPenalty});
                damageValue -= glancingBlowPenalty;
            }

            damageValue += Math.Clamp(weapon.DamageAerospace[rangeBracket], 0, int.MaxValue);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total damage value", Number = damageValue });

            return damageValue;
        }

        private async Task<int> RapidFireWrapper(DamageReport damageReport, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, Func<Task<int>> individualDamageCalculation)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                var maxHits = _mathExpression.Parse(rapidFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Rapid fire weapon potential maximum number of hits", Number = maxHits });
                var hits = await ResolveClusterValue(damageReport, targetUnit, weapon, mode, maxHits, 0);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Rapid fire weapon number of hits", Number = hits });

                var damage = 0;
                for(var ii=0; ii<hits; ii++)
                {
                    damage += await individualDamageCalculation();
                }

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total damage after calculating all hits", Number = damage });

                return damage;
            }

            return await individualDamageCalculation();
        }

        private async Task<int> ResolveOutgoingDamageBattleArmor(DamageReport damageReport, int marginOfSuccess, UnitEntry firingUnit, UnitEntry targetUnit, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterBonus = ResolveClusterBonus(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode);

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });
                // The cluster damage reference value is the cluster value of all the troopers combined
                var clusterDamage = weapon.Damage[rangeBracket] * firingUnit.Troopers;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster value from all troopers", Number = clusterDamage });
                return await ResolveClusterValue(damageReport, targetUnit, weapon, mode, clusterDamage, clusterBonus);
            }

            // Default damage calculation path if we did not have a cluster weapon
            // Calculate the number of hits because not all troopers necessarily hit when the squad hits
            var hits = await ResolveClusterValue(damageReport, targetUnit, weapon, mode, firingUnit.Troopers, 0);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Troopers hit count", Number = hits });
            var damage = weapon.Damage[rangeBracket]*hits;
            damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.Calculation, Context = "Total attack damage value", Number = damage});
            return damage;
        }

        private async Task<int> ResolveOutgoingDamageInfantry(DamageReport damageReport, int marginOfSuccess, UnitEntry firingUnit, UnitEntry targetUnit, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            var clusterTable = await _grainFactory.GetClusterTableRepository().Get(weapon.ClusterTable);
            var damage = clusterTable.GetDamage(firingUnit.Troopers);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = $"Cluster table reference for {firingUnit.Troopers} troopers", Number = damage });

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterBonus = ResolveClusterBonus(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode);

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });
                return await ResolveClusterValue(damageReport, targetUnit, weapon, mode, damage, clusterBonus);
            }

            throw new ArgumentOutOfRangeException(nameof(weapon), "All infantry weapons should be cluster weapons.");
        }

        private int ResolveOutgoingDamageMelee(DamageReport damageReport, UnitEntry firingUnit, Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Melee, out var meleeFeatureEntry))
            {
                var meleeDamage = _mathExpression.Parse(meleeFeatureEntry.Data.InsertVariables(firingUnit));
                damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.Calculation, Context = "Melee damage", Number = meleeDamage});

                return meleeDamage;
            }

            throw new InvalidOperationException("Melee attack does not have a melee special feature.");
        }

        private async Task<int> ResolveOutgoingDamageNormal(DamageReport damageReport, int marginOfSuccess, UnitEntry firingUnit, UnitEntry targetUnit, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterBonus = ResolveClusterBonus(damageReport, marginOfSuccess, firingUnit, targetUnit, rangeBracket, weapon, mode);

                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total cluster modifier", Number = clusterBonus });

                return await ResolveClusterValue(damageReport, targetUnit, weapon, mode, weapon.Damage[rangeBracket], clusterBonus);
            }

            var damage = weapon.Damage[rangeBracket];
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Weapon damage value", Number = damage });
            return damage;
        }

        #endregion

        #region Damage transformation

        /// <summary>
        /// Handles damage transformation based on target unit type.
        /// Needed because infantry, vehicles, aerospace units and mechs take damage differently from different weapons.
        /// </summary>
        /// <param name="damageReport">The damage report to write into.</param>
        /// <param name="marginOfSuccess">The margin of success.</param>
        /// <param name="targetUnit">The target unit.</param>
        /// <param name="weapon">The weapon used.</param>
        /// <param name="mode">The weapon mode used.</param>
        /// <param name="damage">The damage before transformation.</param>
        /// <returns>The transformed damage amount.</returns>
        private int TransformDamageAmountBasedOnTargetQuirks(DamageReport damageReport, int marginOfSuccess, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, int damage)
        {
            // Cluster weapons have been affected earlier, so they will not be affected again
            if (!weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _) && IsGlancingBlow(marginOfSuccess, targetUnit))
            {
                // Round down, but minimum is still 1
                var transformedDamage = Math.Max(damage / 2, 1);
                damageReport.Log(new AttackLogEntry { Context = $"Quirk {Quirk.NarrowLowProfile} modifies received damage. New damage", Number = transformedDamage, Type = AttackLogEntryType.Calculation });

                return transformedDamage;
            }

            return damage;
        }

        /// <summary>
        /// Handles damage transformation based on target unit type.
        /// Needed because infantry, vehicles, aerospace units and mechs take damage differently from different weapons.
        /// </summary>
        /// <param name="damageReport">The damage report to write into.</param>
        /// <param name="firingUnit">The firing unit.</param>
        /// <param name="targetUnit">The target unit.</param>
        /// <param name="weapon">The weapon used.</param>
        /// <param name="mode">The weapon mode used.</param>
        /// <param name="damage">The damage before transformation.</param>
        /// <returns>The transformed damage amount.</returns>
        private async Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, int damage)
        {
            // Make transformations for damage, based on targeted unit type
            switch (targetUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.BattleArmor:
                case UnitType.Building:
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleVtol:
                case UnitType.VehicleWheeled:
                    if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Heat, out var heatFeatureEntry))
                    {
                        var addDamage = _mathExpression.Parse(heatFeatureEntry.Data);
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Bonus damage from heat-inflicting weapon", Number = addDamage });
                        damage += addDamage;
                    }
                    break;
                case UnitType.Infantry:
                    damage = await TransformDamageToInfantryDamage(damageReport, firingUnit, targetUnit, weapon, mode, damage);
                    break;
                case UnitType.AerospaceFighter:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetUnit), "Unexpected unit type.");
            }

            return damage;
        }
        
        private async Task<int> TransformDamageToInfantryDamage(DamageReport damageReport, UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, int damage)
        {
            // Battle armor units have special rules when damaging infantry.
            // Typically infantry damage does not care about the number of hits a weapon does, but battle armor unit attacks are resolved individually.
            if (firingUnit.Type == UnitType.BattleArmor)
            {
                if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Burst, out var battleArmorBurstFeatureEntry))
                {
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Troopers with burst weapons attack infantry individually" });
                    var hits = await ResolveClusterValue(damageReport, targetUnit, weapon, mode, firingUnit.Troopers, 0);
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Troopers hit count with AP weapons against infantry", Number = hits });

                    var burstDamage = 0;
                    for (int ii = 0; ii < hits; ii++)
                    {
                        var addDamage = _mathExpression.Parse(battleArmorBurstFeatureEntry.Data.InsertVariables(firingUnit));
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Bonus damage to infantry", Number = addDamage });
                        burstDamage += addDamage;
                    }

                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total bonus damage to infantry", Number = burstDamage });

                    return burstDamage;
                }

                return damage;
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Burst, out var burstFeatureEntry))
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Burst fire weapon overrides infantry damage.", });
                var burstDamage = _mathExpression.Parse(burstFeatureEntry.Data);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Burst fire weapon damage to infantry", Number = burstDamage });
                return burstDamage;
            }
            
            if (weapon.Type == WeaponType.Missile)
            {
                var missileDamage = (int) Math.Ceiling(damage / 5m);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Missile damage to infantry", Number = missileDamage });
                return missileDamage;
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Pulse, out _))
            {
                var pulseDamage = (int)Math.Ceiling(damage / 10m) + 2;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Pulse weapon damage to infantry", Number = pulseDamage });
                return pulseDamage;
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _))
            {
                var clusterDamage = (int) Math.Ceiling(damage / 10m) + 1;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed Cluster weapon damage to infantry", Number = clusterDamage });
                return clusterDamage;
            }

            var transformedDamage = (int)Math.Ceiling(damage / 10m);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Transformed regular weapon damage to infantry", Number = transformedDamage });
            return transformedDamage;
        }

        #endregion

        #region Damage packetization

        private List<(int damage, List<SpecialDamageEntry> specialDamageEntries)> MakeDamagePackets(DamageReport damageReport, UnitEntry firingUnit, RangeBracket rangeBracket, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, int damage)
        {
            // Infantry and building damage does not need to be packetized at all.
            if (targetUnit.Type == UnitType.Infantry)
            {
                return new List<(int, List<SpecialDamageEntry>)>
                {
                    (damage, new List<SpecialDamageEntry> { new SpecialDamageEntry { Data = _mathExpression.Parse(weapon.SpecialDamage[mode].Data).ToString(), Type = weapon.SpecialDamage[mode].Type } })
                };
            }

            switch (firingUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    return MakeDamagePacketsAerospace(damageReport, weapon, mode, damage);
                case UnitType.BattleArmor:
                    return MakeDamagePacketsBattleArmor(damageReport, rangeBracket, weapon, mode, damage);
                case UnitType.Infantry:
                    return MakeDamagePacketsInfantry(weapon, mode, damage);
                case UnitType.Building:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleVtol:
                case UnitType.VehicleWheeled:
                    return MakeDamagePacketsGround(damageReport, rangeBracket, targetUnit, weapon, mode, damage);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private List<(int, List<SpecialDamageEntry>)> MakeDamagePacketsGround(DamageReport damageReport, RangeBracket rangeBracket, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, int damage)
        {
            // Heat weapons are cluster weapons for vulnerable unit types
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Heat, out _))
            {
                switch (targetUnit.Type)
                {
                    case UnitType.Building:
                    case UnitType.AerospaceCapital:
                    case UnitType.AerospaceDropship:
                    case UnitType.BattleArmor:
                    case UnitType.VehicleHover:
                    case UnitType.VehicleTracked:
                    case UnitType.VehicleVtol:
                    case UnitType.VehicleWheeled:
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Weapon acts as a cluster weapon against targeted unit" });
                        return Clusterize(damage, weapon.ClusterSize, weapon.ClusterDamage, weapon.SpecialDamage[mode]);
                }
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _))
            {
                return Clusterize(damage, weapon.ClusterSize, weapon.ClusterDamage, weapon.SpecialDamage[mode]);
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Rapid, out _))
            {
                // Rapid-fire weapons may already have dealt more damage than the individual instance, clusterize to units of the actual damage value
                return Clusterize(damage, weapon.Damage[rangeBracket], 1, weapon.SpecialDamage[mode]);
            }

            // Clustrerize to a single packet
            return Clusterize(damage, damage, 1, weapon.SpecialDamage[mode]);
        }

        private List<(int, List<SpecialDamageEntry>)> MakeDamagePacketsInfantry(Weapon weapon, WeaponMode mode, in int damage)
        {
            return Clusterize(damage, 2, 1, weapon.SpecialDamage[mode]);
        }

        private List<(int, List<SpecialDamageEntry>)> MakeDamagePacketsBattleArmor(DamageReport damageReport, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode, int damage)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _))
            {
                // The total missile damage accounting for trooper amount has been calculated earlier
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total damage value modified by BA trooper amount", Number = damage });
                return Clusterize(damage, weapon.ClusterSize, weapon.ClusterDamage, weapon.SpecialDamage[mode]);
            }

            // If we did not have a cluster weapon, the weapon still may have hit any amount of times due to possible rapid fire and trooper amount
            // Clusterize to hits which match the actual damage value of the weapon
            return Clusterize(damage, weapon.Damage[rangeBracket], 1, weapon.SpecialDamage[mode]);
        }

        private List<(int, List<SpecialDamageEntry>)> MakeDamagePacketsAerospace(DamageReport damageReport, Weapon weapon, WeaponMode mode, int damage)
        {

            // Missile weapons which do 0 damage have been shot down. Return an empty list.
            if (weapon.Type == WeaponType.Missile && damage == 0)
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile weapon has been shot down and does no damage" });
                return new List<(int, List<SpecialDamageEntry>)>();
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Cluster, out _))
            {
                return Clusterize(damage, 5, 1, weapon.SpecialDamage[mode]);
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                return Clusterize(damage, (int)Math.Ceiling((decimal)damage/_mathExpression.Parse(rapidFeatureEntry.Data)), 1, weapon.SpecialDamage[mode]);
            }

            return Clusterize(damage, damage, 1, weapon.SpecialDamage[mode]);
        }

        #endregion

        #region Damage packet modification

        private void ModifyDamagePacketsBasedOnWeaponFeatures(DamageReport damageReport, List<(int damage, List<SpecialDamageEntry> specialDamageEntries)> damagePackets, UnitEntry targetUnit, Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.ArmorPiercing, out var armorPiercingEntry))
            {
                damagePackets[0].specialDamageEntries.Add(new SpecialDamageEntry { Data = armorPiercingEntry.Data, Type = SpecialDamageType.Critical });
                damageReport.Log(new AttackLogEntry { Context = "Armor Piercing weapon feature adds a potential critical hit", Type = AttackLogEntryType.Information });
            }

            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.MeleeCharge, out var chargeEntry) && targetUnit.Type.CanTakeMotiveHits())
            {
                damagePackets[0].specialDamageEntries.Add(new SpecialDamageEntry { Data = chargeEntry.Data, Type = SpecialDamageType.Motive });
                damageReport.Log(new AttackLogEntry { Context = "Melee charge adds a potential motive hit", Type = AttackLogEntryType.Information });
            }
        }

        private void ModifyDamagePacketsBasedOnTargetType(DamageReport damageReport, List<(int damage, List<SpecialDamageEntry> specialDamageEntries)> damagePackets, UnitEntry targetUnit)
        {
            foreach (var (_, specialDamageEntries) in damagePackets)
            {
                foreach (var entry in specialDamageEntries)
                {
                    switch (entry.Type)
                    {
                        case SpecialDamageType.Emp:
                            if (targetUnit.Type == UnitType.BattleArmor || targetUnit.Type == UnitType.Infantry)
                            {
                                damageReport.Log(new AttackLogEntry { Context = "Target unit cannot receive EMP damage, removing special damage entry", Type = AttackLogEntryType.Information });
                                entry.Clear();
                            }
                            break;
                        case SpecialDamageType.Heat:
                            if (!targetUnit.IsHeatTracking())
                            {
                                damageReport.Log(new AttackLogEntry { Context = "Target unit cannot receive Heat damage, removing special damage entry", Type = AttackLogEntryType.Information });
                                entry.Clear();
                            }
                            break;
                    }
                }
            }
        }

        #endregion

        #region Cluster value calculation

        public int ResolveMissileClusterBonus(DamageReport damageReport, UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon, WeaponMode mode)
        {
            var clusterBonus = 0;

            clusterBonus += weapon.ClusterBonus[mode];
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from weapon", Number = weapon.ClusterBonus[mode] });

            if (targetUnit.HasFeature(UnitFeature.Ams))
            {
                if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.AmsImmune, out _))
                {
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is immune to AMS defenses" });
                }
                else
                {
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender AMS", Number = -4 });
                    clusterBonus -= 4;
                    
                    damageReport.SpendAmmoDefender("AMS", 1);
                }
            }

            if (targetUnit.HasFeature(UnitFeature.Ecm) && !firingUnit.HasFeature(UnitFeature.Bap))
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender ECM", Number = -2 });
                clusterBonus -= 2;
            }

            if (targetUnit.Narced)
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster modifier from defender being NARCed", Number = 2 });
                clusterBonus += 2;
            }

            return clusterBonus;
        }

        public int ResolveProjectileClusterBonus(DamageReport damageReport, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Hag, out _))
            {
                switch (rangeBracket) // Only HAG has cluster bonus for projectile weapons and it is treated differently depending on range
                {
                    case RangeBracket.PointBlank:
                    case RangeBracket.Short:
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "HAG cluster modifier from short range", Number = 2 });
                        return 2;
                    case RangeBracket.Medium:
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "HAG cluster modifier from normal range", Number = 0 });
                        return 0;
                    case RangeBracket.Long:
                    case RangeBracket.Extreme:
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "HAG cluster modifier from long range", Number = -2 });
                        return -2;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(rangeBracket), rangeBracket, null);
                }
            }

            return 0;
        }

        private int ResolveClusterBonus(DamageReport damageReport, int marginOfSuccess, UnitEntry firingUnit, UnitEntry targetUnit, RangeBracket rangeBracket, Weapon weapon, WeaponMode mode)
        {
            var clusterBonus = weapon.Type == WeaponType.Missile ?
                ResolveMissileClusterBonus(damageReport, firingUnit, targetUnit, weapon, mode) :
                ResolveProjectileClusterBonus(damageReport, rangeBracket, weapon, mode);

            if (IsGlancingBlow(marginOfSuccess, targetUnit))
            {
                var clusterBonusGlancing = -4;
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Cluster bonus from glancing blow", Number = clusterBonusGlancing });
                clusterBonus += clusterBonusGlancing;
            }

            return clusterBonus;
        }

        private async Task<int> ResolveClusterValue(DamageReport damageReport, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, int damageValue, int clusterBonus)
        {
            int clusterRoll;

            switch (targetUnit.Type)
            {
                case UnitType.Building:
                    clusterRoll = 12;
                    damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Static cluster roll value against a building", Number = clusterRoll });
                    break;
                default:
                {
                    if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Streak, out _))
                    {
                        clusterRoll = 11;
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Static cluster roll value by a streak weapon", Number = clusterRoll });
                    }
                    else
                    {
                        clusterRoll = _random.D26();
                        damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.DiceRoll, Context = "Cluster", Number = clusterRoll});
                    }

                    break;
                }
            }

            clusterRoll = Math.Clamp(clusterRoll + clusterBonus, 2, 12);
            damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.DiceRoll, Context = "Modified cluster", Number = clusterRoll});

            var damageTable = await _grainFactory.GetClusterTableRepository().Get(Names.DefaultClusterTableName);

            var clusterDamage = damageTable.GetDamage(damageValue, clusterRoll);
            damageReport.Log(new AttackLogEntry {Type = AttackLogEntryType.Calculation, Context = "Cluster result", Number = clusterDamage});

            return clusterDamage;
        }

        #endregion

        #region Clusterization and Packetization

        public List<(int, List<SpecialDamageEntry>)> Clusterize(int damage, int weaponClusterSize, int weaponClusterDamage, SpecialDamageEntry specialDamageEntry, bool onlyApplySpecialDamageOnce=true)
        {
            var damageEntries = new List<(int, List<SpecialDamageEntry>)>();
            var first = true;
            
            while (damage > 0)
            {
                var currentClusterSize = Math.Clamp(damage, 1, weaponClusterSize);

                // Typically we only the first cluster hit applies the special damage entry, if any, so clustering does not multiply any special damage
                var clusterSpecialDamageEntry = first && onlyApplySpecialDamageOnce
                    ? new List<SpecialDamageEntry> {
                        new SpecialDamageEntry
                        {
                            Data = _mathExpression.Parse(specialDamageEntry.Data).ToString(),
                            Type = specialDamageEntry.Type
                        }
                    }
                    : new List<SpecialDamageEntry>
                    {
                        new SpecialDamageEntry()
                    };

                damageEntries.Add((currentClusterSize * weaponClusterDamage, clusterSpecialDamageEntry));
                damage -= currentClusterSize;

                first = false;
            }

            return damageEntries;
        }

        #endregion
    }

    #region Extensions

    internal static class VariableExtensions
    {
        public static string InsertVariables(this string input, UnitEntry firingUnit)
        {
            return input
                .Replace(Names.ExpressionVariableNameDistance, firingUnit.FiringSolution.Distance.ToString())
                .Replace(Names.ExpressionVariableNameTonnage, firingUnit.Tonnage.ToString());
        }
    }

    #endregion
}