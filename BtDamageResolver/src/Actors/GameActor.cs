using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Actors.States;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Main implementation of the game actor.
/// </summary>
public partial class GameActor : Grain, IGameActor
{
    private readonly ILogger<GameActor> _logger;
    private readonly ICommunicationServiceClient _communicationServiceClient;
    private readonly ILoggingServiceClient _loggingServiceClient;
    private readonly IPersistentState<GameActorState> _gameActorState;
    private readonly IPersistentState<GameActorDamageReportState> _gameActorDamageReportState;
    private readonly ILogicUnitFactory _logicUnitFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameActor"/> class.
    /// </summary>
    /// <param name="gameActorState">The state object for this actor.</param>
    /// <param name="gameActorDamageReportState">The state object for this actor containing damage reports.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="communicationServiceClient">The communication service client.</param>
    /// <param name="logicUnitFactory">The unit logic factory.</param>
    /// <param name="loggingServiceClient">The logging service client.</param>
    public GameActor(
        [PersistentState(nameof(GameActorState), Settings.ActorStateStoreName)] IPersistentState<GameActorState> gameActorState,
        [PersistentState(nameof(GameActorDamageReportState), Settings.ActorStateStoreName)] IPersistentState<GameActorDamageReportState> gameActorDamageReportState,
        ILogger<GameActor> logger,
        ICommunicationServiceClient communicationServiceClient,
        ILogicUnitFactory logicUnitFactory,
        ILoggingServiceClient loggingServiceClient)
    {
        _gameActorState = gameActorState;
        _gameActorDamageReportState = gameActorDamageReportState;
        _logger = logger;
        _communicationServiceClient = communicationServiceClient;
        _logicUnitFactory = logicUnitFactory;
        _loggingServiceClient = loggingServiceClient;
    }

    /// <inheritdoc />
    public Task<bool> IsUnitInGame(Guid unitId)
    {
        return Task.FromResult(_gameActorState.State.PlayerStates.Any(p => p.Value.UnitEntries.Exists(u => u.Id == unitId)));
    }

