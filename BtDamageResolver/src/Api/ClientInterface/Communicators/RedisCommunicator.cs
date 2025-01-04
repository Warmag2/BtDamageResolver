using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;

/// <summary>
/// Base Redis implementation of a communicator.
/// </summary>
public abstract class RedisCommunicator
{
    /// <summary>
    /// The common client stream address.
    /// </summary>
    protected const string ClientStreamAddress = "BtDamageResolverClient";

    /// <summary>
    /// The server stream address.
    /// </summary>
    protected const string ServerStreamAddress = "BtDamageResolverServer";

    /// <summary>
    /// The logging interface.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The JSON serializer options.
    /// </summary>
    protected readonly JsonSerializerOptions JsonSerializerOptions;

    /// <summary>
    /// The data helper.
    /// </summary>
    protected readonly DataHelper DataHelper;

    /// <summary>
    /// The Redis subscriber.
    /// </summary>
    protected ISubscriber RedisSubscriber;

    private readonly string _connectionString;
    private readonly RedisChannel _listenTarget;
    private ChannelMessageQueue _listenedMessageQueue;
    private ConnectionMultiplexer _redisConnectionMultiplexer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCommunicator"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="jsonSerializerOptions">JSON serializer options.</param>
    /// <param name="connectionString">The server connection string.</param>
    /// <param name="dataHelper">The data compression helper.</param>
    /// <param name="listenTarget">The target channel to listen to.</param>
    protected RedisCommunicator(ILogger logger, IOptions<JsonSerializerOptions> jsonSerializerOptions, string connectionString, DataHelper dataHelper, string listenTarget)
    {
        Logger = logger;
        JsonSerializerOptions = jsonSerializerOptions.Value;
        _connectionString = connectionString;
        DataHelper = dataHelper;
        _listenTarget = new RedisChannel(listenTarget, RedisChannel.PatternMode.Literal);

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
    /// Uninitialize.
    /// </summary>
    public void Stop()
    {
        Unsubscribe();
        _redisConnectionMultiplexer.CloseAsync();
    }

    /// <summary>
    /// The processor method to run on the listened queue.
    /// </summary>
    /// <param name="envelope">The envelope to process.</param>
    /// <returns>
    /// A task which completes when the listening queue finishes processing.
    /// </returns>
    protected abstract Task RunProcessorMethod(Envelope envelope);

    /// <summary>
    /// Additional subscriptions.
    /// </summary>
    protected virtual void SubscribeAdditional()
    {
    }

    /// <summary>
    /// Sends data to the selected communication endpoint.
    /// </summary>
    /// <param name="target">The target where to send data.</param>
    /// <param name="data">The data to send.</param>
    protected void SendEnvelope(string target, Envelope data)
    {
        var channel = new RedisChannel(target, RedisChannel.PatternMode.Literal);
        CheckChannelConnection(channel);
        RedisSubscriber.Publish(channel, JsonSerializer.Serialize(data, JsonSerializerOptions), CommandFlags.FireAndForget);
    }

    /// <summary>
    /// Sends an error message to the specified target.
    /// </summary>
    /// <param name="target">The target where to send data.</param>
    /// <param name="errorMessage">The error message.</param>
    protected void SendErrorMessage(string target, string errorMessage)
    {
        SendEnvelope(target, new Envelope(EventNames.ErrorMessage, DataHelper.Pack(new ClientErrorEvent(errorMessage))));
    }

    /// <summary>
    /// Checks connection to channel and resubscribes if the connection has been lost.
    /// </summary>
    /// <param name="channel">The channel to connect.</param>
    private void CheckChannelConnection(RedisChannel channel)
    {
        if (!RedisSubscriber.IsConnected(channel))
        {
            Logger.LogWarning("Not connected to server. Reconnecting.");
            Unsubscribe();
            Subscribe();
        }
    }

    /// <summary>
    /// Subscribe to the necessary communication endpoints.
    /// </summary>
    private void Subscribe()
    {
        RedisSubscriber = _redisConnectionMultiplexer.GetSubscriber();
        _listenedMessageQueue = RedisSubscriber.Subscribe(_listenTarget);
        _listenedMessageQueue.OnMessage(async channelMessage => await RunProcessorMethod(JsonSerializer.Deserialize<Envelope>(channelMessage.Message, JsonSerializerOptions)).ConfigureAwait(false));
        SubscribeAdditional();
    }

    /// <summary>
    /// Unsubscribe from the necessary communication endpoints.
    /// </summary>
    private void Unsubscribe()
    {
        RedisSubscriber.UnsubscribeAll();
        _listenedMessageQueue.Unsubscribe();
        _listenedMessageQueue = null;
        RedisSubscriber = null;
    }
}