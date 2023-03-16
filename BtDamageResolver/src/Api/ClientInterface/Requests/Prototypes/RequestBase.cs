using Orleans;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;

/// <summary>
/// Base class for requests.
/// </summary>
[GenerateSerializer]
public abstract class RequestBase
{
    /// <summary>
    /// The name of the player producing this request.
    /// </summary>
    [Id(0)]
    public string PlayerName { get; set; }
}