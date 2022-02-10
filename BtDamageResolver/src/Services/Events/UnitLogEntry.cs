using System;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;

namespace Faemiyah.BtDamageResolver.Services.Events
{
    /// <summary>
    /// The unit log entry.
    /// </summary>
    public class UnitLogEntry
    {
        /// <summary>
        /// The timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The player ID.
        /// </summary>
        public string UnitId { get; set; }

        /// <summary>
        /// The type of the log action.
        /// </summary>
        public UnitActionType ActionType { get; set; }

        /// <summary>
        /// Action data, if any.
        /// </summary>
        public int ActionData { get; set; }
    }
}