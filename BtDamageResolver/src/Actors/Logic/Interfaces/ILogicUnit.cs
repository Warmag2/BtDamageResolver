using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public interface ILogicUnit
    {
        #region Support methods

        /// <summary>
        /// Can this unit take an EMP hit.
        /// </summary>
        /// <returns><b>True</b> if the unit can take EMP hits, and <b>false</b> otherwise.</returns>
        bool CanTakeEmpHits();

        /// <summary>
        /// Can this unit take a motive hit.
        /// </summary>
        /// <returns><b>True</b> if the unit can take motive hits, and <b>false</b> otherwise.</returns>
        bool CanTakeMotiveHits();

        /// <summary>
        /// Gets the modifier to hit resolution from cover.
        /// </summary>
        /// <param name="cover">The cover in the current firing solution.</param>
        /// <returns>The modifier to hit resolution from cover for this unit type.</returns>
        int GetCoverModifier(Cover cover);
        
        /// <summary>
        /// Gets a critical damage table.
        /// </summary>
        /// <param name="criticalDamageTableType">The type of the critical damage table to use.</param>
        /// <param name="location">The location the attack struck.</param>
        /// <returns>The critical damage table for these attack parameters.</returns>
        public Task<CriticalDamageTable> GetCriticalDamageTable(CriticalDamageTableType criticalDamageTableType, Location location);

        /// <summary>
        /// Gets the damage paper doll for a specific attack.
        /// </summary>
        /// <param name="target">The target unit logic.</param>
        /// <param name="attackType">Attack type.</param>
        /// <param name="direction">Attack direction.</param>
        /// <param name="weaponFeatures">The weapon features, if any.</param>
        /// <returns>The damage paper doll for the attack type and direction given.</returns>
        Task<DamagePaperDoll> GetDamagePaperDoll(ILogicUnit target, AttackType attackType, Direction direction, List<WeaponFeature> weaponFeatures);

        /// <summary>
        /// Gets the modifier to hit resolution from weapon features against this unit type.
        /// </summary>
        /// <param name="weapon">The weapon being fired.</param>
        /// <returns>The modifier to hit resolution from weapon features against this unit type.</returns>
        int GetFeatureModifier(Weapon weapon);

        /// <summary>
        /// Gets a the id of the unit represented by this unit logic.
        /// </summary>
        /// <returns>The id of the unit represented by this unit logic.</returns>
        Guid GetId();
        
        /// <summary>
        /// Gets the modifier to hit resolution from movement class.
        /// </summary>
        /// <returns>The modifier to hit resolution from this units movement class.</returns>
        int GetMovementClassModifierBasedOnUnitType();

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
        /// Gets the name of the unit represented by this unit logic.
        /// </summary>
        /// <returns>The name of the unit represented by this unit logic.</returns>
        string GetName();

        /// <summary>
        /// Gets the type of the paper doll for this unit.
        /// </summary>
        /// <returns>The paper doll type this unit uses.</returns>
        PaperDollType GetPaperDollType();

        /// <summary>
        /// Gets the stance this unit is currently in.
        /// </summary>
        /// <returns>The stance this unit is in.</returns>
        Stance GetStance();

        /// <summary>
        /// Gets the modifier to hit resolution from unit stance.
        /// </summary>
        /// <returns>The modifier to hit resolution from unit stance.</returns>
        int GetStanceModifier();

        /// <summary>
        /// Gets the tonnage of the unit represented by this unit logic.
        /// </summary>
        /// <returns>The tonnage of the unit represented by this unit logic.</returns>
        int GetTonnage();

        /// <summary>
        /// Gets the number of troopers of the unit represented by this unit logic.
        /// </summary>
        /// <returns>The number of troopers of the unit represented by this unit logic.</returns>
        int GetTroopers();

        /// <summary>
        /// Gets the modifier to hit resolution from unit type.
        /// </summary>
        /// <returns>The modifier to hit resolution from this unit type.</returns>
        int GetUnitTypeModifier();

        /// <summary>
        /// Gets the unit type.
        /// </summary>
        /// <returns>The unit type.</returns>
        UnitType GetUnitType();

        /// <summary>
        /// Does the unit have a specific feature.
        /// </summary>
        /// <param name="unitFeature">The unit feature to query.</param>
        /// <returns><b>True</b> if the unit has the feature, <b>false</b> otherwise.</returns>
        bool HasFeature(UnitFeature unitFeature);

        /// <summary>
        /// Is the given location behind cover for this unit logic.
        /// </summary>
        /// <returns><b>True</b> if the hit is blocked by cover, and <b>false</b> otherwise.</returns>
        bool IsBlockedByCover(Cover cover, Location location);
        
        /// <summary>
        /// Does this unit track heat.
        /// </summary>
        /// <returns><b>True</b> if the unit tracks heat, and <b>false</b> otherwise.</returns>
        bool IsHeatTracking();

        /// <summary>
        /// Is this combat action a glancing blow or not for this unit.
        /// </summary>
        /// <param name="marginOfSuccess">The margin of success.</param>
        /// <returns><b>True</b> if the combat action results in a glancing blow, and <b>false</b> if it does not.</returns>
        bool IsGlancingBlow(int marginOfSuccess);

        /// <summary>
        /// Get whether the unit isNARCed or not.
        /// </summary>
        /// <returns><b>True</b> if the unit is NARCed, <b>false</b> otherwise.</returns>
        bool IsNarced();

        /// <summary>
        /// Get whether the unit is tagged or not.
        /// </summary>
        /// <returns><b>True</b> if the unit is tagged, <b>false</b> otherwise.</returns>
        bool IsTagged();

        /// <summary>
        /// Transforms a cluster roll based on unit type and possible other properties.
        /// </summary>
        /// <param name="damageReport">The damage report to append to.</param>
        /// <param name="clusterRoll">The cluster roll to transform.</param>
        /// <returns>The transformed cluster roll.</returns>
        int TransformClusterRollBasedOnUnitType(DamageReport damageReport, int clusterRoll);

        /// <summary>
        /// Does any transformations for the combat action.
        /// </summary>
        /// <remarks>
        /// For example, this may invalidate the hit it if it cannot happen due to unit properties or features, etc.
        /// </remarks>
        /// <param name="targetDamageReport">The damage report of the target.</param>
        /// <param name="combatAction">The combat action to transform.</param>
        /// <returns>Nothing.</returns>
        void TransformCombatAction(DamageReport targetDamageReport, CombatAction combatAction);

        /// <summary>
        /// Does any transformations for damage based on cover.
        /// </summary>
        /// <param name="damageReport">The damage report.</param>
        /// <param name="damage">The amount of damage before transformation.</param>
        /// <returns>The transformed damage.</returns>
        int TransformDamageBasedOnStance(DamageReport damageReport, int damageAmount);

        /// <summary>
        /// Does any transformations for damage based on the attack.
        /// </summary>
        /// <param name="damageReport">The damage report.</param>
        /// <param name="combatAction">The combat action to transform.</param>
        /// <param name="damage">The amount of damage before transformation.</param>
        /// <returns>The transformed damage.</returns>
        Task<int> TransformDamageBasedOnUnitType(DamageReport damageReport, CombatAction combatAction, int damage);

        #endregion

        /// <summary>
        /// Apply given damage packets to this unit logic.
        /// </summary>
        /// <param name="targetDamageReport">The damage report to apply the damage into.</param>
        /// <param name="damagePackets">The damage packets.</param>
        /// <param name="firingSolution">The firing solution.</param>
        /// <param name="marginOfSuccess">The margin of success of the action which generated the damage.</param>
        /// <returns>Nothing.</returns>
        Task ApplyDamagePackets(DamageReport damageReport, List<DamagePacket> damagePackets, FiringSolution firingSolution, int marginOfSuccess);

        /// <summary>
        /// Resolves combat for this unit logic.
        /// </summary>
        /// <param name="target">The target unit logic.</param>
        /// <returns>A set of damage reports caused by this unit attacking.</returns>
        Task<List<DamageReport>> ResolveCombat(ILogicUnit target);

        /// <summary>
        /// Resolve the given damage instance.
        /// </summary>
        /// <param name="damageInstance">The damage instance to resolve.</param>
        /// <param name="phase">The phase this damage instance occurs in.</param>
        /// <param name="selfDamage">Should the damage be marked as being done by the unit itself. If not, the instigator is left empty.</param>
        /// <returns>The damage report caused by this damage instance.</returns>
        Task<DamageReport> ResolveDamageInstance(DamageInstance damageInstance, Phase phase, bool selfDamage);

        /// <summary>
        /// Resolves a hit modifier and logs events related to the calculation in the given damage report.
        /// </summary>
        /// <param name="attackLog">The attack log to log to.</param>
        /// <param name="target">The target unit logic.</param>
        /// <param name="weapon">The weapon used.</param>
        /// <returns>A tuple with the hit modifier and the range bracket.</returns>
        (int targetNumber, RangeBracket rangeBracket) ResolveHitModifier(AttackLog attackLog, ILogicUnit target, Weapon weapon);

        /// <summary>
        /// Resolves a hit modifier and logs events related to the calculation in the given damage report.
        /// </summary>
        /// <param name="attackLog">The attack log to log to.</param>
        /// <param name="target">The target unit logic.</param>
        /// <param name="weaponEntry">The weapon entry used.</param>
        /// <returns>A tuple with the hit modifier and the range bracket.</returns>
        Task<(int targetNumber, RangeBracket rangeBracket)> ResolveHitModifier(AttackLog attackLog, ILogicUnit target, WeaponEntry weaponEntry);
    }
}
