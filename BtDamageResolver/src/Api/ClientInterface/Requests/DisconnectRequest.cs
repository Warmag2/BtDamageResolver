using System;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests
{
    /// <summary>
    /// The request for disconnecting from the server.
    /// </summary>
    public class DisconnectRequest : RequestBase
    {
        /// <summary>
        /// The authentication token.
        /// </summary>
        public Guid AuthenticationToken { get; set; }
    }
}