    /// <inheritdoc />
    public async Task<bool> SendPlayerState(string sendingPlayerId, PlayerState playerState, List<Guid> unitIds)
    {
        // Do not accept player states from players who are not in the game.
        if (!_gameActorState.State.PlayerIds.Contains(sendingPlayerId))
        {
            return false;
        }

        var updated = false;

        if (_gameActorState.State.PlayerStates.TryGetValue(playerState.PlayerId, out var value))
        {
            if (playerState.TimeStamp > value.TimeStamp)
            {
                _logger.LogInformation("Updating player {PlayerId} state with new data from {Timestamp}", playerState.PlayerId, playerState.TimeStamp);
                _gameActorState.State.PlayerStates[playerState.PlayerId] = playerState;
                updated = true;
            }
            else
            {
                _logger.LogInformation(
                    "Discarding update event. Timestamp {StampEvent}, is older than existing timestamp {StampState}.",
                    playerState.TimeStamp,
                    _gameActorState.State.PlayerStates[playerState.PlayerId].TimeStamp);
            }
        }
        else
        {
            _logger.LogInformation("Receiving data from player {Player} with no previous data.", playerState.PlayerId);
            _gameActorState.State.PlayerStates.Add(playerState.PlayerId, playerState);
            updated = true;
        }

        if (updated)
        {
            await CheckGameStateUpdateEvents(unitIds);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> SendDamageInstance(string sendingPlayerId, DamageInstance damageInstance)
    {
        // Do not accept damage instances from players who are not in the game.
        if (!_gameActorState.State.PlayerIds.Contains(sendingPlayerId))
        {
            return false;
        }

        var damageReport = await ProcessDamageInstance(damageInstance);
        damageReport.Turn = _gameActorState.State.Turn;

        await DistributeDamageReportsToPlayers(new List<DamageReport> { damageReport });

        return true;
    }

    /// <inheritdoc />>
    public async Task<bool> JoinGame(string playerId, string password)
    {
        if (string.IsNullOrWhiteSpace(playerId) || password == null)
        {
            _logger.LogInformation("In Game {GameId}, player {PlayerId} game connection request is malformed.", this.GetPrimaryKeyString(), playerId);
            return false;
        }

        // Accept any password if the game does not yet exist
        if (string.IsNullOrWhiteSpace(_gameActorState.State.Password) || string.Equals(_gameActorState.State.Password, password))
        {
            _gameActorState.State.Password = password;
            _gameActorState.State.PlayerIds.Add(playerId);
            _gameActorState.State.TimeStamp = DateTime.UtcNow;
            await _gameActorState.WriteStateAsync();

            _logger.LogInformation("In Game {GameId}, Player {PlayerId} successfully connected to the game.", this.GetPrimaryKeyString(), playerId);

            await CheckGameStateUpdateEvents();

            // Log logins to permanent store
            await _loggingServiceClient.LogGameAction(DateTime.UtcNow, this.GetPrimaryKeyString(), GameActionType.Login, 0);

            return true;
        }

        _logger.LogInformation("In Game {GameId}, Player {PlayerId} failed to connect to the game.", playerId, this.GetPrimaryKeyString());

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> LeaveGame(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            _logger.LogInformation("In Game {GameId}, player {PlayerId} game disconnection request is malformed.", this.GetPrimaryKeyString(), playerId);
            return false;
        }

        if (_gameActorState.State.PlayerStates.Remove(playerId) || _gameActorState.State.PlayerIds.Remove(playerId))
        {
            _gameActorState.State.TimeStamp = DateTime.UtcNow;
            await CheckGameStateUpdateEvents();

            _logger.LogInformation("In Game {GameId}, Player {PlayerId} successfully disconnected.", this.GetPrimaryKeyString(), playerId);
        }
        else
        {
            _logger.LogInformation("In Game {GameId} Player {PlayerId}, cannot be disconnected, since the player is not in the game.", this.GetPrimaryKeyString(), playerId);
        }

        // Log logins to permanent store
        await _loggingServiceClient.LogGameAction(DateTime.UtcNow, this.GetPrimaryKeyString(), GameActionType.LogOut, 0);

        return true;
    }

    private void CheckForPlayerCountEvents()
    {
        // If we have no players, reset turn and erase damage reports
        if (_gameActorState.State.PlayerStates.Count == 0)
        {
            _gameActorState.State.Reset();
            _gameActorDamageReportState.State.Reset();
            _logger.LogInformation("Game {GameId} has lost all of its players. Resetting to turn 0 and clearing damage reports.", this.GetPrimaryKeyString());
        }

        // If there is exactly 1 player, make him/her the admin
        if (_gameActorState.State.PlayerStates.Count == 1)
        {
            var onlyPlayerId = _gameActorState.State.PlayerStates.Values.Single().PlayerId;
            if (_gameActorState.State.AdminId != onlyPlayerId)
            {
                _gameActorState.State.AdminId = onlyPlayerId;
                _logger.LogInformation("Game {GameId} has exactly 1 player. Setting admin id to that player.", this.GetPrimaryKeyString());
            }
        }
    }

    private GameState GetGameState(bool markStateAsNew)
    {
        var timeStampNow = DateTime.UtcNow;

        var gameState = new GameState
        {
            AdminId = _gameActorState.State.AdminId,
            GameId = this.GetPrimaryKeyString(),
            Players = _gameActorState.State.PlayerStates,
            TimeStamp = markStateAsNew ? timeStampNow : _gameActorState.State.TimeStamp,
            Turn = _gameActorState.State.Turn,
            TurnTimeStamp = _gameActorState.State.TurnTimeStamp
        };

        if (markStateAsNew)
        {
            foreach (var player in gameState.Players)
            {
                player.Value.TimeStamp = timeStampNow;
            }
        }

        return gameState;
    }
}