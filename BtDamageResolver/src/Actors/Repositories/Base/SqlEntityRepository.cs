﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Exceptions;
using Faemiyah.BtDamageResolver.Api.Interfaces.ActorInterfacePrototypes;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;

using static System.Text.Json.JsonSerializer;

namespace Faemiyah.BtDamageResolver.Actors.Repositories.Base
{
    /// <summary>
    /// A SQL-based entity repository, which directly stores the entities into a database.
    /// Checks the existence of a suitable table in the given database and creates one if necessary.
    /// </summary>
    /// <typeparam name="TEntity">The entity to store.</typeparam>
    /// <remarks>For this repository, the key of the entity key must be string-based.</remarks>
    public class SqlEntityRepository<TEntity> : IEntityRepository<TEntity, string>
        where TEntity : class, IEntity<string>
    {
        private readonly ILogger<SqlEntityRepository<TEntity>> _logger;
        private readonly FaemiyahClusterOptions _clusterOptions;
        private readonly string _tableName;

        public SqlEntityRepository(ILogger<SqlEntityRepository<TEntity>> logger, IOptions<FaemiyahClusterOptions> clusterOptions)
        {
            _logger = logger;
            _clusterOptions = clusterOptions.Value;
            _tableName = $"Resolver{typeof(TEntity).Name}";

            EnsureTableExists().Wait();
        }

        private async Task<NpgsqlConnection> GetConnection()
        {
            var connection = new NpgsqlConnection(_clusterOptions.ConnectionString);
            await connection.OpenAsync();

            return connection;
        }

        private async Task EnsureTableExists()
        {
            try
            {
                await using var connection = await GetConnection();
                await using var command = new NpgsqlCommand(
                    @$"CREATE TABLE IF NOT EXISTS public.{_tableName}
                    (
                        id varchar(256) NOT NULL,
                        data jsonb NOT NULL,
                        CONSTRAINT {_tableName}_PrimaryKey PRIMARY KEY(id)
                    )",
                    connection);

                await command.ExecuteNonQueryAsync();
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not ensure that table {tableName} exists in the database. Database failure with error code {code}.", _tableName, ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not ensure that table {tableName} exists in the database.", _tableName);
            }
        }

        /// <inheritdoc />
        public async Task Add(TEntity entity)
        {
            try
            {
                await using var connection = await GetConnection();
                await using var cmd = new NpgsqlCommand($"INSERT INTO {_tableName} (id, data) VALUES (@p_id, @p_data)", connection);
                cmd.Parameters.AddWithValue("p_id", entity.GetId());
                cmd.Parameters.AddWithValue("p_data", JsonDocument.Parse(SerializeToUtf8Bytes(entity)));
                await cmd.ExecuteNonQueryAsync();
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not add entity {entityName} into table {tableName}. Database failure with error code {code}.", entity.GetId(), _tableName, ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not add entity {entityName} into table {tableName}. Unknown failure.", entity.GetId(), _tableName);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task AddOrUpdate(TEntity entity)
        {
            await Delete(entity.GetId());
            await Add(entity);
        }

        /// <inheritdoc />
        public async Task Delete(string key)
        {
            try
            {
                await using var connection = await GetConnection();
                await using var cmd = new NpgsqlCommand($"DELETE FROM {_tableName} WHERE id = @p_id", connection);
                cmd.Parameters.AddWithValue("p_id", key);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not delete entity {entityName} from table {tableName}. Database failure with error code {code}.", key, _tableName, ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete entity {entityName} from table {tableName}. Unknown failure.", key, _tableName);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task<TEntity> Get(string key)
        {
            try
            {
                await using var connection = await GetConnection();
                await using var cmd = new NpgsqlCommand($"SELECT data FROM {_tableName} WHERE id = @p_id", connection);
                cmd.Parameters.AddWithValue("p_id", key);
                var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return JsonConvert.DeserializeObject<TEntity>(reader.GetString(0));
                }

                return null;
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not get entity {entityName} from table {tableName}. Database failure with error code {code}.", key, _tableName, ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get entity {entityName} from table {tableName}. Unknown failure.", key, _tableName);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task<List<TEntity>> GetAll()
        {
            try
            {
                await using var connection = await GetConnection();
                await using var cmd = new NpgsqlCommand($"SELECT data FROM {_tableName}", connection);
                var reader = await cmd.ExecuteReaderAsync();

                var returnValue = new List<TEntity>();

                while(await reader.ReadAsync())
                {
                    returnValue.Add(JsonConvert.DeserializeObject<TEntity>(reader.GetString(0)));
                }

                return returnValue;
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not get all entities from table {tableName}. Database failure with error code {code}.", _tableName, ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get all entities from table {tableName}. Unknown failure.", _tableName);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task Update(TEntity entity)
        {
            try
            {
                await using var connection = await GetConnection();
                await using var cmd = new NpgsqlCommand($"UPDATE {_tableName} SET data = @p_data WHERE id = @p_id", connection);
                cmd.Parameters.AddWithValue("p_id", entity.GetId());
                cmd.Parameters.AddWithValue("p_data", JsonConvert.SerializeObject(entity));
                await cmd.ExecuteNonQueryAsync();
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not update entity {entityName} from table {tableName}. Database failure with error code {code}.", entity.GetId(), _tableName, ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not update entity {entityName} from table {tableName}. Unknown failure.", entity.GetId(), _tableName);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }
    }
}