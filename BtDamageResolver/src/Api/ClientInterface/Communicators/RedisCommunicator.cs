using System;
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
public abstract class RedisCommunicator : IDisposable
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
    /// <remarks>
    /// Messages are published with <see cref="CommandFlags.FireAndForget"/>, so the publish does not
    /// wait for, and cannot report, the number of subscribers that received it.
    /// </remarks>
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
        SendSingle(target, new Envelope(EventNames.ErrorMessage, DataHelper.Pack(new ClientErrorEvent(errorMessage))));
    }

    /// <summary>
    /// Send a message to a single client.
    /// </summary>
    /// <param name="clientName">The target where to send data.</param>
    /// <param name="envelope">The data to send.</param>
    protected void SendSingle(string clientName, Envelope envelope)
    {
        SendEnvelope(clientName, envelope);
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
        _listenedMessageQueue.OnMessage(ProcessChannelMessage);
        SubscribeAdditional();
    }

    /// <summary>
    /// Deserializes and processes a single channel message, logging any unhandled exception.
    /// </summary>
    /// <param name="channelMessage">The raw channel message.</param>
    /// <returns>A task which completes when the message has been processed.</returns>
    /// <remarks>
    /// <see cref="ChannelMessageQueue.OnMessage(Func{ChannelMessage, Task})"/> observes the returned task only to
    /// serialize processing; it does not surface faults. Without this guard any exception thrown while handling a
    /// message would be silently swallowed (effectively <c>async void</c>), so we catch and log here.
    /// </remarks>
    protected async Task ProcessChannelMessage(ChannelMessage channelMessage)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(channelMessage.Message.ToString(), JsonSerializerOptions);
            await RunProcessorMethod(envelope).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unhandled exception while processing a message on channel {Channel}.", channelMessage.Channel.ToString());
        }
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

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the Redis resources held by this communicator.
    /// </summary>
    /// <param name="disposing"><c>true</c> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _listenedMessageQueue?.Unsubscribe();
        _listenedMessageQueue = null;
        RedisSubscriber?.UnsubscribeAll();
        RedisSubscriber = null;
        _redisConnectionMultiplexer?.Dispose();
        _redisConnectionMultiplexer = null;
    }
}