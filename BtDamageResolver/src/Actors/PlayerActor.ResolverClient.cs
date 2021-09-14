using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Extensions;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;
using static Faemiyah.BtDamageResolver.Api.Compression.DataHelper;
using TargetNumberUpdate = Faemiyah.BtDamageResolver.Api.Events.TargetNumberUpdate;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class PlayerActor
    {
        private IResolverClient _resolverClient;
        private readonly Queue<DamageReport> _unsentDamageReports;
        private readonly Queue<string> _unsentErrorMessages;
        private readonly Queue<TargetNumberUpdate> _unsentTargetNumberUpdates;
        private GameState _unsentGameState; // No need to store many, as the player will only use the newest regardless

        public async Task<LoginState> Connect(string password)
        {
            if (password == null)
            {
                _logger.LogError("Player {playerId} connection request is malformed.", this.GetPrimaryKeyString());
                return null;
            }

            if (string.IsNullOrEmpty(_playerActorState.State.Password) || _playerActorState.State.Password.Equals(password))
            {
                _resolverClient = null; // This must be done so that the player actor does not try to reuse the old client
                _playerActorState.State.Password = password;
                await _playerActorState.WriteStateAsync();
                _logger.LogInformation("Player {playerId} received a successful connection request from a client.", this.GetPrimaryKeyString());

                // Log the login to permanent store
                await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.Login, 0);

                // Ask for state
                await RequestGameState(_playerActorState.State.AuthenticationToken);
                await RequestDamageReports(_playerActorState.State.AuthenticationToken);

                return new LoginState
                {
                    AuthenticationToken = _playerActorState.State.AuthenticationToken,
                    GameId = _playerActorState.State.GameId,
                    GamePassword = _playerActorState.State.GamePassword
                };
            }

            _logger.LogWarning("Player {playerId} has received a failed connection request from a client. Incorrect password.", this.GetPrimaryKeyString());

            return null;
        }

        /// <inheritdoc />
        public async Task<bool> Disconnect(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                _logger.LogWarning("Player {playerId} has received a failed disconnection request from a client. Incorrect password.", this.GetPrimaryKeyString());
                return false;
            }

            if (!await LeaveGame(_playerActorState.State.AuthenticationToken))
            {
                _logger.LogWarning("Player {playerId} has failed to disconnect while signing out.", this.GetPrimaryKeyString());
                await SendErrorMessageToClient($"Inconsistent state. Player {this.GetPrimaryKeyString()} Unable to sign out of the active game. {_playerActorState.State.GameId}");
            }

            // Log the logout to permanent store
            await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.Logout, 0);
            _resolverClient = null; // Do not try to reuse the old client in the future
            await _playerActorState.WriteStateAsync();
            
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> ConnectSubscriber(Guid authenticationToken, IResolverClient client)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (client == null)
            {
                _logger.LogError("Player {playerId} cannot connect to the subscriber. The Resolver Client is null.", this.GetPrimaryKeyString());
                return false;
            }

            _resolverClient = client;
            _logger.LogInformation("Player {playerId} has received an observer reference from the client.", this.GetPrimaryKeyString());
            ClearUnsentQueues().Ignore();

            return true;
        }

        private async Task ClearUnsentQueues()
        {
            foreach (var message in _unsentErrorMessages.DumpIntoList())
            {
                await SendErrorMessageToClient(message);
            }

            await SendDataToClient(_unsentGameState);

            await SendDataToClient(_unsentDamageReports.DumpIntoList());

            await SendDataToClient(_unsentTargetNumberUpdates.DumpIntoList());
        }

        private async Task SendOnlyOwnDataToClient()
        {
            await SendDataToClient(new GameState
            {
                GameId = null,
                Players = new SortedDictionary<string, PlayerState> { {this.GetPrimaryKeyString(), await GetPlayerState(false) }},
                TimeStamp = _playerActorState.State.UpdateTimeStamp
            });
        }

        /// <inheritdoc />
        public async Task<bool> SendDamageReportsToClient(Guid authenticationToken, List<DamageReport> damageReport)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} is sending damage reports to client.", this.GetPrimaryKeyString());
            return await SendDataToClient(damageReport);
        }

        /// <inheritdoc />
        public async Task<bool> SendGameOptionsToClient(Guid authenticationToken, GameOptions gameOptions)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} is sending a game options update to client.", this.GetPrimaryKeyString());
            return await SendDataToClient(gameOptions);
        }

        /// <inheritdoc />
        public async Task<bool> SendGameStateToClient(Guid authenticationToken, GameState gameState)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} is sending a game state update to client.", this.GetPrimaryKeyString());
            return await SendDataToClient(gameState);
        }

        /// <inheritdoc />
        public async Task<bool> SendTargetNumberUpdatesToClient(Guid authenticationToken, List<TargetNumberUpdate> targetNumberUpdates)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogInformation("Player {playerId} is sending {num} target number updates to client.", this.GetPrimaryKeyString(), targetNumberUpdates.Count);
            return await SendDataToClient(targetNumberUpdates);
        }

        /// <inheritdoc />
        public async Task<bool> SendPingToClient(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            _logger.LogDebug("Player {playerId} is sending ping to client.", this.GetPrimaryKeyString());
            return await SendDataToClient(new Ping());
        }

        private async Task<bool> SendDataToClient<TType>(TType data) where TType : class
        {
            if (_resolverClient != null)
            {
                try
                {
                    var uncompressedData = Serialize(data);
                    var compressedData = Compress(uncompressedData);
                    _logger.LogDebug("Player {playerId} is compressing data. Uncompressed: {uncompressedBytes}, Compressed: {compressedBytes}", this.GetPrimaryKeyString(), uncompressedData.Length, compressedData.Length);
                    
                    await _resolverClient.SendCompressedData(compressedData, typeof(TType));
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Player {playerId} encountered a fatal error while sending data to client.", this.GetPrimaryKeyString());
                }
            }
            else
            {
                _logger.LogError("Player {playerId} does not have a connection to client. Unable to send data to client.", this.GetPrimaryKeyString());
            }

            FileUnsentData(data);
            
            return false;
        }

        private async Task SendErrorMessageToClient(string errorMessage)
        {
            if (_resolverClient != null)
            {
                _logger.LogError("Player {playerId} is sending an error message to client.", this.GetPrimaryKeyString());
                try
                {
                    await _resolverClient.SendErrorMessage(errorMessage);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Player {playerId} encountered a fatal error while sending an error message to client.", this.GetPrimaryKeyString());
                }
            }
            else
            {
                _logger.LogError("Player {playerId} failed to send an error message to client.", this.GetPrimaryKeyString());
            }

            FileUnsentData(errorMessage);
        }

        private async Task ReportAuthenticationErrorToClient()
        {
            await SendErrorMessageToClient("You or another user is trying to use this player actor, but the supplied authorization token was incorrect.");
        }

        private void FileUnsentData<TType>(TType data) where TType : class
        {
            switch (data)
            {
                case List<DamageReport> damageReports:
                {
                    foreach (var damageReport in damageReports)
                    {
                        _unsentDamageReports.Enqueue(damageReport);
                    }

                    break;
                }
                case GameState gameState:
                {
                    if (!(_unsentGameState?.TimeStamp > gameState.TimeStamp))
                    {
                        _unsentGameState = gameState;
                    }

                    break;
                }
                case List<TargetNumberUpdate> targetNumberUpdates:
                {
                    foreach (var targetNumberUpdate in targetNumberUpdates)
                    {
                        _unsentTargetNumberUpdates.Enqueue(targetNumberUpdate);
                    }

                    break;
                }
                case Ping _:
                    break;
                case string errorMessage:
                    _unsentErrorMessages.Enqueue(errorMessage);
                    break;
                default: throw new InvalidOperationException("Asked to file unknown type of data.");
            }
        }
    }
}