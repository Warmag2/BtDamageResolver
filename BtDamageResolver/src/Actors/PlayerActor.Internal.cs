using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        if (_playerActorState.State.AuthenticationToken == token)
        {
            return true;
        }

        await SendErrorMessageToClient("You or another user is trying to use this player actor, but the supplied authorization token was incorrect.");
        return false;
    }

    private ConnectionResponse GetConnectionResponse(bool isConnected)
    {
        var connectionResponse = new ConnectionResponse
        {
            AuthenticationToken = _playerActorState.State.AuthenticationToken,
            GameId = _playerActorState.State.GameId,
            GamePassword = _playerActorState.State.GamePassword,
            IsConnected = isConnected,
            PlayerId = this.GetPrimaryKeyString(),
        };

        return connectionResponse;
    }

    /// <summary>
    /// Get the state of this player.
    /// </summary>
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