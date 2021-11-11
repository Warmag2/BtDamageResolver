﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
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

                // Send personal state objects
                await SendDataToClient(EventNames.ConnectionResponse, GetConnectionResponse(true));
                await SendDataToClient(EventNames.PlayerOptions, _playerActorState.State.Options);

                // Ask for game-related state objects
                await RequestGameState(_playerActorState.State.AuthenticationToken);
                await RequestGameOptions(_playerActorState.State.AuthenticationToken);
                await RequestTargetNumbers(_playerActorState.State.AuthenticationToken);
                await RequestDamageReports(_playerActorState.State.AuthenticationToken);

                // Log the login to permanent store
                await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.Login, 0);

                return true;
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

            if (IsConnectedToGame())
            {
                if (!await LeaveGame(_playerActorState.State.AuthenticationToken))
                {
                    _logger.LogWarning("Player {playerId} has failed to disconnect while signing out.", this.GetPrimaryKeyString());
                    await SendErrorMessageToClient($"Inconsistent state. Player {this.GetPrimaryKeyString()} Unable to sign out of the active game. {_playerActorState.State.GameId}");
                }
            }

            await SendDataToClient(EventNames.ConnectionResponse, GetConnectionResponse(false));

            // Log the logout to permanent store
            await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.Logout, 0);
            await _playerActorState.WriteStateAsync();
            
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> JoinGame(Guid authenticationToken, string gameId, string password)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (IsConnectedToGame())
            {
                if (gameId != _playerActorState.State.GameId)
                {
                    _logger.LogWarning("Player {playerId} trying to connect to a game {oldGameId} while being connected to game {newGameId}. Disconnecting first.", this.GetPrimaryKeyString(), _playerActorState.State.GameId, gameId);
                    if (!await LeaveGame(authenticationToken))
                    {
                        _logger.LogError("Player {playerId} failed to disconnect from {oldGameId}. Cannot join another game.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
                        await SendErrorMessageToClient($"Inconsistent state. Player {this.GetPrimaryKeyString()} claims to be a member of game {_playerActorState.State.GameId} but cannot disconnect.");
                        return false;
                    }
                }

                _logger.LogInformation("Player {playerId} trying to connect to a game {oldGameId} while already connected to it. Falling back to resending join request.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
            }

            return await JoinGameInternal(gameId, password);
        }

        private async Task<bool> JoinGameInternal(string gameId, string password)
        {
            var gameActor = GrainFactory.GetGrain<IGameActor>(gameId);

            if (await gameActor.JoinGame(_playerActorState.State.AuthenticationToken, this.GetPrimaryKeyString(), password))
            {
                _logger.LogInformation("Player {id} successfully connected to the game {game}.", this.GetPrimaryKeyString(),
                    gameId);
                _playerActorState.State.GameId = gameId;
                _playerActorState.State.GamePassword = password;
                await _playerActorState.WriteStateAsync();

                // When we connect to a game, the game is not guaranteed to have our state. Send it and mark all units as updated.
                await gameActor.SendPlayerState(_playerActorState.State.AuthenticationToken, await GetPlayerState(false), _playerActorState.State.UnitEntryIds.ToList());
                // Fetch game options on join
                await RequestGameOptions(_playerActorState.State.AuthenticationToken);
                // Connection state has been updated, so send it
                await SendDataToClient(EventNames.ConnectionResponse, GetConnectionResponse(true));

                return true;
            }

            _logger.LogInformation("Player {id} failed to connect to the game {game}.", this.GetPrimaryKeyString(), gameId);

            return false;
        }

        /// <inheritdoc />
        public async Task<bool> LeaveGame(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (!IsConnectedToGame())
            {
                _logger.LogInformation("Player {id} tried to disconnect from game but is not in a game.", this.GetPrimaryKeyString());
                await MarkDisconnectedStateAndSendToClient();

                return true;
            }

            if (await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).LeaveGame(_playerActorState.State.AuthenticationToken, this.GetPrimaryKeyString()))
            {
                _logger.LogInformation("Player {id} successfully disconnected from the game {game}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
                await MarkDisconnectedStateAndSendToClient();

                return true;
            }

            _logger.LogInformation("Player {id} failed to disconnect from the {game}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);

            return false;
        }

        private async Task MarkDisconnectedStateAndSendToClient()
        {
            _playerActorState.State.GameId = null;
            _playerActorState.State.GamePassword = null;
            _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;
            await SendOnlyThisPlayerGameStateToClient();
            await SendDataToClient(EventNames.ConnectionResponse, GetConnectionResponse(true));
        }
    }
}