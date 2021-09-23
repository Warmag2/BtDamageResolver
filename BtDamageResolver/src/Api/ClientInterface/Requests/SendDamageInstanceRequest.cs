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
        public DamageInstance DamageInstance { get; set; }
    }
}