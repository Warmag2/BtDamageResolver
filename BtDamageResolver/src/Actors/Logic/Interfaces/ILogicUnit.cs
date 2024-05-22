using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;

/// <summary>
/// The interface for unit logic.
/// </summary>
public interface ILogicUnit
{
    /// <summary>
    /// A getter for the unit object of this logic.
    /// </summary>
    UnitEntry Unit { get; }

    /// <summary>
    /// Can this unit take a critical hit.
    /// </summary>
    /// <returns><b>True</b> if the unit can take critical hits, and <b>false</b> otherwise.</returns>
    bool CanTakeCriticalHits();

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
    /// Gets the modifier to hit resolution from evasion.
    /// </summary>
    /// <returns>The modifier to hit resolution from the evasion of this unit.</returns>
    int GetEvasionModifier();

    /// <summary>
    /// Gets the type of the paper doll for this unit.
    /// </summary>
    /// <returns>The paper doll type this unit uses.</returns>
    PaperDollType GetPaperDollType();

    /// <summary>
    /// Gets the modifier to hit resolution from unit stance.
    /// </summary>
    /// <returns>The modifier to hit resolution from unit stance.</returns>
    int GetStanceModifier();

    /// <summary>
    /// Gets the modifier to hit resolution from unit type.
    /// </summary>
    /// <returns>The modifier to hit resolution from this unit type.</returns>
    int GetUnitTypeModifier();

    /// <summary>
    /// Is the given location behind cover for this unit logic.
    /// </summary>
    /// <param name="cover">The cover.</param>
    /// <param name="location">The location.</param>
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
    /// Calculates projected ammo usage for weapon.
    /// </summary>
    /// <param name="targetNumber">The target number.</param>
    /// <param name="weaponEntry">The weapon entry to calculate projection for.</param>
    /// <returns>A tuple containing the estimated and maximum ammo usage.</returns>
    Task<(decimal Estimate, int Max)> ProjectAmmo(int targetNumber, WeaponEntry weaponEntry);

    /// <summary>
    /// Calculates projected heat generation for weapon.
    /// </summary>
    /// <param name="targetNumber">The target number.</param>
    /// <param name="rangeBracket">The range bracket to calculate the projection for.</param>
    /// <param name="weaponEntry">The weapon entry to calculate the projection for.</param>
    /// <returns>A tuple containing the estimated and maximum heat generation.</returns>
    Task<(decimal Estimate, int Max)> ProjectHeat(int targetNumber, RangeBracket rangeBracket, WeaponEntry weaponEntry);

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
    void TransformCombatAction(DamageReport targetDamageReport, CombatAction combatAction);

    /// <summary>
    /// Does any transformations for damage based on cover.
    /// </summary>
    /// <param name="damageReport">The damage report.</param>
    /// <param name="damageAmount">The amount of damage before transformation.</param>
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

    /// <summary>
    /// Apply given damage packets to this unit logic.
    /// </summary>
    /// <param name="damageReport">The damage report to apply the damage into.</param>
    /// <param name="damagePackets">The damage packets.</param>
    /// <param name="firingSolution">The firing solution.</param>
    /// <param name="marginOfSuccess">The margin of success of the action which generated the damage.</param>
    /// <returns>Nothing.</returns>
    Task ApplyDamagePackets(DamageReport damageReport, List<DamagePacket> damagePackets, FiringSolution firingSolution, int marginOfSuccess);

    /// <summary>
    /// Resolves combat for this unit logic and a specific weapon bay.
    /// </summary>
    /// <param name="target">The target unit logic.</param>
    /// <param name="weaponBay">The weapon bay.</param>
    /// <param name="processOnlyTags">Only process weapons with tagging features.</param>
    /// <param name="isPrimaryTarget">Does this bay attack the primary target.</param>
    /// <returns>A set of damage reports caused by this unit attacking.</returns>
    Task<List<DamageReport>> ResolveCombatForBay(ILogicUnit target, WeaponBay weaponBay, bool processOnlyTags, bool isPrimaryTarget);

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
    /// <param name="weaponBay">The weapon bay the weapon is in.</param>
    /// <param name="isPrimaryTarget">Is this the primary target of the unit.</param>
    /// <returns>A tuple with the hit modifier and the range bracket.</returns>
    (int TargetNumber, RangeBracket RangeBracket) ResolveHitModifier(AttackLog attackLog, ILogicUnit target, Weapon weapon, WeaponBay weaponBay, bool isPrimaryTarget);

    /// <summary>
    /// Resolves a hit modifier and logs events related to the calculation in the given damage report.
    /// </summary>
    /// <param name="attackLog">The attack log to log to.</param>
    /// <param name="target">The target unit logic.</param>
    /// <param name="weaponEntry">The weapon entry used.</param>
    /// <param name="weaponBay">The weapon bay the weapon is in.</param>
    /// <param name="isPrimaryTarget">Is this the primary target of the unit.</param>
    /// <returns>A tuple with the hit modifier and the range bracket.</returns>
    Task<(int TargetNumber, RangeBracket RangeBracket)> ResolveHitModifier(AttackLog attackLog, ILogicUnit target, WeaponEntry weaponEntry, WeaponBay weaponBay, bool isPrimaryTarget);

    /// <summary>
    /// Calculates all heat buildup not related to weapon fire.
    /// </summary>
    /// <returns>A damage report with non-weapon heat information.</returns>
    Task<DamageReport> ResolveNonWeaponHeat();
}