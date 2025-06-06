﻿using System;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;

/// <summary>
/// A Redis implementation of client-to-server communicator.
/// </summary>
public class ClientToServerCommunicator : RedisClientToServerCommunicator
{
    private readonly HubConnection _hubConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientToServerCommunicator"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer settings.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="dataHelper">The data compression helper.</param>
    /// <param name="hubConnection">The SignalR hub connection.</param>
    /// <param name="playerId">The player ID.</param>
    public ClientToServerCommunicator(ILogger logger, IOptions<JsonSerializerOptions> jsonSerializerOptions, string connectionString, DataHelper dataHelper, HubConnection hubConnection, string playerId) : base(logger, jsonSerializerOptions, connectionString, dataHelper, playerId)
    {
        _hubConnection = hubConnection;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleConnectionResponse(byte[] connectionResponse, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.ConnectionResponse, _hubConnection.ConnectionId, connectionResponse);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleDamageReports(byte[] damageReports, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.DamageReports, _hubConnection.ConnectionId, damageReports);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleErrorMessage(byte[] errorMessage, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.ErrorMessage, _hubConnection.ConnectionId, errorMessage);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGameEntries(byte[] gameEntries, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.GameEntries, _hubConnection.ConnectionId, gameEntries);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGameOptions(byte[] gameOptions, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.GameOptions, _hubConnection.ConnectionId, gameOptions);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGameState(byte[] gameState, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.GameState, _hubConnection.ConnectionId, gameState);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandlePlayerOptions(byte[] playerOptions, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.PlayerOptions, _hubConnection.ConnectionId, playerOptions);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleTargetNumberUpdates(byte[] targetNumbers, Guid correlationId)
    {
        await _hubConnection.SendAsync(EventNames.TargetNumbers, _hubConnection.ConnectionId, targetNumbers);

        return true;
    }
}