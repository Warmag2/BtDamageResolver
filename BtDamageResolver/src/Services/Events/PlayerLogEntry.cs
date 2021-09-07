using System;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;

namespace Faemiyah.BtDamageResolver.Services.Events
{
    [Serializable]
    public class PlayerLogEntry
    {
        public DateTime TimeStamp { get; set; }

        public string PlayerId { get; set; }

        public PlayerActionType ActionType { get; set; }

        public int ActionData { get; set; }
    }
}