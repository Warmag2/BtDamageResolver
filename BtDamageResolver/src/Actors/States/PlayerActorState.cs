using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.States
{
    /// <summary>
    /// The internal state of a player actor.
    /// </summary>
    [Serializable]
    public class PlayerActorState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerActorState"/> class.
        /// </summary>
        public PlayerActorState()
        {
            Options = new PlayerOptions();
            PasswordHash = null;
            PasswordSalt = null;
            UnitEntryIds = new HashSet<Guid>();
            UpdateTimeStamp = DateTime.MinValue; // When a player is created, set this to zero so that we get all updates
        }

        /// <summary>
        /// The ID of the game this player is in.
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// The password for the game this player is in.
        /// </summary>
        public string GamePassword { get; set; }

        /// <summary>
        /// The password hash for the player.
        /// </summary>
        public byte[] PasswordHash { get; set; }

        /// <summary>
        /// The password hash for the player.
        /// </summary>
        public byte[] PasswordSalt { get; set; }

        /// <summary>
        /// Is the player ready to proceed to the next turn.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// The IDs of the units this player possesses.
        /// </summary>
        public HashSet<Guid> UnitEntryIds { get; set; }

        /// <summary>
        /// The update timestamp.
        /// </summary>
        public DateTime UpdateTimeStamp { get; set; }

        /// <summary>
        /// The player options for this player.
        /// </summary>
        public PlayerOptions Options { get; set; }
    }
}