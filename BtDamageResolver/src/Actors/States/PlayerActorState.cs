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
        public PlayerActorState()
        {
            // When a player is created, set this to zero so that we get all updates
            UpdateTimeStamp = DateTime.MinValue;
            AuthenticationToken = Guid.NewGuid();
            Options = new PlayerOptions();
            UnitEntryIds = new HashSet<Guid>();
            Password = string.Empty;
        }

        public string GameId { get; set; }

        public string GamePassword { get; set; }

        public string Password { get; set; }
        
        public Guid AuthenticationToken { get; set; }

        public bool IsReady { get; set; }

        public HashSet<Guid> UnitEntryIds { get; set; }

        public DateTime UpdateTimeStamp { get; set; }

        public PlayerOptions Options { get; set; }
    }
}