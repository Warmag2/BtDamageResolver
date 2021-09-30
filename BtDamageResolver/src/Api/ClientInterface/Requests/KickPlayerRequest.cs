using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests
{
    /// <summary>
    /// Request for kicking a player from the requesting player's game.
    /// </summary>
    public class KickPlayerRequest : AuthenticatedRequest
    {
        /// <summary>
        /// The player to kick.
        /// </summary>
        public string PlayerToKickName { get; set; }
    }
}