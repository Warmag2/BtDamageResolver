using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Common.Options;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Services
{
    /// <summary>
    /// Provides client-server communication to the silo and server-to-client communication for grains.
    /// </summary>
    [Reentrant]
    public class CommunicationService : GrainService, ICommunicationService
    {
        private readonly ILogger<CommunicationService> _logger;
        private readonly IServerToClientCommunicator _serverToClientCommunicator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationService"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="communicationOptions">The communication options.</param>
        /// <param name="grainFactory">The grain factory.</param>
        /// <param name="grainId">The grain ID.</param>
        /// <param name="silo">The silo.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public CommunicationService(
            ILogger<CommunicationService> logger,
            IOptions<CommunicationOptions> communicationOptions,
            IGrainFactory grainFactory,
            IGrainIdentity grainId,
            Silo silo,
            ILoggerFactory loggerFactory) : base(grainId, silo, loggerFactory)
        {
            _logger = logger;
            _serverToClientCommunicator = new ServerToClientCommunicator(loggerFactory.CreateLogger<ServerToClientCommunicator>(), communicationOptions.Value.ConnectionString, grainFactory);
        }

        /// <inheritdoc />
        public override Task Start()
        {
            _logger.LogInformation("{service} connected to redis successfully.", this.GetType());

            return base.Start();
        }

        /// <inheritdoc />
        public Task Send(string playerId, string envelopeType, object data)
        {
            _logger.LogDebug("Sending data of type {type} to player {player}", envelopeType, playerId);
            _serverToClientCommunicator.Send(playerId, envelopeType, data);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SendToMany(List<string> playerIds, string envelopeType, object data)
        {
            _logger.LogDebug("Sending data of type {type} to players: {playerList}", envelopeType, string.Join(", ", playerIds));
            _serverToClientCommunicator.SendToMany(playerIds, envelopeType, data);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SendToAllClients(string envelopeType, object data)
        {
            _logger.LogDebug("Sending data of type {type} to all players", envelopeType);
            _serverToClientCommunicator.SendToAll(envelopeType, data);

            return Task.CompletedTask;
        }
    }
}
