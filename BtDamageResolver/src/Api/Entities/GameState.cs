using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// The game state.
    /// </summary>
    [Serializable]
    public class GameState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameState"/> class.
        /// </summary>
        public GameState()
        {
            TimeStamp = DateTime.UtcNow;
            TurnTimeStamp = TimeStamp;
        }

        /// <summary>
        /// The ID of the admin of this game.
        /// </summary>
        public string AdminId { get; set; }

        /// <summary>
        /// The game ID.
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// The update timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The turn the game is in.
        /// </summary>
        public int Turn { get; set; }

        /// <summary>
        /// The timestamp when the last turn processing occurred.
        /// </summary>
        public DateTime TurnTimeStamp { get; set; }

        /// <summary>
        /// The players in the game and their states.
        /// </summary>
        public SortedDictionary<string, PlayerState> Players { get; set; }
    }
}
