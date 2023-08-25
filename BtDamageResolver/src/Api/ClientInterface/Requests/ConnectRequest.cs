using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;

/// <summary>
/// Request for connecting to the server.
/// </summary>
public class ConnectRequest : RequestBase
{
    /// <summary>
    /// The credentials for server connection.
    /// </summary>
    public Credentials Credentials { get; set; }
}