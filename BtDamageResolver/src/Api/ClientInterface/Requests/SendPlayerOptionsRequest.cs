using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests
{
    /// <summary>
    /// Request for uploading player options.
    /// </summary>
    public class SendPlayerOptionsRequest : AuthenticatedRequest
    {
        /// <summary>
        /// The player options.
        /// </summary>
        public PlayerOptions PlayerOptions { get; set; }
    }
}