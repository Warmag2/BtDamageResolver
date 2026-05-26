using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Extensions;

/// <summary>
/// Extensions for <see cref="Task"/>.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Fire-and-forget a task, logging any exception. Use as a safer alternative to
    /// <c>Task.Ignore()</c> in cases where a grain call must not be awaited (e.g. to avoid
    /// deadlock) but failures should still be surfaced in the log.
    /// </summary>
    /// <param name="task">The task to observe.</param>
    /// <param name="logger">The logger to write failures to.</param>
    /// <param name="messageTemplate">The structured logging message template.</param>
    /// <param name="args">The arguments to fill the message template.</param>
    public static void LogAndForget(this Task task, ILogger logger, string messageTemplate, params object[] args)
    {
        _ = ObserveAsync(task, logger, messageTemplate, args);
    }

    private static async Task ObserveAsync(Task task, ILogger logger, string messageTemplate, object[] args)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, messageTemplate, args);
        }
    }
}
