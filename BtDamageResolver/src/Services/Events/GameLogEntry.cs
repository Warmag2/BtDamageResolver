using System;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;

namespace Faemiyah.BtDamageResolver.Services.Events
{
    [Serializable]
    public class GameLogEntry
    {
        public DateTime TimeStamp { get; set; }

        public string GameId { get; set; }

        public GameActionType ActionType { get; set; }

        public int ActionData { get; set; }
    }
}