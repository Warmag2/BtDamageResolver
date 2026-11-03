using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.States.Types;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Internal upkeep methods for the Player Actor.
/// </summary>
public partial class PlayerActor
{
    private async Task<bool> CheckAuthentication(Guid token)
    {
        var now = DateTime.UtcNow;

        if (_playerActorState.State.AuthenticationTokens.Values.Any(authenticationToken => authenticationToken.Token == token && !authenticationToken.IsExpired(now)))
        {
            return true;
        }

        await SendErrorMessageToClient("You or another user is trying to use this player actor, but the supplied authorization token was incorrect or has expired.");
        return false;
    }

    /// <summary>
    /// Removes all expired authentication tokens from the player state.
    /// </summary>
    /// <remarks>
    /// Does not persist the state by itself. Callers that rely on the pruning being durable must write the state.
    /// </remarks>
    /// <returns><b>True</b> if any tokens were removed, <b>false</b> otherwise.</returns>
    private bool PruneExpiredAuthenticationTokens()
    {
        var now = DateTime.UtcNow;
        var expiredClientIds = _playerActorState.State.AuthenticationTokens
            .Where(pair => pair.Value.IsExpired(now))
            .Select(pair => pair.Key)
            .ToList();

        foreach (var clientId in expiredClientIds)
        {
            _playerActorState.State.AuthenticationTokens.Remove(clientId);
        }

        return expiredClientIds.Count != 0;
    }

    /// <summary>
    /// Removes any authentication token with the given value from the player state.
    /// </summary>
    /// <remarks>
    /// Does not persist the state by itself. Callers that rely on the removal being durable must write the state.
    /// </remarks>
    /// <param name="token">The token value to remove.</param>
    private void RemoveAuthenticationToken(Guid token)
    {
        var clientIds = _playerActorState.State.AuthenticationTokens
            .Where(pair => pair.Value.Token == token)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var clientId in clientIds)
        {
            _playerActorState.State.AuthenticationTokens.Remove(clientId);
        }
    }

    private ConnectionResponse GetConnectionResponse(bool isConnected, Guid authenticationToken)
    {
        var connectionResponse = new ConnectionResponse
        {
            AuthenticationToken = authenticationToken,
            GameId = _playerActorState.State.GameId,
            IsConnected = isConnected,
            PlayerId = this.GetPrimaryKeyString(),
        };

        return connectionResponse;
    }

    /// <summary>
    /// Get the state of this player.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="PlayerState"/> aliases the stored <see cref="UnitEntry"/> instances
    /// via a shallow <c>ToList()</c>. This is safe only because the consumer is always reached via
    /// a cross-grain or Redis call, both of which round-trip through the JSON serializer registered
    /// in <c>Silo/Program.cs</c> and therefore produce a deep copy at the boundary. Do not call this
    /// from same-grain code that mutates the result.
    /// </remarks>
    /// <returns>The <see cref="PlayerState"/> object containing the properties of this unit actor.</returns>
    private Task<PlayerState> GetPlayerState()
    {
        var playerState = new PlayerState
        {
            IsReady = _playerActorState.State.IsReady,
            PlayerId = this.GetPrimaryKeyString(),
            TimeStamp = _playerActorState.State.UpdateTimeStamp,
            UnitEntries = _playerActorState.State.UnitEntries.ToList()
        };

        return Task.FromResult(playerState);
    }

    private bool IsConnectedToGame()
    {
        return !string.IsNullOrEmpty(_playerActorState.State.GameId);
    }

    private async Task SendOnlyThisPlayerGameStateToClient()
    {
        await SendDataToClient(
            EventNames.GameState,
            new GameState
            {
                GameId = _playerActorState.State.GameId,
                Players = new SortedDictionary<string, PlayerState> { { this.GetPrimaryKeyString(), await GetPlayerState() } },
                TimeStamp = _playerActorState.State.UpdateTimeStamp
            });
    }

    private async Task SendDataToClient(string eventName, object data)
    {
        await _communicationServiceClient.Send(this.GetPrimaryKeyString(), eventName, data);
    }

    private async Task SendErrorMessageToClient(string errorMessage)
    {
        await SendDataToClient(EventNames.ErrorMessage, new ClientErrorEvent(errorMessage));
    }

    private async Task SendErrorMessageToClient(ClientErrorEvent errorMessage)
    {
        await SendDataToClient(EventNames.ErrorMessage, errorMessage);
    }
}