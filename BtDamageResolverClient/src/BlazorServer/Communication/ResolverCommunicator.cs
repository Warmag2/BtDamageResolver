using System;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;

/// <summary>
/// The resolver communicator.
/// </summary>
public class ResolverCommunicator : IDisposable
{
    private readonly ILogger<ResolverCommunicator> _logger;
    private readonly IOptions<JsonSerializerOptions> _jsonSerializerOptions;
    private readonly DataHelper _dataHelper;
    private readonly string _connectionString;
    private readonly ClientMessageDispatcher _dispatcher;

    private string _playerName;
    private Guid _authenticationToken;
    private ClientToServerCommunicator _clientToServerCommunicator;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolverCommunicator"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <param name="dataHelper">The data compression helper.</param>
    /// <param name="dispatcher">The in-process dispatcher delivering events to the circuit.</param>
    public ResolverCommunicator(ILogger<ResolverCommunicator> logger, string connectionString, IOptions<JsonSerializerOptions> jsonSerializerOptions, DataHelper dataHelper, ClientMessageDispatcher dispatcher)
    {
        _logger = logger;
        _jsonSerializerOptions = jsonSerializerOptions;
        _connectionString = connectionString;
        _dataHelper = dataHelper;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Sets the authentication token.
    /// </summary>
    /// <param name="authenticationToken">The authentication token.</param>
    public void SetAuthenticationToken(Guid authenticationToken)
    {
        _authenticationToken = authenticationToken;
    }

    /// <summary>
    /// Connects to the server.
    /// </summary>
    /// <param name="credentials">The user credentials.</param>
    public void Connect(Credentials credentials)
    {
        _playerName = credentials.Name;
        Reset();

        try
        {
            _clientToServerCommunicator.Send(RequestNames.Connect, new ConnectRequest { PlayerName = credentials.Name, Credentials = credentials });
        }
        catch (Exception ex)
        {
            SendErrorMessage($"Error while trying to send data to server. Reason: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnect from the game.
    /// </summary>
    public void Disconnect()
    {
        SendRequest(
            RequestNames.Disconnect,
            new DisconnectRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName
            });
        TeardownCommunicator();
    }

    /// <summary>
    /// Force all players ready for the current game.
    /// </summary>
    public void ForceReady()
    {
        SendRequest(
            RequestNames.ForceReady,
            new ForceReadyRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName
            });
    }

    /// <summary>
    /// Get all damage reports for the current game.
    /// </summary>
    public void GetDamageReports()
    {
        SendRequest(
            RequestNames.GetDamageReports,
            new GetDamageReportsRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName
            });
    }

    /// <summary>
    /// Get game options for the current game.
    /// </summary>
    public void GetGameOptions()
    {
        SendRequest(
            RequestNames.GetGameOptions,
            new GetGameOptionsRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName
            });
    }

