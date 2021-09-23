using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests
{
    /// <summary>
    /// Request for uploading game options.
    /// </summary>
    public class SendGameOptionsRequest : AuthenticatedRequest
    {
        /// <summary>
        /// The game options.
        /// </summary>
        public GameOptions GameOptions { get; set; }
    }
}