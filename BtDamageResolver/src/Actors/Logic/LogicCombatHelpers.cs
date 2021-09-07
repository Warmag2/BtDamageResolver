using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public static class LogicCombatHelpers
    {
        /// <summary>
        /// Transform a target type to the appropriate paperdoll type.
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <returns>The paperdoll type</returns>
        public static PaperDollType TransformTargetTypeToPaperDollType(UnitType targetType)
        {
            switch (targetType)
            {
                case UnitType.Building:
                    return PaperDollType.Building;
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                    return PaperDollType.AerospaceCapital;
                case UnitType.AerospaceFighter:
                    return PaperDollType.AerospaceFighter;
                case UnitType.BattleArmor:
                    return PaperDollType.BattleArmor;
                case UnitType.Infantry:
                    return PaperDollType.Trooper;
                case UnitType.Mech:
                    return PaperDollType.Mech;
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleWheeled:
                    return PaperDollType.Vehicle;
                case UnitType.VehicleVtol:
                    return PaperDollType.VehicleVtol;
                default:
                    throw new NotImplementedException($"Handling for paper doll type transformation for target type {targetType} has not yet been implemented.");
            }
        }

        /// <summary>
        /// Helper method for paper doll selection, based on attack parameters and target type.
        /// Needed because not all units have their individual paperdoll.
        /// </summary>
        /// <param name="targetType">The UnitType of the target.</param>
        /// <param name="attackType">The type of the attack.</param>
        /// <param name="direction">The direction the attack is coming from.</param>
        /// <param name="gameOptions">The game options.</param>
        /// <returns></returns>
        public static string GetPaperDollNameFromAttackParameters(UnitType targetType, AttackType attackType, Direction direction, GameOptions gameOptions)
        {
            var transformedTargetType = TransformTargetTypeToPaperDollType(targetType);

            // Melee attacks that are not kicks or punches use normal attack tables
            var transformedAttackType = attackType == AttackType.Melee ? AttackType.Normal : attackType;

            // Punch and kick tables only exist for mechs
            switch (targetType)
            {
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    break;
                default:
                    transformedAttackType = AttackType.Normal;
                    break;
            }

            Direction transformedDirection;

            if (targetType == UnitType.Infantry || targetType == UnitType.BattleArmor || targetType == UnitType.Building)
            {
                transformedDirection = Direction.Front;
            }
            else
            {
                transformedDirection = direction;
            }

            // Get floating critical paperdoll if that is needed. Only for mechs and only for normal attacks.
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

        public static bool IsGlancingBlow(int marginOfSuccess, Unit targetUnit)
        {
            return targetUnit.HasQuirk(Quirk.NarrowLowProfile)
                   && marginOfSuccess == 0;
        }
    }
}