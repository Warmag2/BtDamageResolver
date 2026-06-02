using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Extensions;
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
    /// <summary>
    /// Upper bound on the number of entries retained in a queue across write failures.
    /// If a write fails while the backlog is already at or above this size, the failed batch is
    /// dropped instead of re-enqueued, bounding memory use when the database is unavailable for a long time.
    /// </summary>
    private const int MaxRetainedLogEntries = 50000;

    private readonly ILogger<LoggingRepository> _logger;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingRepository"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="connectionString">The database connection string.</param>
    public LoggingRepository(ILogger<LoggingRepository> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    /// <summary>
    /// Writes log entries from a queue, supporting both <see cref="GameLogEntry"/> and <see cref="PlayerLogEntry"/>.
    /// </summary>
    /// <typeparam name="T">The log entry type.</typeparam>
    /// <param name="entries">The log entries to write.</param>
    /// <returns>A task which finishes when the entries have been written.</returns>
    /// <remarks>
    /// Requires ResolverLogGame.GameId and ResolverLogPlayer.PlayerId to be BIGINT columns.
    /// All entries are written in a single pipelined <see cref="NpgsqlBatch"/> inside one transaction,
    /// so a write either fully succeeds or fully fails (the whole batch is re-enqueued on failure, never duplicated).
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
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using var sqlBatch = new NpgsqlBatch(connection, transaction);

            foreach (var entry in batch)
            {
                sqlBatch.BatchCommands.Add(BuildBatchCommand(entry));
            }

            await sqlBatch.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            if (entries.Count >= MaxRetainedLogEntries)
            {
                _logger.LogError(ex, "Failure writing {Count} {Type} log entries. Backlog already at {Backlog}; dropping this batch to bound memory.", batch.Count, typeof(T).Name, entries.Count);
            }
            else
            {
                _logger.LogError(ex, "Failure writing {Count} {Type} log entries. Re-enqueuing for retry.", batch.Count, typeof(T).Name);
                foreach (var entry in batch)
                {
                    entries.Enqueue(entry);
                }
            }
        }
    }

    private static NpgsqlBatchCommand BuildBatchCommand<T>(T entry) =>
        entry switch
        {
            GameLogEntry g => BuildGameLogCommand(g),
            PlayerLogEntry p => BuildPlayerLogCommand(p),
            _ => throw new ArgumentException($"Unknown log entry type: {typeof(T).Name}")
        };

    private static NpgsqlBatchCommand BuildGameLogCommand(GameLogEntry entry)
    {
        var cmd = new NpgsqlBatchCommand(
            "INSERT INTO ResolverLogGame (EventTime, GameId, ActionType, ActionData) VALUES (@timeStamp, @gameId, @actionType, @actionData)");
        cmd.Parameters.AddWithValue("timeStamp", entry.TimeStamp);
        cmd.Parameters.AddWithValue("gameId", entry.GameId.Fnv1aHash64());
        cmd.Parameters.AddWithValue("actionType", (int)entry.ActionType);
        cmd.Parameters.AddWithValue("actionData", entry.ActionData);
        return cmd;
    }

    private static NpgsqlBatchCommand BuildPlayerLogCommand(PlayerLogEntry entry)
    {
        var cmd = new NpgsqlBatchCommand(
            "INSERT INTO ResolverLogPlayer (EventTime, PlayerId, ActionType, ActionData) VALUES (@timeStamp, @playerId, @actionType, @actionData)");
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