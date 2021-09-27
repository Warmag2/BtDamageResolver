using System;
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
        private readonly ClientToServerCommunicatorContainer _clientToServerCommunicatorContainer;
        private readonly CommunicationOptions _communicationOptions;
        private HubConnection _hubConnection;
        private IClientToServerCommunicator _clientToServerCommunicator;

        private string _playerName;
        private Guid _authenticationToken;

        public ResolverCommunicator(ILogger<ResolverCommunicator> logger, IOptions<CommunicationOptions> communicationOptions, ClientToServerCommunicatorContainer clientToServerCommunicatorContainer)
        {
            _logger = logger;
            _clientToServerCommunicatorContainer = clientToServerCommunicatorContainer;
            _communicationOptions = communicationOptions.Value;
        }

        private void Reset()
        {
            _clientToServerCommunicatorContainer.Subscribe(_playerName, _hubConnection);
        }

        public void SetAuthenticationToken(Guid authenticationToken)
        {
            _authenticationToken = authenticationToken;
        }

        public void SetHubConnection(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

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
                SendErrorMessage(ex.Message);
            }
        }

        public void Disconnect()
        {
            SendRequest(RequestNames.Disconnect,
                new DisconnectRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public void ForceReady()
        {
            SendRequest(RequestNames.ForceReady,
                new ForceReadyRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public void GetDamageReports()
        {
            SendRequest(RequestNames.GetDamageReports,
                new GetDamageReportsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public void GetGameOptions()
        {
            SendRequest(RequestNames.GetGameOptions,
                new GetGameOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public void GetGameState()
        {
            SendRequest(RequestNames.GetGameState,
                new GetGameStateRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public void GetPlayerOptions()
        {
            SendRequest(RequestNames.GetPlayerOptions,
                new GetPlayerOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public void JoinGame(Credentials credentials)
        {
            SendRequest(RequestNames.JoinGame,
                new JoinGameRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    Credentials = credentials
                });
        }

        public void KickPlayer(string playerId)
        {
            SendRequest(RequestNames.KickPlayer,
                new KickPlayerRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    PlayerToKickName = playerId
                });
        }

        public void LeaveGame()
        {
            SendRequest(RequestNames.LeaveGame,
                new LeaveGameRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName
                });
        }

        public void MoveUnit(Guid unitId, string playerId)
        {
            SendRequest(RequestNames.MoveUnit,
                new MoveUnitRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    ReceivingPlayer = playerId,
                    UnitId = unitId
                });
        }

        public void SendDamageInstance(DamageInstance damageInstance)
        {
            SendRequest(RequestNames.SendDamageInstanceRequest,
                new SendDamageInstanceRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    DamageInstance = damageInstance
                });
        }

        public void SendGameOptions(GameOptions gameOptions)
        {
            SendRequest(RequestNames.SendGameOptions,
                new SendGameOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    GameOptions = gameOptions
                });
        }

        public void SendPlayerOptions(PlayerOptions playerOptions)
        {
            SendRequest(RequestNames.SendPlayerOptions,
                new SendPlayerOptionsRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    PlayerOptions = playerOptions
                });
        }

        public void SendPlayerState(PlayerState playerState)
        {
            SendRequest(RequestNames.SendPlayerState,
                new SendPlayerStateRequest
                {
                    AuthenticationToken = _authenticationToken,
                    PlayerName = _playerName,
                    PlayerState = playerState
                });
        }

        private void SendRequest(string requestType, RequestBase requestBase)
        {
            if (!CheckAuthentication(requestType))
            {
                return;
            }

            try
            {
                _clientToServerCommunicator.Send(requestType, requestBase);
            }
            catch (Exception ex)
            {
                SendErrorMessage(ex.Message);
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

        private void SendErrorMessage(string errorMessage)
        {
            _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, errorMessage).Ignore();
        }
    }
}