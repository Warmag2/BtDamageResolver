using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Options
{
    [Serializable]
    public class PlayerOptions
    {
        public PlayerOptions()
        {
            AttackLogEntryVisibility = Enum.GetValues(typeof(AttackLogEntryType)).Cast<AttackLogEntryType>().ToDictionary(a => a, a => true);
            ShowDamageReportsOnMainScreenByDefault = true;
        }

        public Dictionary<AttackLogEntryType, bool> AttackLogEntryVisibility { get; set; }

        public bool ShowAttackLogByDefault { get; set; }

        public bool ShowToolsOnMainScreenByDefault { get; set; }

        public bool ShowDamageReportsOnMainScreenByDefault { get; set; }

        public bool ShowOtherPlayersDamageReportsOnMainScreenByDefault { get; set; }

        public bool ShowDamageRequestGeneratorOnMainScreenByDefault { get; set; }

        public bool ShowOtherUnitsOnMainScreenByDefault { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}