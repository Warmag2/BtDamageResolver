using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces
{
    /// <summary>
    /// Interface for an unit actor representing the state of a single <see cref="UnitEntry"/>.
    /// </summary>
    public interface IUnitActor : IGrainWithGuidKey
    {
        /// <summary>
        /// Perform the fire event of the unit represented by this <see cref="IUnitActor"/> and return a damage report.
        /// </summary>
        /// <param name="gameOptions">The game options.</param>
        /// <returns>A <see cref="DamageReport"/> detailing the effects of the fire event.</returns>
        Task<List<DamageReport>> Fire(GameOptions gameOptions);

        /// <summary>
        /// Update target numbers for the unit represented by this <see cref="IUnitActor"/>.
        /// </summary>
        /// <param name="gameOptions">The game options.</param>
        /// <param name="setBlankNumbers">Just blank the numbers instead of really calculating them.</param>
        /// <remarks>Blanking is required for units which target invalid units or are not in a game.</remarks>
        /// <returns>A list of <see cref="TargetNumberUpdate"/> events corresponding to new target numbers for the unit represented by this <see cref="IUnitActor"/>.</returns>
        Task<List<TargetNumberUpdate>> UpdateTargetNumbers(GameOptions gameOptions, bool setBlankNumbers = false);

        /// <summary>
        /// Get the state of this unit.
        /// </summary>
        /// <returns>The <see cref="UnitEntry"/> object containing the properties of the unit represented by this <see cref="IUnitActor"/>.</returns>
        Task<UnitEntry> GetUnitState();

        /// <summary>
        /// Processes a damage instance against the unit represented by this <see cref="IUnitActor"/>.
        /// </summary>
        /// <param name="damageInstance">The damage instance to process.</param>
        /// <param name="gameOptions">The game options.</param>
        /// <returns>A <see cref="DamageReport"/> report corresponding to the damage request.</returns>
        Task<DamageReport> ProcessDamageInstance(DamageInstance damageInstance, GameOptions gameOptions);

        /// <summary>
        /// Update the state of this 
        /// </summary>
        /// <param name="unit">The <see cref="UnitEntry"/> object containing the new state of the unit represented by this <see cref="IUnitActor"/>.</param>
        /// <returns><b>True</b> if the unit state was updated, <b>false</b> otherwise.</returns>
        Task<bool> UpdateState(UnitEntry unit);
    }
}