using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Constants;
using Microsoft.AspNetCore.SignalR;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Hubs;

/// <summary>
/// SignalR hub that the game server uses to push data to connected browser clients.
/// Each method receives a target connection ID and a compressed payload, then
/// forwards the payload to the appropriate client via <see cref="Microsoft.AspNetCore.SignalR.IClientProxy"/>.
/// </summary>
public class ClientHub : Hub
{
    /// <summary>Forwards a connection response payload to the specified client.</summary>
    public async Task ConnectionResponse(string connectionId, byte[] connectionResponse)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.ConnectionResponse, connectionResponse);
    }

    /// <summary>Forwards a damage reports payload to the specified client.</summary>
    public async Task DamageReports(string connectionId, byte[] damageReports)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.DamageReports, damageReports);
    }

    /// <summary>Forwards an error message payload to the specified client.</summary>
    public async Task ErrorMessage(string connectionId, byte[] errorMessage)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.ErrorMessage, errorMessage);
    }

    /// <summary>Forwards a game entries payload to the specified client.</summary>
    public async Task GameEntries(string connectionId, byte[] gameEntries)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.GameEntries, gameEntries);
    }

    /// <summary>Forwards a game options payload to the specified client.</summary>
    public async Task GameOptions(string connectionId, byte[] gameOptions)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.GameOptions, gameOptions);
    }

    /// <summary>Forwards a game state payload to the specified client.</summary>
    public async Task GameState(string connectionId, byte[] gameState)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.GameState, gameState);
    }

    /// <summary>Forwards a player options payload to the specified client.</summary>
    public async Task PlayerOptions(string connectionId, byte[] playerOptions)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.PlayerOptions, playerOptions);
    }

    /// <summary>Forwards a target numbers payload to the specified client.</summary>
    public async Task TargetNumbers(string connectionId, byte[] targetNumberUpdates)
    {
        await Clients.Client(connectionId).SendAsync(EventNames.TargetNumbers, targetNumberUpdates);
    }
}