using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// The player state.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerState"/> class.
        /// </summary>
        public PlayerState()
        {
            TimeStamp = DateTime.UtcNow;
        }

        /// <summary>
        /// The player ID.
        /// </summary>
        public string PlayerId { get; set; }

        /// <summary>
        /// The update timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Is the player ready to proceed to the next turn.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// The units the player controls.
        /// </summary>
        public List<UnitEntry> UnitEntries { get; set; }
    }
}
