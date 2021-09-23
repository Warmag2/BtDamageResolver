using System;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes
{
    /// <summary>
    /// Base class for requests.
    /// </summary>
    [Serializable]
    public abstract class RequestBase
    {
        /// <summary>
        /// The name of the player producing this request.
        /// </summary>
        public string PlayerName { get; set; }
    }
}