using System;
using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Common.Options;
using Faemiyah.BtDamageResolver.Services.Events;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Faemiyah.BtDamageResolver.Services.Database
{
    public class LoggingRepository
    {
        private readonly ILogger<LoggingRepository> _logger;
        private readonly FaemiyahClusterOptions _clusterOptions;
        private NpgsqlConnection _connection;
        private readonly MD5 _md5;

        public LoggingRepository(ILogger<LoggingRepository> logger, FaemiyahClusterOptions clusterOptions)
        {
            _logger = logger;
            _clusterOptions = clusterOptions;
            _md5 = MD5.Create();
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
                        _connection, transaction);
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

        public async Task WriteUserLogEntries(ConcurrentQueue<PlayerLogEntry> entries)
        {
            try
            {
                await RefreshConnection();

                await using var transaction = _connection.BeginTransaction();

                while (!entries.IsEmpty)
                {
                    entries.TryDequeue(out var entry);
                    await using var cmd = new NpgsqlCommand(
                        "INSERT INTO ResolverLogPlayer (EventTime, PlayerId, ActionType, ActionData) VALUES (@timeStamp, @playerId, @actionType, @actionData)",
                        _connection, transaction);
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

        public async Task WriteUnitLogEntries(ConcurrentQueue<UnitLogEntry> entries)
        {
            try
            {
                await RefreshConnection();

                await using var transaction = await _connection.BeginTransactionAsync();

                while (!entries.IsEmpty)
                {
                    entries.TryDequeue(out var entry);
                    await using var cmd = new NpgsqlCommand(
                        "INSERT INTO ResolverLogUnit (EventTime, UnitId, ActionType, ActionData) VALUES (@timeStamp, @unitId, @actionType, @actionData)",
                        _connection, transaction);
                    cmd.Parameters.AddWithValue("timeStamp", entry.TimeStamp);
                    cmd.Parameters.AddWithValue("unitId", entry.UnitId.GetHashCode());
                    cmd.Parameters.AddWithValue("actionType", (int)entry.ActionType);
                    cmd.Parameters.AddWithValue("actionData", entry.ActionData);

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failure writing unit log entry.");
            }
        }

        private byte[] GetMd5Hash(string input)
        {
            return _md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        }
    }
}