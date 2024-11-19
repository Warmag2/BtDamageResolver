using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Partial class for player actor containing client connection methods.
/// </summary>
public partial class PlayerActor
{
    /// <inheritdoc/>
    public async Task<bool> Connect(string password)
    {
        if (password == null)
        {
            _logger.LogError("Player {PlayerId} connection request is malformed.", this.GetPrimaryKeyString());
            return false;
        }

        // Generate new password hash and salt if this user has not been accessed before
        if (_playerActorState.State.PasswordHash == null)
        {
            (_playerActorState.State.PasswordHash, _playerActorState.State.PasswordSalt) = _hasher.Hash(password);
            await _playerActorState.WriteStateAsync();
            _logger.LogInformation("New password and salt created for Player {PlayerId}.", this.GetPrimaryKeyString());
        }

        if (_hasher.Verify(password, _playerActorState.State.PasswordSalt, _playerActorState.State.PasswordHash))
        {
            // Invalidate any previous authentication token when connecting
            _playerActorState.State.AuthenticationToken = Guid.NewGuid();
            await _playerActorState.WriteStateAsync();

            await PerformConnectionActions();

            return true;
        }

        _logger.LogWarning("Player {PlayerId} has received a failed connection request from a client. Incorrect password.", this.GetPrimaryKeyString());

        await SendErrorMessageToClient($"Player {this.GetPrimaryKeyString()} has received a failed connection request from a client.Incorrect password.");

        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> Connect(Guid authenticationToken)
    {
        if (await CheckAuthentication(authenticationToken))
        {
            await PerformConnectionActions();

            return true;
        }

        _logger.LogWarning("Player {PlayerId} has received a failed connection request from a client. Incorrect or expired authentication token.", this.GetPrimaryKeyString());

        await SendErrorMessageToClient($"Player {this.GetPrimaryKeyString()} has received a failed connection request from a client. Incorrect or expired authentication token. Please login with a password.");

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> Disconnect(Guid authenticationToken)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            _logger.LogWarning("Player {PlayerId} has received a failed disconnection request from a client. Incorrect password.", this.GetPrimaryKeyString());
            return false;
        }

        if (IsConnectedToGame() && !await LeaveGame(authenticationToken))
        {
            _logger.LogWarning("Player {PlayerId} has failed to disconnect while signing out.", this.GetPrimaryKeyString());
            await SendErrorMessageToClient($"Inconsistent state. Player {this.GetPrimaryKeyString()} Unable to sign out of the active game. {_playerActorState.State.GameId}");
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
                _logger.LogWarning("Player {PlayerId} trying to connect to a game {OldGameId} while being connected to game {NewGameId}. Disconnecting first.", this.GetPrimaryKeyString(), _playerActorState.State.GameId, gameId);
                if (!await LeaveGame(authenticationToken))
                {
                    _logger.LogError("Player {PlayerId} failed to disconnect from {OldGameId}. Cannot join another game.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
                    await SendErrorMessageToClient($"Inconsistent state. Player {this.GetPrimaryKeyString()} claims to be a member of game {_playerActorState.State.GameId} but cannot disconnect.");
                    return false;
                }
            }

            _logger.LogInformation("Player {PlayerId} trying to connect to a game {OldGameId} while already connected to it. Falling back to resending join request.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
        }

        return await JoinGameInternal(gameId, password);
    }

    /// <inheritdoc />
    public async Task<bool> LeaveGame()
    {
        return await LeaveGame(_playerActorState.State.AuthenticationToken);
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
            _logger.LogInformation("Player {PlayerId} tried to disconnect from game but is not in a game.", this.GetPrimaryKeyString());
            await MarkDisconnectedStateAndSendToClient();

            return true;
        }

        if (await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).LeaveGame(this.GetPrimaryKeyString()))
        {
            _logger.LogInformation("Player {PlayerId} successfully disconnected from the game {GameId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);
            await MarkDisconnectedStateAndSendToClient();

            return true;
        }

        _logger.LogInformation("Player {PlayerId} failed to disconnect from the {GameId}.", this.GetPrimaryKeyString(), _playerActorState.State.GameId);

        return false;
    }

    private async Task<bool> JoinGameInternal(string gameId, string password)
    {
        var gameActor = GrainFactory.GetGrain<IGameActor>(gameId);

        if (await gameActor.JoinGame(this.GetPrimaryKeyString(), password))
        {
            _logger.LogInformation("Player {PlayerId} successfully connected to the game {GameId}.", this.GetPrimaryKeyString(), gameId);
            _playerActorState.State.GameId = gameId;
            _playerActorState.State.GamePassword = password;
            await _playerActorState.WriteStateAsync();

            // When we connect to a game, the game is not guaranteed to have our state. Send it and mark all units as updated.
            await gameActor.SendPlayerState(this.GetPrimaryKeyString(), await GetPlayerState(), _playerActorState.State.UnitEntries.UnitIds);

            // Fetch game options on join
            await gameActor.RequestGameOptions(this.GetPrimaryKeyString());

            // Connection state has been updated, so send it
            await SendDataToClient(EventNames.ConnectionResponse, GetConnectionResponse(true));

            return true;
        }

        _logger.LogInformation("Player {PlayerId} failed to connect to the game {GameId}.", this.GetPrimaryKeyString(), gameId);

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

    private async Task PerformConnectionActions()
    {
        _logger.LogInformation("Player {PlayerId} received a successful connection request from a client.", this.GetPrimaryKeyString());

        // Send personal state objects
        await SendDataToClient(EventNames.ConnectionResponse, GetConnectionResponse(true));
        await SendDataToClient(EventNames.PlayerOptions, _playerActorState.State.Options);

        // Ask for game-related state objects
        await RequestGameOptions(_playerActorState.State.AuthenticationToken);
        await RequestGameState(_playerActorState.State.AuthenticationToken);
        await RequestDamageReports(_playerActorState.State.AuthenticationToken);
        await RequestTargetNumbers();

        // Log the login to permanent store
        await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.Login, 0);
    }
}