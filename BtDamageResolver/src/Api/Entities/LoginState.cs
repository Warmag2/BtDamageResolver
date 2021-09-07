using System;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class LoginState
    {
        /// <summary>
        /// The authentication token for this player.
        /// </summary>
        public Guid AuthenticationToken { get; set; }

        /// <summary>
        /// The ID of the game this player is connected to, if any.
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// The password of the game this player is connected to, if any.
        /// </summary>
        public string GamePassword { get; set; }
    }
}