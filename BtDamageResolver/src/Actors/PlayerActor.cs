using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.States;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class PlayerActor : Grain, IPlayerActor
    {
        private readonly ILogger<PlayerActor> _logger;
        private readonly ICommunicationServiceClient _communicationServiceClient;
        private readonly ILoggingServiceClient _loggingServiceClient;
        private readonly IPersistentState<PlayerActorState> _playerActorState;

        /// <summary>
        /// Constructor for a Player actor.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="communicationServiceClient">The communication service client.</param>
        /// <param name="loggingServiceClient">The logging service client.</param>
        /// <param name="playerActorState">The state object for this actor.</param>
        public PlayerActor(
            ILogger<PlayerActor> logger,
            ICommunicationServiceClient communicationServiceClient,
            ILoggingServiceClient loggingServiceClient,
            [PersistentState(nameof(PlayerActorState), Settings.ActorStateStoreName)]IPersistentState<PlayerActorState> playerActorState)
        {
            _logger = logger;
            _communicationServiceClient = communicationServiceClient;
            _loggingServiceClient = loggingServiceClient;
            _playerActorState = playerActorState;
        }

        /// <inheritdoc />
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
        }

        /// <inheritdoc />
        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
        }

        /// <inheritdoc />
        public async Task<PlayerState> GetPlayerState(Guid authenticationToken, bool markStateAsNew)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return null;
            }

            return await GetPlayerState(markStateAsNew);
        }

        private async Task<PlayerState> GetPlayerState(bool markStateAsNew)
        {
            if (markStateAsNew)
            {
                _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;
            }

            var units = new List<UnitEntry>();
            foreach (var unitId in _playerActorState.State.UnitEntryIds)
            {
                units.Add(await GrainFactory.GetGrain<IUnitActor>(unitId).GetUnitState());
            }

            var playerState = new PlayerState
            {
                IsReady = _playerActorState.State.IsReady,
                PlayerId = this.GetPrimaryKeyString(),
                TimeStamp = _playerActorState.State.UpdateTimeStamp,
                UnitEntries = units
            };

            return playerState;
        }

        /// <inheritdoc />
        public async Task<bool> SendPlayerState(Guid authenticationToken, PlayerState playerState)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            try
            {
                if (playerState.TimeStamp > _playerActorState.State.UpdateTimeStamp)
                {
                    _logger.LogInformation("Updating player {playerId} state with new data from {timestamp}", this.GetPrimaryKeyString(), playerState.TimeStamp);
                    _playerActorState.State.IsReady = playerState.IsReady;
                    _playerActorState.State.UpdateTimeStamp = playerState.TimeStamp;
                    _playerActorState.State.UnitEntryIds = playerState.UnitEntries.Select(u => u.Id).ToHashSet();

                    List<Guid> updatedUnits = new List<Guid>();

                    foreach (var unit in playerState.UnitEntries)
                    {
                        var unitActor = GrainFactory.GetGrain<IUnitActor>(unit.Id);
                        if (await unitActor.UpdateState(unit))
                        {
                            updatedUnits.Add(unit.Id);
                        }
                    }

                    // Save this state first and wait for save to finish to avoid any race conditions
                    // arising from changes incurred by uploading the state to the game actor.
                    await _playerActorState.WriteStateAsync();

                    if (ConnectedToGame())
                    {
                        // If we are connected to the game, also push player state to the game actor to be distributed to other players.
                        // The result is ignored, because we don't have to wait here to see the results.
                        await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).UpdatePlayerState(_playerActorState.State.AuthenticationToken, playerState, updatedUnits);
                    }

                    // Log the number of updated units to permanent store
                    await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.UpdateUnit, updatedUnits.Count);
                }
                else
                {
                    _logger.LogInformation(
                        "Discarding update event for player {id}. Timestamp {stampEvent}, is older than existing timestamp {stampState}.",
                        this.GetPrimaryKeyString(), playerState.TimeStamp, _playerActorState.State.UpdateTimeStamp);
                }
            }
            catch (Exception ex)
            {
                await SendErrorMessageToClient($"{ex.Message}\n{ex.StackTrace}");
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> GetDamageReports(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (ConnectedToGame())
            {
                await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).RequestDamageReports(authenticationToken);
                return true;
            }

            return false;
        }

        public async Task<bool> GetGameState(Guid authenticationToken)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (ConnectedToGame())
            {
                await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).RequestGameState(authenticationToken);
            }
            else
            {
                await SendOnlyThisPlayerGameStateToClient();
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> JoinGame(Guid authenticationToken, string gameId, string password)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (ConnectedToGame())
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
                await gameActor.UpdatePlayerState(_playerActorState.State.AuthenticationToken, await GetPlayerState(false), _playerActorState.State.UnitEntryIds.ToList());
                // Fetch game options on join
                await GetGameOptions(_playerActorState.State.AuthenticationToken);

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

            if (!ConnectedToGame())
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

        /// <inheritdoc />
        public async Task<bool> SendDamageInstance(Guid authenticationToken, DamageInstance damageInstance)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return false;
            }

            if (ConnectedToGame())
            {
                var gameActor = GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId);

                if (await gameActor.IsUnitInGame(_playerActorState.State.AuthenticationToken, damageInstance.UnitId))
                {
                    return await gameActor.ProcessDamageInstance(_playerActorState.State.AuthenticationToken, damageInstance);
                }

                _logger.LogWarning("Player {playerId} asked for a damage request against unit {unitId}, but the said unit is not in the game.", this.GetPrimaryKeyString(), damageInstance.UnitId);
                
                return false;
            }

            _logger.LogWarning("Player {playerId} asked for a damage request, but is not connected to a game.", this.GetPrimaryKeyString());

            return false;
        }

        /// <inheritdoc />
        public async Task UnReady()
        {
            _playerActorState.State.IsReady = false;
            _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;
            await _playerActorState.WriteStateAsync();
        }

        private async Task MarkDisconnectedStateAndSendToClient()
        {
            _playerActorState.State.GameId = null;
            _playerActorState.State.GamePassword = null;
            _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;
            await SendOnlyThisPlayerGameStateToClient();
        }

        private bool ConnectedToGame()
        {
            if (string.IsNullOrEmpty(_playerActorState.State.GameId))
            {
                return false;
            }

            return true;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private async Task<bool> CheckAuthentication(Guid token)
        {
            if (_playerActorState.State.AuthenticationToken == token)
            {
                return true;
            }

            await ReportAuthenticationErrorToClient();
            return false;
        }
    }
}