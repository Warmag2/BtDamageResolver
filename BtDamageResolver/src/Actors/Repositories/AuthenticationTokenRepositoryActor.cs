using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.States;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Orleans;
using Orleans.Concurrency;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// A repository for holding authentication tokens.
    /// </summary>
    public class AuthenticationTokenRepositoryActor : Grain, IAuthenticationTokenRepository
    {
        private readonly TwoWayMap<string, Guid> _tokenMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationTokenRepositoryActor"/> class.
        /// </summary>
        public AuthenticationTokenRepositoryActor()
        {
            _tokenMap = new TwoWayMap<string, Guid>();
        }

        /// <inheritdoc/>
        [AlwaysInterleave]
        public Task<Guid> GetToken(string playerId)
        {
            return Task.FromResult(_tokenMap.Get(playerId));
        }

        /// <inheritdoc/>
        [AlwaysInterleave]
        public Task<bool> Match(string playerId, Guid token)
        {
            if (token == Guid.Empty)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(_tokenMap.Get(playerId) == token);
        }

        /// <inheritdoc/>
        public Task<Guid> Renew(string playerId)
        {
            _tokenMap.Delete(playerId);
            var newToken = Guid.NewGuid();

            _tokenMap.Add(playerId, newToken);

            return Task.FromResult(newToken);
        }
    }
}
