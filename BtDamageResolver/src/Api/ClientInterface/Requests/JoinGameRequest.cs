using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests
{
    /// <summary>
    /// The request for joining a game.
    /// </summary>
    public class JoinGameRequest : AuthenticatedRequest
    {
        /// <summary>
        /// The game credentials.
        /// </summary>
        public Credentials Credentials { get; set; }
    }
}