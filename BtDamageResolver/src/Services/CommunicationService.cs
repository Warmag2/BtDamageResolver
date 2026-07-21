using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Services;

/// <summary>
/// Provides client-server communication to the silo and server-to-client communication for grains.
/// </summary>
public class CommunicationService : GrainService, ICommunicationService
{
    private readonly ILogger<CommunicationService> _logger;
    private readonly ServerToClientCommunicator _serverToClientCommunicator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationService"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="serverToClientCommunicator">The server-to-client communicator.</param>
    /// <param name="grainId">The grain ID.</param>
    /// <param name="silo">The silo.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public CommunicationService(
        ILogger<CommunicationService> logger,
        ServerToClientCommunicator serverToClientCommunicator,
        GrainId grainId,
        Silo silo,
        ILoggerFactory loggerFactory) : base(grainId, silo, loggerFactory)
    {
        _logger = logger;
        _serverToClientCommunicator = serverToClientCommunicator;
    }

    /// <inheritdoc />
    public override Task Start()
    {
        _logger.LogInformation("{Service} connected to Redis successfully.", GetType());

        return base.Start();
    }

    /// <inheritdoc />
    public Task Send(string playerId, string envelopeType, object data)
    {
        _logger.LogDebug("Sending data of type {Type} to player {Player}", envelopeType, playerId);
        _serverToClientCommunicator.Send(playerId, envelopeType, data);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendToMany(List<string> playerIds, string envelopeType, object data)
    {
        _logger.LogDebug("Sending data of type {Type} to players: {PlayerList}", envelopeType, string.Join(", ", playerIds));
        _serverToClientCommunicator.SendToMany(playerIds, envelopeType, data);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendToAllClients(string envelopeType, object data)
    {
        _logger.LogDebug("Sending data of type {Type} to all players", envelopeType);
        _serverToClientCommunicator.SendToAll(envelopeType, data);

        return Task.CompletedTask;
    }
}