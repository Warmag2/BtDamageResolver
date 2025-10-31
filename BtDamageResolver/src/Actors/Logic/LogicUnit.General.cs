using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// General top-level methods for resolving combat.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc />
    public virtual async Task<IReadOnlyCollection<DamageReport>> ResolveCombatForBay(ILogicUnit target, WeaponBay weaponBay, bool processOnlyTags, bool isPrimaryTarget)
    {
        var allDamageReports = new List<DamageReport>();
        var damageReportCombatActionPairs = new List<(DamageReport DamageReport, CombatAction CombatAction)>();

        foreach (var (weapon, weaponEntry) in await GetActiveWeaponsFromBay(weaponBay))
        {
            // If not processing tags, skip tag weapons
            if (weapon.HasSpecialDamage(SpecialDamageType.Tag, out _) && !processOnlyTags)
            {
                continue;
            }

            // If processing tags, skip non-tag weapons
            if (!weapon.HasSpecialDamage(SpecialDamageType.Tag, out _) && processOnlyTags)
            {
                continue;
            }

            var hitCalclulationDamageReport = new DamageReport
            {
                Phase = weapon.UsePhase,
                DamagePaperDoll = await GetDamagePaperDoll(target, AttackType.Normal, weaponBay.FiringSolution.Direction, weapon.SpecialFeatures.Select(w => w.Type).ToList()),
                FiringUnitId = Unit.Id,
                FiringUnitName = Unit.Name,
                TargetUnitId = target.Unit.Id,
                TargetUnitName = target.Unit.Name,
                InitialTroopers = target.Unit.Troopers
            };

            var combatAction = ResolveHit(hitCalclulationDamageReport, target, weapon, weaponBay, weaponEntry, isPrimaryTarget);

            damageReportCombatActionPairs.Add((hitCalclulationDamageReport, combatAction));
        }

        // Add damage resolution to damage reports based on combat actions
        foreach (var damageReportCombatActionPairsByPhase in damageReportCombatActionPairs.GroupBy(d => d.CombatAction.Weapon.UsePhase))
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

        if (!processOnlyTags)
        {
            allDamageReports.Add(await ResolveNonWeaponHeat());
        }

        return allDamageReports;
    }

    /// <summary>
    /// Form the weapon list from weapon entries in a bay.
    /// </summary>
    /// <param name="weaponBay">The bay to form the weapon list from.</param>
    /// <returns>The weapon list formed from weapons in the bay.</returns>
    protected virtual async Task<IReadOnlyCollection<(Weapon Weapon, WeaponEntry WeaponEntry)>> GetActiveWeaponsFromBay(WeaponBay weaponBay)
    {
        var weapons = new List<(Weapon, WeaponEntry)>();

        foreach (var weaponEntry in weaponBay.Weapons.Where(w => w.State == WeaponState.Active))
        {
            weapons.Add((await FormWeapon(weaponEntry), weaponEntry));
        }

        return weapons;
    }
}