    /// <summary>
    /// Get game state for the current game.
    /// </summary>
    public void GetGameState()
    {
        SendRequest(
            RequestNames.GetGameState,
            new GetGameStateRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName
            });
    }

    /// <summary>
    /// Get player options for the current game.
    /// </summary>
    public void GetPlayerOptions()
    {
        SendRequest(
            RequestNames.GetPlayerOptions,
            new GetPlayerOptionsRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName
            });
    }

    /// <summary>
    /// Join a game.
    /// </summary>
    /// <param name="credentials">Credentials for joining a game.</param>
    public void JoinGame(Credentials credentials)
    {
        SendRequest(
            RequestNames.JoinGame,
            new JoinGameRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName,
                Credentials = credentials
            });
    }

    /// <summary>
    /// Kick a player from the game.
    /// </summary>
    /// <param name="playerId">The ID of the player to kick.</param>
    public void KickPlayer(string playerId)
    {
        SendRequest(
            RequestNames.KickPlayer,
            new KickPlayerRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName,
                PlayerToKickName = playerId
            });
    }

    /// <summary>
    /// Leave the current game.
    /// </summary>
    public void LeaveGame()
    {
        SendRequest(
            RequestNames.LeaveGame,
            new LeaveGameRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName
            });
    }

    /// <summary>
    /// Move an unit from a player to another.
    /// </summary>
    /// <param name="unitId">The unit to move.</param>
    /// <param name="playerId">The player to move the unit to.</param>
    public void MoveUnit(Guid unitId, string playerId)
    {
        SendRequest(
            RequestNames.MoveUnit,
            new MoveUnitRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName,
                ReceivingPlayer = playerId,
                UnitId = unitId
            });
    }

    /// <summary>
    /// Send a damage instance.
    /// </summary>
    /// <param name="damageInstance">The damage instance to send.</param>
    public void SendDamageInstance(DamageInstance damageInstance)
    {
        if (damageInstance == null)
        {
            return;
        }

        SendRequest(
            RequestNames.SendDamageInstanceRequest,
            new SendDamageInstanceRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName,
                DamageInstance = damageInstance
            });
    }

    /// <summary>
    /// Send game options.
    /// </summary>
    /// <param name="gameOptions">The game options.</param>
    public void SendGameOptions(GameOptions gameOptions)
    {
        if (gameOptions == null)
        {
            return;
        }

        SendRequest(
            RequestNames.SendGameOptions,
            new SendGameOptionsRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName,
                GameOptions = gameOptions
            });
    }

    /// <summary>
    /// Send player options.
    /// </summary>
    /// <param name="playerOptions">The player options.</param>
    public void SendPlayerOptions(PlayerOptions playerOptions)
    {
        if (playerOptions == null)
        {
            return;
        }

        SendRequest(
            RequestNames.SendPlayerOptions,
            new SendPlayerOptionsRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName,
                PlayerOptions = playerOptions
            });
    }

    /// <summary>
    /// Send a player state.
    /// </summary>
    /// <param name="playerState">The player state.</param>
    public void SendPlayerState(PlayerState playerState)
    {
        if (playerState == null)
        {
            return;
        }

        SendRequest(
            RequestNames.SendPlayerState,
            new SendPlayerStateRequest
            {
                AuthenticationToken = _authenticationToken,
                PlayerName = _playerName,
                PlayerState = playerState
            });
    }

    /// <summary>
    /// Standard dispose pattern.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose of managed resources.
    /// </summary>
    /// <param name="dispose">Should managed resources be disposed.</param>
    protected virtual void Dispose(bool dispose)
    {
        if (!disposed)
        {
            if (dispose)
            {
                TeardownCommunicator();
            }

            disposed = true;
        }
    }

    private void SendRequest<TRequest>(string requestType, TRequest request)
        where TRequest : class
    {
        if (!CheckAuthentication(requestType))
        {
            return;
        }

        try
        {
            _clientToServerCommunicator.Send(requestType, request);
        }
        catch (Exception ex)
        {
            SendErrorMessage($"Error while trying to send data to server. Reason: {ex.Message}");
        }
    }

    private bool CheckAuthentication(string requestType)
    {
        if (_authenticationToken == Guid.Empty)
        {
            SendErrorMessage($"Tried to send a request of type {requestType}, but no server authentication is available");
            return false;
        }

        return true;
    }

    private void Reset()
    {
        TeardownCommunicator();
        _clientToServerCommunicator = new ClientToServerCommunicator(_logger, _jsonSerializerOptions, _connectionString, _dataHelper, _dispatcher, _playerName);
    }

    /// <summary>
    /// Disposes the current client-to-server communicator (if any), releasing its Redis subscription and connection.
    /// </summary>
    private void TeardownCommunicator()
    {
        if (_clientToServerCommunicator != null)
        {
            _clientToServerCommunicator.Dispose();
            _clientToServerCommunicator = null;
        }
    }

    private void SendErrorMessage(string errorMessage)
    {
        // Route local errors through the same in-process dispatcher the server uses, so they reach the
        // existing ErrorMessage handler in Index.razor and are surfaced to the user.
        _ = _dispatcher.DispatchAsync(EventNames.ErrorMessage, _dataHelper.Pack(new ClientErrorEvent(errorMessage)))
            .ContinueWith(t => _logger.LogError(t.Exception, "Failed to dispatch error message"), TaskContinuationOptions.OnlyOnFaulted);
    }
}