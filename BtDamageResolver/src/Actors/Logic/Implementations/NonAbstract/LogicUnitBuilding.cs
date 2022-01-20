using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations.NonAbstract
{
    /// <summary>
    /// Logic class for buildings.
    /// </summary>
    public class LogicUnitBuilding : LogicUnit
    {
        public LogicUnitBuilding(ILogger<LogicUnitBuilding> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override int GetMovementClassModifier()
        {
            return -4;
        }

        /// <inheritdoc />
        public override PaperDollType GetPaperDollType()
        {
            return PaperDollType.Building;
        }

        /// <inheritdoc />
        public override int TransformClusterRollBasedOnUnitType(DamageReport damageReport, int clusterRoll)
        {
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Static cluster roll value against a building", Number = 12 });

            return 12;
        }
    }
}
