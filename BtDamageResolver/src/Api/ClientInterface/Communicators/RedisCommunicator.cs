using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Microsoft.Extensions.Logging;
using SevenZip.Compression.LZMA;
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

        /// <summary>
        /// The common client stream address.
        /// </summary>
        protected const string ClientStreamAddress = "BtDamageResolverClient";

        protected readonly ILogger Logger;
        protected readonly DataHelper DataHelper;
        private readonly string _connectionString;
        private readonly string _listenTarget;
        private ChannelMessageQueue _listenedMessageQueue;
        private ConnectionMultiplexer _redisConnectionMultiplexer;
        protected ISubscriber RedisSubscriber;

        /// <summary>
        /// Redis implementation of a server-client communication interface.
        /// </summary>
        /// <param name="logger">The logging interface</param>
        /// <param name="dataHelper">The data helper.</param>
        /// <param name="connectionString">The server connection string.</param>
        /// <param name="listenTarget">The target channel to listen to.</param>
        protected RedisCommunicator(ILogger logger, DataHelper dataHelper, string connectionString, string listenTarget)
        {
            Logger = logger;
            DataHelper = dataHelper;
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
            RedisSubscriber = _redisConnectionMultiplexer.GetSubscriber();
            _listenedMessageQueue = RedisSubscriber.Subscribe(_listenTarget);
            _listenedMessageQueue.OnMessage(async channelMessage => await RunProcessorMethod(DataHelper.Deserialize<Envelope>(channelMessage.Message)).ConfigureAwait(false));
            SubscribeAdditional();
        }

        /// <summary>
        /// Additional subscriptions.
        /// </summary>
        protected virtual void SubscribeAdditional()
        {
        }

        /// <summary>
        /// Unsubscribe from the necessary communication endpoints.
        /// </summary>
        private void Unsubscribe()
        {
            RedisSubscriber.UnsubscribeAll();
            _listenedMessageQueue = null;
            RedisSubscriber = null;
        }

        /// <summary>
        /// Checks connection to channel and resubscribes if the connection has been lost.
        /// </summary>
        /// <param name="channel">The channel to connect.</param>
        private void CheckChannelConnection(string channel)
        {
            if (!RedisSubscriber.IsConnected(channel))
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
            RedisSubscriber.Publish(target, DataHelper.Serialize(data), CommandFlags.FireAndForget);
        }

        /// <summary>
        /// Sends an error message to the specified target.
        /// </summary>
        /// <param name="target">The target where to send data.</param>
        /// <param name="errorMessage">The error message.</param>
        protected void SendErrorMessage(string target, string errorMessage)
        {
            SendEnvelope(target, new Envelope(EventNames.ErrorMessage, DataHelper.Pack(errorMessage)));
        }
    }
}