using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class PlayerActor
    {
        public async Task<bool> Connect(string password)
        {
            if (password == null)
            {
                _logger.LogError("Player {playerId} connection request is malformed.", this.GetPrimaryKeyString());
                return false;
            }

            if (string.IsNullOrEmpty(_playerActorState.State.Password) || _playerActorState.State.Password.Equals(password))
            {
                _playerActorState.State.Password = password;
                await _playerActorState.WriteStateAsync();
                _logger.LogInformation("Player {playerId} received a successful connection request from a client.", this.GetPrimaryKeyString());

                // Ask for state
                await GetGameState(_playerActorState.State.AuthenticationToken);
                await GetDamageReports(_playerActorState.State.AuthenticationToken);

                var connectionResponse = new ConnectionResponse
                {
                    AuthenticationToken = _playerActorState.State.AuthenticationToken,
                    GameId = _playerActorState.State.GameId,
                    GamePassword = _playerActorState.State.GamePassword,
                    IsConnected = true,
                    PlayerId = this.GetPrimaryKeyString(),
                    PlayerPassword = _playerActorState.State.Password
                };

                await SendDataToClient(EventNames.ConnectionResponse, connectionResponse);

                // Log the login to permanent store
                await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.Login, 0);
            }

            _logger.LogWarning("Player {playerId} has received a failed connection request from a client. Incorrect password.", this.GetPrimaryKeyString());

            await SendErrorMessageToClient($"Player {this.GetPrimaryKeyString()} has received a failed connection request from a client.Incorrect password.");

            return false;
        }

        /// <inheritdoc />
        public async Task<bool> Disconnect(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                _logger.LogWarning("Player {playerId} has received a failed disconnection request from a client. Incorrect password.", this.GetPrimaryKeyString());
                return false;
            }

            if (ConnectedToGame())
            {
                if (!await LeaveGame(_playerActorState.State.AuthenticationToken))
                {
                    _logger.LogWarning("Player {playerId} has failed to disconnect while signing out.", this.GetPrimaryKeyString());
                    await SendErrorMessageToClient($"Inconsistent state. Player {this.GetPrimaryKeyString()} Unable to sign out of the active game. {_playerActorState.State.GameId}");
                }
            }

            var connectionResponse = new ConnectionResponse
            {
                IsConnected = false,
            };

            await SendDataToClient(EventNames.ConnectionResponse, connectionResponse);

            // Log the logout to permanent store
            await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.Logout, 0);
            await _playerActorState.WriteStateAsync();
            
            return true;
        }

        private async Task SendOnlyThisPlayerGameStateToClient()
        {
            await SendDataToClient(EventNames.GameState,
                new GameState
                {
                    GameId = null,
                    Players = new SortedDictionary<string, PlayerState> { { this.GetPrimaryKeyString(), await GetPlayerState(false) } },
                    TimeStamp = _playerActorState.State.UpdateTimeStamp
                });
        }

        public async Task<bool> SendDataToClient(string eventName, object data)
        {
            await _communicationServiceClient.Send(this.GetPrimaryKeyString(), eventName, data);

            return true;
        }

        private async Task SendErrorMessageToClient(string errorMessage)
        {
            await SendDataToClient(EventNames.ErrorMessage, errorMessage);
        }

        private async Task ReportAuthenticationErrorToClient()
        {
            await SendErrorMessageToClient("You or another user is trying to use this player actor, but the supplied authorization token was incorrect.");
        }
    }
}