using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// General top-level methods for resolving combat
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc />
        public async Task<(DamageReport selfDamageReport, DamageReport targetDamageReport)> ResolveCombat(ILogicUnit target)
        {
            var targetPaperDoll = await GetPaperDoll(target, AttackType.Normal, Unit.FiringSolution.Direction, Options);
            var selfPaperDoll = await GetPaperDoll(target, AttackType.Normal, Unit.FiringSolution.Direction, Options);

            var targetDamageReport = new DamageReport
            {
                DamagePaperDoll = targetPaperDoll.GetDamagePaperDoll(),
                FiringUnitId = Unit.Id,
                FiringUnitName = Unit.Name,
                TargetUnitId = target.GetUnit().Id,
                TargetUnitName = target.GetUnit().Name,
                InitialTroopers = target.GetUnit().Troopers
            };

            var selfDamageReport = new DamageReport
            {
                DamagePaperDoll = selfPaperDoll.GetDamagePaperDoll(),
                FiringUnitId = Unit.Id,
                FiringUnitName = Unit.Name,
                TargetUnitId = Unit.Id,
                TargetUnitName = Unit.Name,
                InitialTroopers = Unit.Troopers
            };

            // Get combat actions
            var combatActions = await ResolveHits(targetDamageReport, target);

            // Form damage reports from combat actions
            foreach (var combatAction in combatActions)
            {
                if (combatAction.HitHappened)
                {
                    targetDamageReport.Merge(await ResolveCombatAction(target, combatAction));
                }

                selfDamageReport.Merge(await ResolveCombatActionSelf(target, combatAction));
            }

            return (selfDamageReport, targetDamageReport);
        }
    }
}
