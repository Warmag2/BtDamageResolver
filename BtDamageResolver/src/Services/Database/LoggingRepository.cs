using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Common.Options;
using Faemiyah.BtDamageResolver.Services.Events;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Faemiyah.BtDamageResolver.Services.Database;

/// <summary>
/// The logging database access.
/// Uses a short-lived connection per write batch (Npgsql pools connections internally),
/// so no persistent connection is held and no disposal is required.
/// GameId and PlayerId are stored as FNV-1a 64-bit hashes (BIGINT columns) for
/// deterministic, cross-session-stable identifiers.
/// </summary>
public class LoggingRepository
{
    private readonly ILogger<LoggingRepository> _logger;
    private readonly FaemiyahClusterOptions _clusterOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingRepository"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="clusterOptions">The cluster options.</param>
    public LoggingRepository(ILogger<LoggingRepository> logger, FaemiyahClusterOptions clusterOptions)
    {
        _logger = logger;
        _clusterOptions = clusterOptions;
    }

    /// <summary>
    /// Writes log entries from a queue, supporting both <see cref="GameLogEntry"/> and <see cref="PlayerLogEntry"/>.
    /// </summary>
    /// <typeparam name="T">The log entry type.</typeparam>
    /// <param name="entries">The log entries to write.</param>
    /// <returns>A task which finishes when the entries have been written.</returns>
    /// <remarks>
    /// Requires ResolverLogGame.GameId and ResolverLogPlayer.PlayerId to be BIGINT columns.
    /// </remarks>
    public async Task WriteLogEntries<T>(ConcurrentQueue<T> entries)
    {
        var batch = DrainQueue(entries);
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_clusterOptions.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            foreach (var entry in batch)
            {
                await using var cmd = BuildCommand(entry, connection, transaction);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure writing {Count} {Type} log entries. Re-enqueuing.", batch.Count, typeof(T).Name);
            foreach (var entry in batch)
            {
                entries.Enqueue(entry);
            }
        }
    }

    private static NpgsqlCommand BuildCommand<T>(T entry, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        entry switch
        {
            GameLogEntry g => BuildGameLogCommand(g, connection, transaction),
            PlayerLogEntry p => BuildPlayerLogCommand(p, connection, transaction),
            _ => throw new ArgumentException($"Unknown log entry type: {typeof(T).Name}")
        };

    private static NpgsqlCommand BuildGameLogCommand(GameLogEntry entry, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var cmd = new NpgsqlCommand(
            "INSERT INTO ResolverLogGame (EventTime, GameId, ActionType, ActionData) VALUES (@timeStamp, @gameId, @actionType, @actionData)",
            connection,
            transaction);
        cmd.Parameters.AddWithValue("timeStamp", entry.TimeStamp);
        cmd.Parameters.AddWithValue("gameId", entry.GameId.Fnv1aHash64());
        cmd.Parameters.AddWithValue("actionType", (int)entry.ActionType);
        cmd.Parameters.AddWithValue("actionData", entry.ActionData);
        return cmd;
    }

    private static NpgsqlCommand BuildPlayerLogCommand(PlayerLogEntry entry, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var cmd = new NpgsqlCommand(
            "INSERT INTO ResolverLogPlayer (EventTime, PlayerId, ActionType, ActionData) VALUES (@timeStamp, @playerId, @actionType, @actionData)",
            connection,
            transaction);
        cmd.Parameters.AddWithValue("timeStamp", entry.TimeStamp);
        cmd.Parameters.AddWithValue("playerId", entry.PlayerId.Fnv1aHash64());
        cmd.Parameters.AddWithValue("actionType", (int)entry.ActionType);
        cmd.Parameters.AddWithValue("actionData", entry.ActionData);
        return cmd;
    }

    private static List<T> DrainQueue<T>(ConcurrentQueue<T> queue)
    {
        var batch = new List<T>();
        while (queue.TryDequeue(out var item))
        {
            batch.Add(item);
        }

        return batch;
    }
}