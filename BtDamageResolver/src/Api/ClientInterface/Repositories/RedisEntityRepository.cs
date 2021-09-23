using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories
{
    /// <summary>
    /// A redis-based entity repository, which directly stores all entities into database 0.
    /// Entities are stored with their string-based name, and prefixed with Resolver{EntityName}
    /// </summary>
    /// <typeparam name="TEntity">The entity to store.</typeparam>
    /// <remarks>For this repository, the key of the entity key must be string-based.</remarks>
    public class RedisEntityRepository<TEntity> : IEntityRepository<TEntity, string>
        where TEntity : class, IEntity<string>
    {
        private readonly ILogger<RedisEntityRepository<TEntity>> _logger;
        private readonly string _connectionString;
        private readonly string _keyPrefix;
        private readonly IConnectionMultiplexer _redisConnectionMultiplexer;

        public RedisEntityRepository(ILogger<RedisEntityRepository<TEntity>> logger, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
            _keyPrefix = $"Resolver{typeof(TEntity).Name}";
            _redisConnectionMultiplexer = ConnectionMultiplexer.Connect(_connectionString);
        }

        private string GetKey(TEntity entity)
        {
            return $"{_keyPrefix}_{entity.GetId()}";
        }

        private string GetKey(string key)
        {
            return $"{_keyPrefix}_{key}";
        }

        private IDatabase GetConnection()
        {
            return _redisConnectionMultiplexer.GetDatabase();
        }

        private IServer GetServer()
        {
            return _redisConnectionMultiplexer.GetServer(_connectionString);
        }

        /// <inheritdoc />
        public async Task Add(TEntity entity)
        {
            try
            {
                var connection = GetConnection();
                await connection.StringSetAsync(GetKey(entity), JsonConvert.SerializeObject(entity));
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not add entity {entityName} of type {entityType} into redis database. Database failure with error code {code}.", entity.GetId(), typeof(TEntity), ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not add entity {entityName} of type {entityType} into redis database. Unknown failure.", entity.GetId(), typeof(TEntity));
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task AddOrUpdate(TEntity entity)
        {
            // We do not have to delete the old one while using Redis.
            await Add(entity);
        }

        /// <inheritdoc />
        public async Task Delete(string key)
        {
            try
            {
                var connection = GetConnection();
                await connection.KeyDeleteAsync(GetKey(key));
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not delete entity {entityName} of type {entityType}. Database failure with error code {code}.", key, typeof(TEntity), ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete entity {entityName} of type {entityType}. Unknown failure.", key, typeof(TEntity));
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task<TEntity> Get(string key)
        {
            try
            {
                var connection = GetConnection();
                var value = await connection.StringGetAsync(GetKey(key));

                if (value != RedisValue.Null)
                {
                    return JsonConvert.DeserializeObject<TEntity>(value);
                }

                return null;
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not get entity {entityName} of type {entityType}. Database failure with error code {code}.", key, typeof(TEntity), ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get entity {entityName} of type {entityType}. Unknown failure.", key, typeof(TEntity));
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task<List<TEntity>> GetAll()
        {
            try
            {
                var server = GetServer();
                var entities = new List<TEntity>();

                foreach (var key in server.Keys(pattern: $"{_keyPrefix}*"))
                {
                    entities.Add(await Get(key));
                }

                return entities;
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not get all entities of type {entityType}. Database failure with error code {code}.", typeof(TEntity), ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get all entities of type {entityType}. Unknown failure.", typeof(TEntity));
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task Update(TEntity entity)
        {
            try
            {
                var connection = GetConnection();
                if (connection.KeyExists(GetKey(entity)))
                {
                    await Add(entity);
                }
                else
                {
                    throw new DataAccessException(DataAccessErrorCode.NotFound);
                }
            }
            catch (DataAccessException)
            {
                throw;
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Could not update entity {entityName} of type {entityType}. Database failure with error code {code}.", entity.GetId(), typeof(TEntity), ex.ErrorCode);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not update entity {entityName} of type {entityType}. Unknown failure.", entity.GetId(), typeof(TEntity));
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }
    }
}