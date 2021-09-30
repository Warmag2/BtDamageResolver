using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    /// <summary>
    /// Contains the non-volatile data of an unit.
    /// </summary>
    [Serializable]
    public class Unit : NamedEntity
    {
        public Unit()
        {
            Quirks = new HashSet<Quirk>();
            Weapons = new List<WeaponEntry>();
        }

        /// <summary>
        /// The gunnery skill of this unit.
        /// </summary>
        public int Gunnery { get; set; }

        /// <summary>
        /// The piloting skill of this unit.
        /// </summary>
        public int Piloting { get; set; }

        /// <summary>
        /// The features this unit possesses.
        /// </summary>
        public HashSet<UnitFeature> Features { get; set; }

        /// <summary>
        /// How many jump jets does this unit have, if any.
        /// </summary>
        public int JumpJets { get; set; }
        
        /// <summary>
        /// How many jump jets does this unit have, if any.
        /// </summary>
        public HashSet<Quirk> Quirks { get; set; }

        /// <summary>
        /// The base ground or air speed of this unit, when moving at normal speed, without modifications, in units per turn.
        /// </summary>
        public int Speed { get; set; }

        /// <summary>
        /// The unit type of this unit.
        /// </summary>
        public UnitType Type;

        /// <summary>
        /// The tonnage of this unit.
        /// </summary>
        public int Tonnage { get; set; }

        /// <summary>
        /// The number of individual acting troopers left in this battle armor or infantry unit.
        /// </summary>
        public int Troopers { get; set; }

        /// <summary>
        /// The amount of heat this unit sinks per turn.
        /// </summary>
        public int Sinks { get; set; }

        /// <summary>
        /// The weapons this unit has equipped.
        /// </summary>
        public List<WeaponEntry> Weapons { get; set; }

        /// <summary>
        /// Check whether the unit has a specific feature or not.
        /// </summary>
        /// <param name="feature">The feature to check.</param>
        /// <returns><b>True</b> if the unit has the specified feature, <b>false</b> otherwise.</returns>
        public bool HasFeature(UnitFeature feature)
        {
            return Features.Contains(feature);
        }

        /// <summary>
        /// Check whether the unit has a specific quirk or not.
        /// </summary>
        /// <param name="quirk">The quirk to check.</param>
        /// <returns><b>True</b> if the unit has the specified quirk, <b>false</b> otherwise.</returns>
        public bool HasQuirk(Quirk quirk)
        {
            return Quirks.Contains(quirk);
        }

        /// <summary>
        /// Add or remove a feature from an unit.
        /// </summary>
        /// <param name="feature">The feature to alter.</param>
        /// <param name="present">Should the feature be present or not.</param>
        public void SetFeature(UnitFeature feature, bool present)
        {
            Features.Set(feature, present);
        }

        /// <summary>
        /// Add or remove a quirk from an unit.
        /// </summary>
        /// <param name="quirk">The quirk to alter.</param>
        /// <param name="present">Should the quirk be present or not.</param>
        public void SetQuirk(Quirk quirk, bool present)
        {
            Quirks.Set(quirk, present);
        }
    }
}
