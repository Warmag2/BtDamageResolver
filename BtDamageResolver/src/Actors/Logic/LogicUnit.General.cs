using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// General top-level methods for resolving combat.
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc />
        public async Task<List<DamageReport>> ResolveCombat(ILogicUnit target)
        {
            var damageReportCombatActionPairs = new List<(DamageReport DamageReport, CombatAction CombatAction)>();

            foreach (var weaponEntry in Unit.Weapons.Where(w => w.State == WeaponState.Active))
            {
                var weapon = await FormWeapon(weaponEntry);

                var hitCalclulationDamageReport = new DamageReport
                {
                    Phase = weapon.GetUsePhase(),
                    DamagePaperDoll = await GetDamagePaperDoll(target, AttackType.Normal, Unit.FiringSolution.Direction, weapon.SpecialFeatures.Select(w => w.Type).ToList()),
                    FiringUnitId = Unit.Id,
                    FiringUnitName = Unit.Name,
                    TargetUnitId = target.Unit.Id,
                    TargetUnitName = target.Unit.Name,
                    InitialTroopers = target.Unit.Troopers
                };

                var combatAction = ResolveHit(hitCalclulationDamageReport, target, weapon);

                damageReportCombatActionPairs.Add((hitCalclulationDamageReport, combatAction));
            }

            // If we have no actions at all, return an empty list.
            if (!damageReportCombatActionPairs.Any())
            {
                return new List<DamageReport>();
            }

            var allDamageReports = new List<DamageReport>();

            // Add damage resolution to damage reports based on combat actions
            foreach (var damageReportCombatActionPairsByPhase in damageReportCombatActionPairs.GroupBy(d => d.CombatAction.Weapon.GetUsePhase()))
            {
                var targetDamageReportsForPhase = new List<DamageReport>();
                var selfDamageReportsForPhase = new List<DamageReport>();

                foreach (var (damageReport, combatAction) in damageReportCombatActionPairsByPhase)
                {
                    // Do damage resolution only for combat actions which actually happened
                    if (combatAction.ActionHappened)
                    {
                        // Target is unharmed if no hit happened
                        if (combatAction.HitHappened)
                        {
                            damageReport.Merge(await ResolveCombatAction(target, combatAction));
                        }

                        // Attacker may be harmed even if a hit did not occur
                        selfDamageReportsForPhase.AddIfNotNull(await ResolveCombatActionSelf(target, combatAction));
                    }

                    // Hit calculation and combat action pre-calculations are always included
                    targetDamageReportsForPhase.Add(damageReport);
                }

                allDamageReports.AddIfNotNull(targetDamageReportsForPhase.Merge());
                allDamageReports.AddIfNotNull(selfDamageReportsForPhase.Merge());
            }

            return allDamageReports;
        }
    }
}
