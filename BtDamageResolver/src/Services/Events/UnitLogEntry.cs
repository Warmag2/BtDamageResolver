using System;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;

namespace Faemiyah.BtDamageResolver.Services.Events
{
    public class UnitLogEntry
    {
        public DateTime TimeStamp { get; set; }

        public string UnitId { get; set; }

        public UnitActionType ActionType { get; set; }

        public int ActionData { get; set; }
    }
}