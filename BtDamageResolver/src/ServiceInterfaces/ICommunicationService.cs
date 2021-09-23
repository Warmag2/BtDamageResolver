using System.Threading.Tasks;
using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces
{
    /// <summary>
    /// Interface for providing client-server communication to the silo and server-to-client communication for grains.
    /// </summary>
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
        Task Send(string playerId, string envelopeType, object data);
    }
}