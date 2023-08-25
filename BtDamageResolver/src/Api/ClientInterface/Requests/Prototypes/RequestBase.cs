namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;

/// <summary>
/// Base class for requests.
/// </summary>
public abstract class RequestBase
{
    /// <summary>
    /// The name of the player producing this request.
    /// </summary>
    public string PlayerName { get; set; }
}