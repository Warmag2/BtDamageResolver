using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using static System.Text.Json.JsonSerializer;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators
{
    /// <summary>
    /// Redis implementation of a communicator.
    /// </summary>
    public abstract class RedisCommunicator
    {
        /// <summary>
        /// The server stream address.
        /// </summary>
        protected const string ServerStreamAddress = "BtDamageResolverServer";

        protected readonly ILogger Logger;
        private readonly string _connectionString;
        private ConnectionMultiplexer _redisConnectionMultiplexer;

        protected ISubscriber RedisSubscriber;

        /// <summary>
        /// Redis implementation of a server-client communication interface.
        /// </summary>
        /// <param name="logger">The logging interface</param>
        /// <param name="connectionString">The server connection string.</param>
        protected RedisCommunicator(ILogger logger, string connectionString)
        {
            Logger = logger;
            _connectionString = connectionString;

            Init();
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        private void Init()
        {
            _redisConnectionMultiplexer = ConnectionMultiplexer.Connect(_connectionString);
            RedisSubscriber = _redisConnectionMultiplexer.GetSubscriber();
            Subscribe();
        }

        /// <summary>
        /// Subscribe to the necessary communication endpoints.
        /// </summary>
        protected abstract void Subscribe();

        /// <summary>
        /// Checks connection to channel and resubscribes if the connection has been lost.
        /// </summary>
        /// <param name="channel">The channel to connect.</param>
        private void CheckChannelConnection(string channel)
        {
            if (!RedisSubscriber.IsConnected(channel))
            {
                Logger.LogWarning("Not connected to server. Reconnecting.");
                RedisSubscriber.UnsubscribeAll();
                RedisSubscriber = _redisConnectionMultiplexer.GetSubscriber();
                Subscribe();
            }
        }
        
        /// <summary>
        /// Sends data to the selected communication endpoint.
        /// </summary>
        /// <param name="target">The target where to send data.</param>
        /// <param name="data">The data to send.</param>
        protected async Task SendEnvelope(string target, Envelope data)
        {
            CheckChannelConnection(target);
            await RedisSubscriber.PublishAsync(target, Serialize(data));
        }

        /// <summary>
        /// Sends an error message to the specified target.
        /// </summary>
        /// <param name="target">The target where to send data.</param>
        /// <param name="errorMessage">The error message.</param>
        protected async Task SendErrorMessage(string target, string errorMessage)
        {
            await SendEnvelope(target, new Envelope(EventNames.ErrorMessage, errorMessage));
        }
    }
}