using System;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;

namespace Faemiyah.BtDamageResolver.Services.Events
{
    /// <summary>
    /// The player log entry.
    /// </summary>
    [Serializable]
    public class PlayerLogEntry
    {
        /// <summary>
        /// The timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The player ID.
        /// </summary>
        public string PlayerId { get; set; }

        /// <summary>
        /// The type of the log action.
        /// </summary>
        public PlayerActionType ActionType { get; set; }

        /// <summary>
        /// Action data, if any.
        /// </summary>
        public int ActionData { get; set; }
    }
}