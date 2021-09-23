using System;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes
{
    public abstract class AuthenticatedRequest : RequestBase
    {
        /// <summary>
        /// The authentication token.
        /// </summary>
        public Guid AuthenticationToken { get; set; }
    }
}