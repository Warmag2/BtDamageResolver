using System;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// The connection response.
/// </summary>
[Serializable]
public class ConnectionResponse
{
    /// <summary>
    /// The authentication token for the player who made the connection request.
    /// </summary>
    public Guid AuthenticationToken { get; set; }

    /// <summary>
    /// The ID of the game this player is connected to, if any.
    /// </summary>
    public string GameId { get; set; }

    /// <summary>
    /// The password of the game this player is connected to, if any.
    /// </summary>
    public string GamePassword { get; set; }

    /// <summary>
    /// Is the user connected or not.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// The ID of the player which made the connection request.
    /// </summary>
    public string PlayerId { get; set; }
}