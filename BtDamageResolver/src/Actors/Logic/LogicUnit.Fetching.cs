﻿using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Data fetching helper methods for unit logic.
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc />
        public async Task<CriticalDamageTable> GetCriticalDamageTable(CriticalDamageTableType criticalDamageTableType, Location location)
        {
            //var criticalDamageTableId = GetCriticalDamageTableName(target, criticalDamageTableType, location);
            var transformedTargetType = GetPaperDollType();

            Location transformedLocation;

            // Not self-evident, but all mech crit tables are actually just the default crit-ot-not table.
            if (Unit.Type == UnitType.Mech || Unit.Type == UnitType.Building)
            {
                transformedLocation = Location.Front;
            }
            else
            {
                transformedLocation = location;
            }

            var criticalDamageTableId = CriticalDamageTable.GetIdFromProperties(transformedTargetType, criticalDamageTableType, transformedLocation);

            return await GrainFactory.GetCriticalDamageTableRepository().Get(criticalDamageTableId);
        }

        /*/// <summary>
        /// Helper method for critical damage table selection, based on attack parameters.
        /// Needed because not all units have their individual paperdoll.
        /// </summary>
        /// <param name="targetType">The UnitType of the target.</param>
        /// <param name="criticalDamageTableType">The type of the critical damage table to use.</param>
        /// <param name="location">The location the attack struck.</param>
        /// <returns>The name of the critical damage table for the given parameters.</returns>
        private static string GetCriticalDamageTableName(ILogicUnit target, CriticalDamageTableType criticalDamageTableType, Location location)
        {
            var transformedTargetType = target.GetPaperDollType();

            Location transformedLocation;

            // Not self-evident, but all mech crit tables are actually just the default crit-ot-not table.
            if (target.GetUnitType() == UnitType.Mech || target.GetUnitType() == UnitType.Building)
            {
                transformedLocation = Location.Front;
            }
            else
            {
                transformedLocation = location;
            }

            return CriticalDamageTable.GetIdFromProperties(transformedTargetType, criticalDamageTableType, transformedLocation);
        }*/

        /// <summary>
        /// Helper method for paper doll selection, based on attack parameters and target type.
        /// Needed because not all units have their individual paperdoll.
        /// </summary>
        /// <param name="target">The target unit logic.</param>
        /// <param name="attackType">The type of the attack.</param>
        /// <param name="direction">The direction the attack is coming from.</param>
        /// <param name="gameOptions">The game options.</param>
        /// <param name="weaponFeatures">The weapon features, if any.</param>
        /// <returns>The correct paper doll name.</returns>
        private string GetPaperDollNameFromAttackParameters(ILogicUnit target, AttackType attackType, Direction direction, GameOptions gameOptions, List<WeaponFeature> weaponFeatures)
        {
            var transformedTargetType = target.GetPaperDollType();

            var targetType = target.GetUnitType();

            var transformedAttackType = TransformAttackType(target, attackType, weaponFeatures);

            Direction transformedDirection;

            if (targetType == UnitType.Infantry || targetType == UnitType.BattleArmor || targetType == UnitType.Building)
            {
                transformedDirection = Direction.Front;
            }
            else
            {
                transformedDirection = direction;
            }

            // Get alterative paperdolls based on rules
            var transformedRules = new List<Rule>();

            // Floating critical may only apply to mechs
            if (gameOptions.Rules[Rule.FloatingCritical] && transformedAttackType == AttackType.Normal)
            {
                switch (targetType)
                {
                    case UnitType.Mech:
                    case UnitType.MechTripod:
                    case UnitType.MechQuad:
                        transformedRules.Add(Rule.FloatingCritical);
                        break;
                }
            }

            // Improved vehicle survivability may only apply to vehicles
            if (gameOptions.Rules[Rule.ImprovedVehicleSurvivability])
            {
                switch (targetType)
                {
                    case UnitType.VehicleHover:
                    case UnitType.VehicleTracked:
                    case UnitType.VehicleWheeled:
                    case UnitType.VehicleVtol:
                        transformedRules.Add(Rule.ImprovedVehicleSurvivability);
                        break;
                }
            }

            return PaperDoll.GetIdFromProperties(transformedTargetType, transformedAttackType, transformedDirection, transformedRules);
        }

        protected virtual AttackType TransformAttackType(ILogicUnit target, AttackType attackType, List<WeaponFeature> weaponFeatures)
        {
            // Melee attacks that are not kicks or punches use normal attack tables
            var transformedAttackType = attackType == AttackType.Melee ? AttackType.Normal : attackType;

            var targetType = target.GetUnitType();

            // Punch and kick tables only exist for mechs, revert to normal for all other target types
            switch (targetType)
            {
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    // Punches and kicks to prone or crouched mechs can hit anywhere
                    if(target.GetStance() == Stance.Prone || target.GetStance() == Stance.Crouch)
                    {
                        transformedAttackType = AttackType.Normal;
                    }
                    break;
                default:
                    transformedAttackType = AttackType.Normal;
                    break;
            }

            return transformedAttackType;
        }

        /*/// <summary>
        /// Gets the paper doll for a specific attack.
        /// </summary>
        /// <param name="logicUnit">The unit logic.</param>
        /// <param name="attackType">Attack type.</param>
        /// <param name="direction">Attack direction.</param>
        /// <param name="options">Game options</param>
        /// <returns>The paper doll for this attack type.</returns>
        private async Task<PaperDoll> GetPaperDoll(ILogicUnit logicUnit, AttackType attackType, Direction direction, GameOptions options)
        {
            var paperDollName = GetPaperDollNameFromAttackParameters(logicUnit, attackType, direction, options);
            return await GrainFactory.GetPaperDollRepository().Get(paperDollName);
        }*/

        /// <inheritdoc />
        public async Task<DamagePaperDoll> GetDamagePaperDoll(ILogicUnit target, AttackType attackType, Direction direction, List<WeaponFeature> weaponFeatures)
        {
            var paperDollName = GetPaperDollNameFromAttackParameters(target, attackType, direction, GameOptions, weaponFeatures);
            var paperDoll = await GrainFactory.GetPaperDollRepository().Get(paperDollName);

            return paperDoll.GetDamagePaperDoll();
        }
    }
}