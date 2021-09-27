using System.Collections.Concurrent;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication
{
    /// <summary>
    /// Shared class for subscribing to Redis and supplying clients with data.
    /// </summary>
    public class ClientToServerCommunicatorContainer
    {
        private readonly ILogger<ClientToServerCommunicatorContainer> _logger;
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, ClientToServerCommunicator> _communicators;

        /// <summary>
        /// Constructor for <see cref="ClientToServerCommunicatorContainer"/>
        /// </summary>
        public ClientToServerCommunicatorContainer(ILogger<ClientToServerCommunicatorContainer> logger, IOptions<CommunicationOptions> communicationOptions)
        {
            _logger = logger;
            _connectionString = communicationOptions.Value.ConnectionString;
            _communicators = new ConcurrentDictionary<string, ClientToServerCommunicator>();
        }

        public void Subscribe(string playerId, HubConnection hubConnection)
        {
            var newCommunicator = new ClientToServerCommunicator(_logger, _connectionString, playerId, hubConnection);

            _communicators.AddOrUpdate(playerId, newCommunicator, (_, _) => newCommunicator);
        }

        public void Unsubscribe(string playerId)
        {
            _communicators.Remove(playerId, out _);
        }
    }
}