using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Events;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Entities;
using Microsoft.AspNetCore.SignalR.Client;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication
{
    public interface IResolverCommunicator
    {
        void SetHubConnection(HubConnection hubConnection);

        Task<LoginState> Connect(Credentials credentials);

        Task<bool> Disconnect();

        Task<bool> JoinGame(Credentials credentials);

        Task<bool> LeaveGame();

        Task<GameOptions> GetGameOptions();

        Task<PlayerOptions> GetPlayerOptions();

        /// <summary>
        /// Update the player state.
        /// </summary>
        /// <param name="playerState">The player state object to send.</param>
        Task UpdatePlayerState(PlayerState playerState);

       
        Task SendDamageRequest(DamageRequest damageRequest);

        Task RequestDamageReports();

        Task RequestGameState();

        Task<bool> SetGameOptions(GameOptions gameOptions);

        Task<bool> SetPlayerOptions(PlayerOptions playerOptions);

        Task ForceReady();

        Task KickPlayer(string playerId);

        Task MoveUnit(Guid unitId, string playerId);
    }
}