using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class GameActor
    {
        private bool CheckAuthentication(Guid authenticationToken, string playerId = null)
        {
            var contains = _gameActorState.State.AuthenticationTokens.ContainsKey(authenticationToken);

            if (playerId == null)
            {
                if (!contains)
                {
                    _logger.LogWarning("Authentication failure in game {gameId}. Unknown authentication token.", this.GetPrimaryKeyString());
                }

                return contains;
            }

            if (contains)
            {
                return _gameActorState.State.AuthenticationTokens[authenticationToken] == playerId;
            }

            _logger.LogWarning("Authentication failure in game {gameId} for player {playerId}. Incorrect authentication token.", this.GetPrimaryKeyString(), playerId);

            return false;
        }

        private Guid GetAuthenticationTokenForPlayer(string playerId)
        {
            if (_gameActorState.State.AuthenticationTokens.Any(a => a.Value == playerId))
            {
                return _gameActorState.State.AuthenticationTokens.Single(a => a.Value == playerId).Key;
            }

            return Guid.Empty;
        }

        private string GetPlayerForAuthenticationToken(Guid authenticationToken)
        {
            if (_gameActorState.State.AuthenticationTokens.Any(a => a.Key == authenticationToken))
            {
                return _gameActorState.State.AuthenticationTokens.Single(a => a.Key == authenticationToken).Value;
            }

            return null;
        }
    }
}