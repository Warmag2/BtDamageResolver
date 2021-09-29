using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

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
        private readonly string _listenTarget;
        private ChannelMessageQueue _listenedMessageQueue;
        private ConnectionMultiplexer _redisConnectionMultiplexer;
        private ISubscriber _redisSubscriber;

        /// <summary>
        /// Redis implementation of a server-client communication interface.
        /// </summary>
        /// <param name="logger">The logging interface</param>
        /// <param name="connectionString">The server connection string.</param>
        /// <param name="listenTarget">The target channel to listen to.</param>
        protected RedisCommunicator(ILogger logger, string connectionString, string listenTarget)
        {
            Logger = logger;
            _connectionString = connectionString;
            _listenTarget = listenTarget;

            Start();
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        public void Start()
        {
            _redisConnectionMultiplexer = ConnectionMultiplexer.Connect(_connectionString);
            Subscribe();
        }

        /// <summary>
        /// The processor method to run on the listened queue.
        /// </summary>
        /// <param name="envelope">The envelope to process.</param>
        protected abstract Task RunProcessorMethod(Envelope envelope);

        /// <summary>
        /// Subscribe to the necessary communication endpoints.
        /// </summary>
        private void Subscribe()
        {
            _redisSubscriber = _redisConnectionMultiplexer.GetSubscriber();
            _listenedMessageQueue = _redisSubscriber.Subscribe(_listenTarget);
            _listenedMessageQueue.OnMessage(async channelMessage => await RunProcessorMethod(JsonConvert.DeserializeObject<Envelope>(channelMessage.Message)).ConfigureAwait(false));
        }

        /// <summary>
        /// Unsubscribe from the necessary communication endpoints.
        /// </summary>
        private void Unsubscribe()
        {
            _redisSubscriber.Unsubscribe(_listenTarget);
            _listenedMessageQueue = null;
            _redisSubscriber = null;
        }

        /// <summary>
        /// Checks connection to channel and resubscribes if the connection has been lost.
        /// </summary>
        /// <param name="channel">The channel to connect.</param>
        private void CheckChannelConnection(string channel)
        {
            if (!_redisSubscriber.IsConnected(channel))
            {
                Logger.LogWarning("Not connected to server. Reconnecting.");
                Unsubscribe();
                Subscribe();
            }
        }
        
        /// <summary>
        /// Sends data to the selected communication endpoint.
        /// </summary>
        /// <param name="target">The target where to send data.</param>
        /// <param name="data">The data to send.</param>
        protected void SendEnvelope(string target, Envelope data)
        {
            CheckChannelConnection(target);
            _redisSubscriber.Publish(target, JsonConvert.SerializeObject(data), CommandFlags.FireAndForget);
        }

        /// <summary>
        /// Sends an error message to the specified target.
        /// </summary>
        /// <param name="target">The target where to send data.</param>
        /// <param name="errorMessage">The error message.</param>
        protected void SendErrorMessage(string target, string errorMessage)
        {
            SendEnvelope(target, new Envelope(EventNames.ErrorMessage, errorMessage));
        }
    }
}