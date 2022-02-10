using System;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests
{
    /// <summary>
    /// The request for generating a damage instance.
    /// </summary>
    [Serializable]
    public class SendDamageInstanceRequest : AuthenticatedRequest
    {
        /// <summary>
        /// The damage instance.
        /// </summary>
        public DamageInstance DamageInstance { get; set; }
    }
}