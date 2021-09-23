using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication
{
    public class ResolverCommunicator
    {
        private readonly ILogger<ResolverCommunicator> _logger;
        private readonly CommunicationOptions _communicationOptions;
        private HubConnection _hubConnection;
        private IClientToServerCommunicator _clientToServerCommunicator;

        private string _playerName;
        private Guid _authenticationToken;

        public ResolverCommunicator(ILogger<ResolverCommunicator> logger, IOptions<CommunicationOptions> communicationOptions)
        {
            _logger = logger;
            _communicationOptions = communicationOptions.Value;
        }

        private void Reset()
        {
            _clientToServerCommunicator = new ClientToServerCommunicator(_logger, _communicationOptions.ConnectionString, _playerName, _hubConnection);
        }

        public void SetAuthenticationToken(Guid authenticationToken)
        {
            _authenticationToken = authenticationToken;
        }

        public void SetHubConnection(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

        public async Task Connect(Credentials credentials)
        {
            _playerName = credentials.Name;
            Reset();

            try
            {
                await _clientToServerCommunicator.Send(RequestNames.Connect, new ConnectRequest { PlayerName = credentials.Name, Credentials = credentials });
            }
            catch (Exception ex)
            {
                await SendErrorMessage(ex.Message);
            }
        }

        public async Task Disconnect()
        {
            await SendRequest(RequestNames.Disconnect,
                new DisconnectRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public async Task ForceReady()
        {
            await SendRequest(RequestNames.ForceReady,
                new ForceReadyRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public async Task GetDamageReports()
        {
            await SendRequest(RequestNames.GetDamageReports,
                new GetDamageReportsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public async Task GetGameOptions()
        {
            await SendRequest(RequestNames.GetGameOptions,
                new GetGameOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public async Task GetGameState()
        {
            await SendRequest(RequestNames.GetGameState,
                new GetGameStateRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public async Task GetPlayerOptions()
        {
            await SendRequest(RequestNames.GetPlayerOptions,
                new GetPlayerOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public async Task JoinGame(Credentials credentials)
        {
            await SendRequest(RequestNames.JoinGame,
                new JoinGameRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    Credentials = credentials
                });
        }

        public async Task KickPlayer(string playerId)
        {
            await SendRequest(RequestNames.KickPlayer,
                new KickPlayerRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    PlayerToKickName = playerId
                });
        }

        public async Task LeaveGame()
        {
            await SendRequest(RequestNames.LeaveGame,
                new LeaveGameRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public async Task MoveUnit(Guid unitId, string playerId)
        {
            await SendRequest(RequestNames.MoveUnit,
                new MoveUnitRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    ReceivingPlayer = playerId,
                    UnitId = unitId
                });
        }

        public async Task SendDamageInstance(DamageInstance damageInstance)
        {
            await SendRequest(RequestNames.SendDamageInstanceRequest,
                new SendDamageInstanceRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    DamageInstance = damageInstance
                });
        }

        public async Task SendGameOptions(GameOptions gameOptions)
        {
            await SendRequest(RequestNames.SendGameOptions,
                new SendGameOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    GameOptions = gameOptions
                });
        }

        public async Task SendPlayerOptions(PlayerOptions playerOptions)
        {
            await SendRequest(RequestNames.SendPlayerOptions,
                new SendPlayerOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    PlayerOptions = playerOptions
                });
        }

        public async Task SendPlayerState(PlayerState playerState)
        {
            await SendRequest(RequestNames.SendPlayerState,
                new SendPlayerStateRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    PlayerState = playerState
                });
        }

        private async Task SendRequest(string requestType, RequestBase requestBase)
        {
            if (!await CheckAuthentication(requestType))
            {
                return;
            }

            try
            {
                await _clientToServerCommunicator.Send(requestType, requestBase);
            }
            catch (Exception ex)
            {
                await SendErrorMessage(ex.Message);
            }
        }

        private async Task<bool> CheckAuthentication(string requestType)
        {
            if (_authenticationToken == Guid.Empty)
            {
                await SendErrorMessage($"Tried to send a request of type {requestType}, but no server authentication is available");
                return false;
            }
            
            return true;
        }

        private async Task SendErrorMessage(string errorMessage)
        {
            await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, errorMessage);
        }
    }
}