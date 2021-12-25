using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public abstract class LogicUnitBuilding : LogicUnit
    {
        public LogicUnitBuilding(ILogger<LogicUnitBuilding> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        public override int GetMovementClassModifier()
        {
            return -4;
        }

        protected override int ResolveClusterRoll(DamageReport damageReport, CombatAction combatAction)
        {
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Static cluster roll value against a building", Number = 12 });

            return 12;
        }
    }
}
