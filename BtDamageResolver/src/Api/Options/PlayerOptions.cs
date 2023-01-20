using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Options
{
    /// <summary>
    /// The player options.
    /// </summary>
    [Serializable]
    public class PlayerOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerOptions"/> class.
        /// </summary>
        public PlayerOptions()
        {
            AttackLogEntryVisibility = Enum.GetValues(typeof(AttackLogEntryType)).Cast<AttackLogEntryType>().ToDictionary(a => a, a => true);
        }

        /// <summary>
        /// Visibility of various types of entries in the attack log.
        /// </summary>
        public Dictionary<AttackLogEntryType, bool> AttackLogEntryVisibility { get; set; }

        /// <summary>
        /// Should damage reports be expanded on the main screen by default.
        /// </summary>
        /// <remarks>
        /// <b>True</b> by default.
        /// </remarks>
        public bool DashboardShowDamageReportsByDefault { get; set; } = true;

        /// <summary>
        /// Should the damage request generator be expanded on the main screen by default.
        /// </summary>
        public bool DashboardShowDamageRequestsByDefault { get; set; }

        /// <summary>
        /// Should the tools be expanded by default.
        /// </summary>
        public bool DashboardShowToolsByDefault { get; set; }

        /// <summary>
        /// Highlight fields on units, which have not been altered since last turn.
        /// </summary>
        /// <remarks>
        /// <b>True</b> by default.
        /// </remarks>
        public bool HighlightUnalteredFields { get; set; } = true;

        /// <summary>
        /// Should the attack log be visible by default.
        /// </summary>
        public bool ShowAttackLogByDefault { get; set; }

        /// <summary>
        /// Should also damage reports concerning the unit's own movement be shown.
        /// </summary>
        /// <remarks>
        /// This is simply clutter so it is disabled by default.
        /// </remarks>
        public bool ShowMovementDamageReports { get; set; }

        /// <summary>
        /// Should also damage reports concerning other players be displayed.
        /// </summary>
        public bool ShowOtherPlayersDamageReports { get; set; } = true;

        /// <summary>
        /// The update timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}