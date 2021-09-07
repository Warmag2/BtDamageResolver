using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Interfaces
{
    public interface ILogicHits
    {
        /// <summary>
        /// Applies hits to an unit paper doll and records them to the damage report given.
        /// </summary>
        /// <param name="damageReport">The damage report to log to.</param>
        /// <param name="rules">Dictionary of game rules.</param>
        /// <param name="firingSolution">The firing solution.</param>
        /// <param name="marginOfSuccess">The margin of success which created this hit.</param>
        /// <param name="targetUnit">The target unit.</param>
        /// <param name="weapon">The weapon which created this hit.</param>
        /// <param name="mode">The weapon mode of the weapon which created this hit.</param>
        /// <param name="damagePackets">The damage packets to apply.</param>
        /// <returns>Nothing.</returns>
        Task ResolveHits(DamageReport damageReport, Dictionary<Rule, bool> rules, FiringSolution firingSolution, int marginOfSuccess, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, List<(int damage, List<SpecialDamageEntry> specialDamageEntries)> damagePackets);
    }
}