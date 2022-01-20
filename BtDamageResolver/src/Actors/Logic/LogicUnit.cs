using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Abstract base logic class.
    /// </summary>
    public abstract partial class LogicUnit : ILogicUnit
    {
        protected readonly ILogger<LogicUnit> Logger;
        protected readonly LogicHelper LogicHelper;
        protected readonly GameOptions Options;
        protected readonly UnitEntry Unit;

        /// <summary>
        /// General logic constructor.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="logicHelper">The logic helper class.</param>
        /// <param name="options">The game options.</param>
        /// <param name="unit">The unit entry to construct this logic for.</param>
        public LogicUnit(ILogger<LogicUnit> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit)
        {
            Logger = logger;
            LogicHelper = logicHelper;
            Options = options;
            Unit = unit;
        }

        /// <summary>
        /// Helper method for critical damage table selection, based on attack parameters.
        /// Needed because not all units have their individual paperdoll.
        /// </summary>
        /// <param name="targetType">The UnitType of the target.</param>
        /// <param name="criticalDamageTableType">The type of the critical damage table to use.</param>
        /// <param name="location">The location the attack struck.</param>
        /// <returns></returns>
        protected static string GetCriticalDamageTableName(ILogicUnit target, CriticalDamageTableType criticalDamageTableType, Location location)
        {
            var transformedTargetType = target.GetPaperDollType();

            Location transformedLocation;

            if (target.GetUnit().Type == UnitType.Mech || target.GetUnit().Type == UnitType.Building)
            {
                transformedLocation = Location.Front;
            }
            else
            {
                transformedLocation = location;
            }

            return CriticalDamageTable.GetIdFromProperties(transformedTargetType, criticalDamageTableType, transformedLocation);
        }

        /// <summary>
        /// Helper method for paper doll selection, based on attack parameters and target type.
        /// Needed because not all units have their individual paperdoll.
        /// </summary>
        /// <param name="target">The target unit logic.</param>
        /// <param name="attackType">The type of the attack.</param>
        /// <param name="direction">The direction the attack is coming from.</param>
        /// <param name="gameOptions">The game options.</param>
        /// <returns></returns>
        private static string GetPaperDollNameFromAttackParameters(ILogicUnit target, AttackType attackType, Direction direction, GameOptions gameOptions)
        {
            var transformedTargetType = target.GetPaperDollType();

            // Melee attacks that are not kicks or punches use normal attack tables
            var transformedAttackType = attackType == AttackType.Melee ? AttackType.Normal : attackType;

            var targetType = target.GetUnit().Type;

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

        /// <summary>
        /// Gets the paper doll for a specific attack.
        /// </summary>
        /// <param name="logicUnit">The unit logic.</param>
        /// <param name="attackType">Attack type.</param>
        /// <param name="direction">Attack direction.</param>
        /// <param name="options">Game options</param>
        /// <returns>The paper doll for this attack type.</returns>
        protected async Task<PaperDoll> GetPaperDoll(ILogicUnit logicUnit, AttackType attackType, Direction direction, GameOptions options)
        {
            var paperDollName = GetPaperDollNameFromAttackParameters(logicUnit, attackType, direction, options);
            return await LogicHelper.GrainFactory.GetPaperDollRepository().Get(paperDollName);
        }

        /// <inheritdoc />
        public virtual bool CanTakeEmpHits()
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool CanTakeMotiveHits()
        {
            return false;
        }

        /// <inheritdoc />
        public abstract PaperDollType GetPaperDollType();

        /// <inheritdoc />
        public UnitEntry GetUnit() => Unit;

        /// <inheritdoc />
        public virtual bool IsBlockedByCover(Cover cover, Location location)
        {
            return false;
        }

        /// <inheritdoc />
        public virtual bool IsHeatTracking()
        {
            return false;
        }

        /// <inheritdoc />
        public bool IsGlancingBlow(int marginOfSuccess)
        {
            return Unit.HasFeature(UnitFeature.NarrowLowProfile) && marginOfSuccess == 0;
        }

        /// <inheritdoc />
        public bool IsTagged() => Unit.Tagged;
    }
}
