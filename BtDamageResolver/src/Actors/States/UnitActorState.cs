using System;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors.States
{
    /// <summary>
    /// The internal state of an unit actor.
    /// </summary>
    [Serializable]
    public class UnitActorState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitActorState"/> class.
        /// </summary>
        public UnitActorState()
        {
            // When an unit is created, set this to zero so that we get all updates
            TimeStamp = DateTime.MinValue;
        }

        /// <summary>
        /// Is the unit actor initialized.
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// The unit entry for this unit actor.
        /// </summary>
        public UnitEntry UnitEntry { get; set; }

        /// <summary>
        /// The update timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}