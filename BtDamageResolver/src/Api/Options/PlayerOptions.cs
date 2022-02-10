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
            ShowDamageReportsOnMainScreenByDefault = true;
        }

        /// <summary>
        /// Visibility of various types of entries in the attack log.
        /// </summary>
        public Dictionary<AttackLogEntryType, bool> AttackLogEntryVisibility { get; set; }

        /// <summary>
        /// Should the attack log be visible by default.
        /// </summary>
        public bool ShowAttackLogByDefault { get; set; }

        /// <summary>
        /// Should the tools be expanded by default.
        /// </summary>
        public bool ShowToolsOnMainScreenByDefault { get; set; }

        /// <summary>
        /// Should damage reports be expanded on the main screen by default.
        /// </summary>
        public bool ShowDamageReportsOnMainScreenByDefault { get; set; }

        /// <summary>
        /// Only show damage repots concerning this player on the main screen.
        /// </summary>
        public bool ShowOtherPlayersDamageReportsOnMainScreenByDefault { get; set; }

        /// <summary>
        /// Should the damage request generator be expanded on the main screen by default.
        /// </summary>
        public bool ShowDamageRequestGeneratorOnMainScreenByDefault { get; set; }

        /// <summary>
        /// The update timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}