using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Interfaces
{
    public interface ILogicHitModifier
    {
        /// <summary>
        /// Resolves a hit modifier and logs events related to the calculation in the given damage report.
        /// </summary>
        /// <param name="attackLog">The attack log to log to.</param>
        /// <param name="options">The game options.</param>
        /// <param name="firingUnit">The firing unit.</param>
        /// <param name="targetUnit">The target unit.</param>
        /// <param name="weapon">The weapon used.</param>
        /// <param name="mode">The weapon mode in use.</param>
        /// <returns>The hit modifier.</returns>
        (int targetNumber, RangeBracket rangeBracket) ResolveHitModifier(AttackLog attackLog, GameOptions options, UnitEntry firingUnit, UnitEntry targetUnit, Weapon weapon, WeaponMode mode);
    }
}