using System;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;

/// <summary>
/// A Redis implementation of client-to-server communicator.
/// </summary>
public class ClientToServerCommunicator : RedisClientToServerCommunicator
{
    private readonly ClientMessageDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientToServerCommunicator"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer settings.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="dataHelper">The data compression helper.</param>
    /// <param name="dispatcher">The in-process dispatcher delivering events to the circuit.</param>
    /// <param name="playerId">The player ID.</param>
    public ClientToServerCommunicator(ILogger logger, IOptions<JsonSerializerOptions> jsonSerializerOptions, string connectionString, DataHelper dataHelper, ClientMessageDispatcher dispatcher, string playerId) : base(logger, jsonSerializerOptions, connectionString, dataHelper, playerId)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleConnectionResponse(byte[] connectionResponse, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.ConnectionResponse, connectionResponse);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleDamageReports(byte[] damageReports, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.DamageReports, damageReports);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleErrorMessage(byte[] errorMessage, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.ErrorMessage, errorMessage);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGameEntries(byte[] gameEntries, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.GameEntries, gameEntries);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGameOptions(byte[] gameOptions, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.GameOptions, gameOptions);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleGameState(byte[] gameState, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.GameState, gameState);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandlePlayerOptions(byte[] playerOptions, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.PlayerOptions, playerOptions);

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> HandleTargetNumberUpdates(byte[] targetNumbers, Guid correlationId)
    {
        await _dispatcher.DispatchAsync(EventNames.TargetNumbers, targetNumbers);

        return true;
    }
}