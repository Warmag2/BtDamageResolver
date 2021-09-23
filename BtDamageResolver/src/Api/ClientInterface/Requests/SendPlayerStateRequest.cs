using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests
{
    /// <summary>
    /// Request for uploading the player state.
    /// </summary>
    public class SendPlayerStateRequest : AuthenticatedRequest
    {
        /// <summary>
        /// The player state.
        /// </summary>
        public PlayerState PlayerState { get; set; }
    }
}