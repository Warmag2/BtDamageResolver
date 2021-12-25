using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public interface ILogicUnit
    {
        #region Support methods

        /// <summary>
        /// Does any transformations for the combat action.
        /// </summary>
        /// <remarks>
        /// For example, this may invalidate the hit it if it cannot happen due to unit properties or features, etc.
        /// </remarks>
        /// <param name="targetDamageReport">The damage report of the target.</param>
        /// <param name="combatAction">The combat action to transform.</param>
        /// <returns>Nothing.</returns>
        public void TransformCombatAction(DamageReport targetDamageReport, CombatAction combatAction);

        /// <summary>
        /// Does any transformations for damage based on the attack.
        /// </summary>
        /// <param name="targetDamageReport">The damage report of the target.</param>
        /// <param name="combatAction">The combat action to transform.</param>
        /// <param name="damage">The amount of damage before transformation.</param>
        /// <returns>Nothing.</returns>
        public Task<int> TransformDamage(DamageReport targetDamageReport, CombatAction combatAction, int damage);

        /// <summary>
        /// Gets the modifier to hit resolution from cover.
        /// </summary>
        /// <param name="cover">The cover in the current firing solution.</param>
        /// <returns>The modifier to hit resolution from cover for this unit type.</returns>
        int GetCoverModifier(Cover cover);

        /// <summary>
        /// Gets the modifier to hit resolution from weapon features against this unit type.
        /// </summary>
        /// <param name="weapon">The weapon being fired.</param>
        /// <param name="mode">The firing mode of the weapon being fired.</param>
        /// <returns>The modifier to hit resolution from weapon features against this unit type.</returns>
        int GetFeatureModifier(Weapon weapon, WeaponMode mode);

        /// <summary>
        /// Gets the modifier to hit resolution from movement class.
        /// </summary>
        /// <returns>The modifier to hit resolution from this units movement class.</returns>
        int GetMovementClassModifier();

        /// <summary>
        /// Gets the modifier to hit resolution from movement direction.
        /// </summary>
        /// <param name="direction">The direction this unit is being fired from.</param>
        /// <returns>The modifier to hit resolution from movement direction.</returns>
        int GetMovementDirectionModifier(Direction direction);

        /// <summary>
        /// Gets the modifier to hit resolution from movement amount.
        /// </summary>
        /// <returns>The modifier to hit resolution from the movement amount of this unit.</returns>
        int GetMovementModifier();

        /// <summary>
        /// Gets the type of the paper doll for this unit.
        /// </summary>
        /// <returns>The paper doll type this unit uses.</returns>
        PaperDollType GetPaperDollType();

        /// <summary>
        /// Gets the modifier to hit resolution from unit type.
        /// </summary>
        /// <returns>The modifier to hit resolution from this unit type.</returns>
        int GetUnitTypeModifier();

        /// <summary>
        /// Gets the unit data object.
        /// </summary>
        /// <returns>The unit data object of this logic unit.</returns>
        UnitEntry GetUnit();

        /// <summary>
        /// Is this combat action a glancing blow or not for this unit.
        /// </summary>
        /// <param name="combatAction">The combat action to check.</param>
        /// <returns><b>True</b> if the combat action results in a glancing blow, and <b>false</b> if it does not.</returns>
        bool IsGlancingBlow(CombatAction combatAction);

        /// <summary>
        /// Get whether the unit is tagged or not.
        /// </summary>
        /// <returns>Whether the unit is tagged or not.</returns>
        bool IsTagged();

        #endregion

        /// <summary>
        /// Resolves combat for this unit logic.
        /// </summary>
        /// <param name="target">The target unit logic.</param>
        /// <returns>A tuple with the damage reports caused by this unit.</returns>
        Task<(DamageReport selfDamageReport, DamageReport targetDamageReport)> ResolveCombat(ILogicUnit target);
        
        /// <summary>
        /// Resolves a hit modifier and logs events related to the calculation in the given damage report.
        /// </summary>
        /// <param name="attackLog">The attack log to log to.</param>
        /// <param name="target">The target unit logic.</param>
        /// <param name="weapon">The weapon used.</param>
        /// <param name="mode">The weapon mode in use.</param>
        /// <returns>A tuple with the hit modifier and the range bracket.</returns>
        (int targetNumber, RangeBracket rangeBracket) ResolveHitModifier(AttackLog attackLog, ILogicUnit target, Weapon weapon, WeaponMode mode);
    }
}
