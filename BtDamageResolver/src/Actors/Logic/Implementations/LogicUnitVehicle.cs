using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations
{
    /// <summary>
    /// Abstract logic class for all ground vechicles and VTOLs.
    /// </summary>
    public abstract class LogicUnitVehicle : LogicUnit
    {
        /// <inheritdoc />
        public LogicUnitVehicle(ILogger<LogicUnitVehicle> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override bool CanTakeMotiveHits()
        {
            return true;
        }

        /// <inheritdoc />
        protected override int GetOwnMovementModifier()
        {
            switch (Unit.MovementClass)
            {
                case MovementClass.Normal:
                    return 1;
                case MovementClass.Fast:
                case MovementClass.Masc:
                    return Unit.HasFeature(UnitFeature.StabilizedWeapons) ? 1 : 2;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets the modifier to motive hits for this unit type.
        /// </summary>
        /// <returns></returns>
        protected abstract int GetMotiveHitModifier();

        /// <inheritdoc />
        protected override async Task ResolveCriticalHit(DamageReport damageReport, Location location, int criticalThreatRoll, int inducingDamage, int transformedDamage, CriticalDamageTableType criticalDamageTableType)
        {
            var criticalDamageTableId = GetCriticalDamageTableName(this, criticalDamageTableType, location);
            var criticalDamageTable = await LogicHelper.GrainFactory.GetCriticalDamageTableRepository().Get(criticalDamageTableId);

            if (criticalDamageTableType == CriticalDamageTableType.Motive)
            {
                switch (Unit.Type)
                {
                    case UnitType.VehicleHover:
                        criticalThreatRoll += 2;
                        break;
                    case UnitType.VehicleWheeled:
                        criticalThreatRoll += 1;
                        break;
                }

                damageReport.Log(new AttackLogEntry
                {
                    Context = "Critical Threat roll modified by unit type",
                    Number = criticalThreatRoll,
                    Type = AttackLogEntryType.Calculation
                });
            }

            if (criticalDamageTable.Mapping[criticalThreatRoll].Any(c => c != CriticalDamageType.None))
            {
                damageReport.DamagePaperDoll.RecordCriticalDamage(location, inducingDamage, CriticalThreatType.Normal, criticalDamageTable.Mapping[criticalThreatRoll]);
                damageReport.Log(new AttackLogEntry
                {
                    Context = string.Join(", ", criticalDamageTable.Mapping[criticalThreatRoll].Select(c => c.ToString())),
                    Number = transformedDamage,
                    Location = location,
                    Type = AttackLogEntryType.Critical
                });
            }
        }

        /// <inheritdoc />
        public override Task<int> TransformDamage(DamageReport damageReport, CombatAction combatAction, int damageAmount)
        {
            return Task.FromResult(ResolveHeatExtraDamage(damageReport, combatAction, damageAmount));
        }
    }
}
