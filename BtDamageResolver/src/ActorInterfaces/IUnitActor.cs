using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces;

/// <summary>
/// Interface for an unit actor representing the state of a single <see cref="UnitEntry"/>.
/// </summary>
public interface IUnitActor : IGrainWithGuidKey
{
    /// <summary>
    /// Get the state of this unit.
    /// </summary>
    /// <returns>The <see cref="UnitEntry"/> object containing the properties of the unit represented by this <see cref="IUnitActor"/>.</returns>
    Task<UnitEntry> GetUnit();

    /// <summary>
    /// Process a damage instance against the unit represented by this <see cref="IUnitActor"/> and return a damage report.
    /// </summary>
    /// <param name="damageInstance">The damage instance to process.</param>
    /// <param name="gameOptions">The game options.</param>
    /// <returns>A <see cref="DamageReport"/> report corresponding to the damage request.</returns>
    Task<DamageReport> ProcessDamageInstance(DamageInstance damageInstance, GameOptions gameOptions);

    /// <summary>
    /// Perform the fire event of the unit represented by this <see cref="IUnitActor"/> and return a damage report.
    /// </summary>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="processOnlyTags">Only process weapons which can tag.</param>
    /// <returns>A set of <see cref="DamageReport"/>s detailing the effects of the fire event.</returns>
    Task<List<DamageReport>> ProcessFireEvent(GameOptions gameOptions, bool processOnlyTags);

    /// <summary>
    /// Process new target numbers for the unit represented by this <see cref="IUnitActor"/> and return them.
    /// </summary>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="setBlankNumbers">Just blank the numbers instead of really calculating them.</param>
    /// <remarks>Blanking is required for units which target invalid units or are not in a game.</remarks>
    /// <returns>A <see cref="TargetNumberUpdate"/> event corresponding to new target numbers, heat and ammo estimates for the unit represented by this <see cref="IUnitActor"/>.</returns>
    Task<TargetNumberUpdate> ProcessTargetNumbers(GameOptions gameOptions, bool setBlankNumbers = false);

    /// <summary>
    /// Receive a new state for this unit.
    /// </summary>
    /// <param name="unit">The <see cref="UnitEntry"/> object containing the new state of the unit represented by this <see cref="IUnitActor"/>.</param>
    /// <returns><b>True</b> if the unit state was updated, <b>false</b> otherwise.</returns>
    Task<bool> SendState(UnitEntry unit);
}