using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Events;
using Faemiyah.BtDamageResolver.Api.Interfaces;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Entities;
using Microsoft.AspNetCore.SignalR.Client;
using Orleans;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication
{
    public class ResolverCommunicator : IResolverCommunicator, IResolverClient
    {
        private readonly IClusterClient _clusterClient;
        private HubConnection _hubConnection;
        private IClientInterface _playerActor;

        private string _playerName;
        private Guid _authenticationToken;

        public ResolverCommunicator(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public void SetHubConnection(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

        public async Task<LoginState> Connect(Credentials credentials)
        {
            try
            {
                return await ConnectPlayer(credentials.Name, credentials.Password);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return null;
        }

        public async Task<bool> Disconnect()
        {
            try
            {
                await _playerActor.Disconnect(_authenticationToken);
                _playerName = null;
                _playerActor = null;
                _authenticationToken = Guid.Empty;

                return true;
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return false;
        }

        public async Task<bool> JoinGame(Credentials credentials)
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                return await _playerActor.JoinGame(_authenticationToken, credentials.Name, credentials.Password);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return false;
        }

        public async Task<bool> LeaveGame()
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                return await _playerActor.LeaveGame(_authenticationToken);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return false;
        }

        public async Task UpdatePlayerState(PlayerState state)
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                await _playerActor.UpdateState(_authenticationToken, state);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }
        }

        public async Task<GameOptions> GetGameOptions()
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                return await _playerActor.GetGameOptions(_authenticationToken);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return new GameOptions(); // So that the UI does not crash.
        }

        public async Task<bool> SetGameOptions(GameOptions gameOptions)
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                return await _playerActor.SetGameOptions(_authenticationToken, gameOptions);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return false;
        }

        public async Task<PlayerOptions> GetPlayerOptions()
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                return await _playerActor.GetPlayerOptions(_authenticationToken);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return new PlayerOptions(); // So that the UI does not crash.
        }

        public async Task<bool> SetPlayerOptions(PlayerOptions playerOptions)
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                return await _playerActor.SetPlayerOptions(_authenticationToken, playerOptions);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }

            return false;
        }

        public async Task SendDamageRequest(DamageRequest damageRequest)
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                await _playerActor.ProcessDamageRequest(_authenticationToken, damageRequest);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }
        }

        public async Task RequestDamageReports()
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                await _playerActor.RequestDamageReports(_authenticationToken);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }
        }

        public async Task RequestGameState()
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                await _playerActor.RequestGameState(_authenticationToken);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }
        }

        public async Task ForceReady()
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                await _playerActor.ForceReady(_authenticationToken);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }
        }

        public async Task KickPlayer(string playerId)
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                await _playerActor.KickPlayer(_authenticationToken, playerId);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }
        }

        public async Task MoveUnit(Guid unitId, string playerId)
        {
            await CheckConnectionStateAndRefresh();

            try
            {
                await _playerActor.MoveUnit(_authenticationToken, unitId, playerId);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
            }
        }

        private async Task CheckConnectionStateAndRefresh()
        {
            bool resubscriptionNeeded;

            try
            {
                resubscriptionNeeded = !await _playerActor.CheckConnection(_authenticationToken);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
                resubscriptionNeeded = true;
            }

            if (resubscriptionNeeded && !await ConnectSubscriber())
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, "Problem reconnecting subscriber. System is in an unrecoverable state.");
            }
        }

        private async Task<LoginState> ConnectPlayer(string playerName, string password)
        {
            _playerName = playerName;
            _playerActor = _clusterClient.GetGrain<IClientInterface>(_playerName);
            var loginState = await _playerActor.Connect(password);
            
            if (loginState != null)
            {
                _authenticationToken = loginState.AuthenticationToken;

                if (await ConnectSubscriber())
                {
                    return loginState;
                }
            }

            return null;
        }

        private async Task<bool> ConnectSubscriber()
        {
            try
            {
                var clientObject = _clusterClient.CreateObjectReference<IResolverClient>(this).Result;
                return await _playerActor.ConnectSubscriber(_authenticationToken, clientObject);
            }
            catch (Exception ex)
            {
                await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, ex.Message);
                return false;
            }
        }

        public async Task SendCompressedData(byte[] data, Type dataType)
        {
            if(dataType == typeof(List<DamageReport>))
            {
                await _hubConnection.SendAsync("ReceiveDamageReport", _hubConnection.ConnectionId, data);
            }
            else if (dataType == typeof(GameOptions))
            {
                await _hubConnection.SendAsync("ReceiveGameOptions", _hubConnection.ConnectionId, data);
            }
            else if (dataType == typeof(GameState))
            {
                await _hubConnection.SendAsync("ReceiveGameState", _hubConnection.ConnectionId, data);
            }
            else if (dataType == typeof(List<TargetNumberUpdate>))
            {
                await _hubConnection.SendAsync("ReceiveTargetNumberUpdates", _hubConnection.ConnectionId, data);
            }
            else if (dataType == typeof(Ping))
            {
                // Do nothing. If we get here, we received the data.
            }
            else
            {
                await SendErrorMessage($"Server is sending unknown data to this client. Data type: {dataType.Name}");
            }
        }

        public async Task SendErrorMessage(string errorMessage)
        {
            await _hubConnection.SendAsync("ReceiveErrorMessage", _hubConnection.ConnectionId, errorMessage);
        }
    }
}