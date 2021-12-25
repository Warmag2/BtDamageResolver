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

using static Faemiyah.BtDamageResolver.Actors.Logic.Helpers.LogicCombatHelpers;

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

        public List<(int damage, List<SpecialDamageEntry> specialDamageEntries)> ResolveDamageInstance(DamageInstance damageInstance)
        {
            return Clusterize(damageInstance.Damage, damageInstance.ClusterSize, 1, new SpecialDamageEntry { Type = SpecialDamageType.None});
        }

        #region Outgoing damage calculation

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