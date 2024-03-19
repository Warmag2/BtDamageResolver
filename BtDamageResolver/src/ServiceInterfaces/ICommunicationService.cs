using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces;

/// <summary>
/// Interface for providing client-server communication to the silo and server-to-client communication for grains.
/// </summary>
[Alias("ICommunicationService")]
public interface ICommunicationService : IGrainService
{
    /// <summary>
    /// Send data to a client.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <param name="envelopeType">The type name of the data.</param>
    /// <param name="data">The data.</param>
    /// <remarks>
    /// The envelope type is a hint for the recipient on how to process the data.
    /// </remarks>
    /// <returns>A task which finishes when the envelope has been sent.</returns>
    [Alias("Send")]
    public Task Send(string playerId, string envelopeType, object data);

    /// <summary>
    /// Send data to multiple clients.
    /// </summary>
    /// <param name="playerIds">The player IDs.</param>
    /// <param name="envelopeType">The type name of the data.</param>
    /// <param name="data">The data.</param>
    /// <remarks>
    /// The envelope type is a hint for the recipient on how to process the data.
    /// </remarks>
    /// <returns>A task which finishes when the envelope has been sent to all recipients.</returns>
    [Alias("SendToMany")]
    public Task SendToMany(List<string> playerIds, string envelopeType, object data);

    /// <summary>
    /// Send data to all clients.
    /// </summary>
    /// <param name="envelopeType">The type name of the data.</param>
    /// <param name="data">The data.</param>
    /// <remarks>
    /// The envelope type is a hint for the recipient on how to process the data.
    /// </remarks>
    /// <returns>A task which finishes when the envelope has been sent to all clients.</returns>
    [Alias("SendToAllClients")]
    public Task SendToAllClients(string envelopeType, object data);
}