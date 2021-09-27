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
    }
}
