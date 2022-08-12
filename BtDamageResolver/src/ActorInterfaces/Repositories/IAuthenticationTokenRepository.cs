using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace Faemiyah.BtDamageResolver.Services.Interfaces
{
    /// <summary>
    /// Repository for storing authentication tokens for users.
    /// </summary>
    /// <remarks>
    /// Run-time and in-memory only.
    /// </remarks>
    public interface IAuthenticationTokenRepository : IGrainWithIntegerKey
    {
        /// <summary>
        /// Get an authentication token for a player ID.
        /// </summary>
        /// <param name="playerId">The player ID.</param>
        /// <returns>An authentication token associated with the player ID.</returns>
        Task<Guid> GetToken(string playerId);

        /// <summary>
        /// Does the token match a player.
        /// </summary>
        /// <param name="playerId">The player ID.</param>
        /// <param name="token">The authentication token.</param>
        /// <returns><b>True</b> if the token was correct, <b>false</b> otherwise.</returns>
        Task<bool> Match(string playerId, Guid token);

        /// <summary>
        /// Renew an authentication token for a player ID.
        /// </summary>
        /// <param name="playerId">The player ID.</param>
        /// <returns>A new authentication token for the associated player ID.</returns>
        Task<Guid> Renew(string playerId);
    }
}
