using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class GameState
    {
        public GameState()
        {
            TimeStamp = DateTime.UtcNow;
            TurnTimeStamp = TimeStamp;
        }

        public string AdminId { get; set; }

        public string GameId { get; set; }

        public DateTime TimeStamp { get; set; }

        public int Turn { get; set; }

        public DateTime TurnTimeStamp { get; set; }

        public SortedDictionary<string, PlayerState> Players { get; set; }
    }
}
