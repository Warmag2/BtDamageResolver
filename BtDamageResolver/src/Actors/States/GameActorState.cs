using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.States
{
    /// <summary>
    /// The internal state of a game actor.
    /// </summary>
    [Serializable]
    public class GameActorState
    {
        public GameActorState()
        {
            TimeStamp = DateTime.UtcNow;
            AuthenticationTokens = new Dictionary<Guid, string>();
            Options = new GameOptions();
            PlayerStates = new SortedDictionary<string, PlayerState>();
            Password = string.Empty;
        }

        public string AdminId { get; set; }

        public Dictionary<Guid, string> AuthenticationTokens { get; set; }

        public GameOptions Options { get; set; }

        public string Password { get; set; }

        public SortedDictionary<string, PlayerState> PlayerStates { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}