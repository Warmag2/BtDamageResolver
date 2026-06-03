using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;

/// <summary>
/// In-process, per-circuit dispatcher that delivers server-originated events directly to their
/// registered handlers.
/// </summary>
/// <remarks>
/// Replaces the previous self-looping SignalR <c>HubConnection</c>/<c>ClientHub</c> pair, which existed only
/// to funnel Redis-delivered events back into the circuit. Events arrive on independent Redis subscription
/// queues (the player channel and the common client channel) that can run concurrently, so a single gate
/// serializes handler execution to preserve the one-at-a-time ordering the old single SignalR receive loop
/// provided. Handlers therefore continue to run off the Blazor render dispatcher, exactly as before.
/// </remarks>
public sealed class ClientMessageDispatcher : IDisposable
{
    private readonly ConcurrentDictionary<string, Func<byte[], Task>> _handlers = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Registers (or replaces) the handler invoked for the given event name.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="handler">The handler to invoke with the event payload.</param>
    public void On(string eventName, Func<byte[], Task> handler)
    {
        _handlers[eventName] = handler;
    }

    /// <summary>
    /// Removes the handler registered for the given event name, if any.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    public void Off(string eventName)
    {
        _handlers.TryRemove(eventName, out _);
    }

    /// <summary>
    /// Dispatches the payload to the handler registered for the given event name, serializing execution so
    /// that handlers fed by concurrent Redis subscription queues never run at the same time.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="data">The event payload.</param>
    /// <returns>A task which completes when the handler has finished, or immediately if none is registered.</returns>
    public async Task DispatchAsync(string eventName, byte[] data)
    {
        if (!_handlers.TryGetValue(eventName, out var handler))
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await handler(data).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Dispose();
    }
}
