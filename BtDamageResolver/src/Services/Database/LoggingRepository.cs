using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Common.Options;
using Faemiyah.BtDamageResolver.Services.Events;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Faemiyah.BtDamageResolver.Services.Database;

/// <summary>
/// The logging database access.
/// </summary>
public class LoggingRepository
{
    private readonly ILogger<LoggingRepository> _logger;
    private readonly FaemiyahClusterOptions _clusterOptions;
    private NpgsqlConnection _connection;

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
    /// Writes game log entries.
    /// </summary>
    /// <param name="entries">The game log entries to write.</param>
    /// <returns>A task which finishes when the entries have been written.</returns>
    public async Task WriteGameLogEntries(ConcurrentQueue<GameLogEntry> entries)
    {
        try
        {
            await RefreshConnection();

            await using var transaction = await _connection.BeginTransactionAsync();

            while (!entries.IsEmpty)
            {
                entries.TryDequeue(out var entry);
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO ResolverLogGame (EventTime, GameId, ActionType, ActionData) VALUES (@timeStamp, @gameId, @actionType, @actionData)",
                    _connection,
                    transaction);
                cmd.Parameters.AddWithValue("timeStamp", entry.TimeStamp);
                cmd.Parameters.AddWithValue("gameId", entry.GameId.GetHashCode());
                cmd.Parameters.AddWithValue("actionType", (int)entry.ActionType);
                cmd.Parameters.AddWithValue("actionData", entry.ActionData);

                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure writing game log entry.");
        }
    }

    /// <summary>
    /// Writes user log entries.
    /// </summary>
    /// <param name="entries">The user log entries to write.</param>
    /// <returns>A task which finishes when the entries have been written.</returns>
    public async Task WriteUserLogEntries(ConcurrentQueue<PlayerLogEntry> entries)
    {
        try
        {
            await RefreshConnection();

            await using var transaction = await _connection.BeginTransactionAsync();

            while (!entries.IsEmpty)
            {
                entries.TryDequeue(out var entry);
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO ResolverLogPlayer (EventTime, PlayerId, ActionType, ActionData) VALUES (@timeStamp, @playerId, @actionType, @actionData)",
                    _connection,
                    transaction);
                cmd.Parameters.AddWithValue("timeStamp", entry.TimeStamp);
                cmd.Parameters.AddWithValue("playerId", entry.PlayerId.GetHashCode());
                cmd.Parameters.AddWithValue("actionType", (int)entry.ActionType);
                cmd.Parameters.AddWithValue("actionData", entry.ActionData);

                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure writing user log entry.");
        }
    }

    private async Task RefreshConnection()
    {
        if (_connection == null || _connection.State == ConnectionState.Closed)
        {
            _logger.LogInformation("Logging repository trying to connect to PostgreSql.");

            _connection = new NpgsqlConnection(_clusterOptions.ConnectionString);
            await _connection.OpenAsync();

            _logger.LogInformation("Logging repository connected to PostgreSql.");
        }
    }
}