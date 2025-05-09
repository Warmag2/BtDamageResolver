using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;

/// <summary>
/// A redis-based entity repository, which directly stores all entities into database 0.
/// Entities are stored with their string-based name, and prefixed with Resolver{EntityName}.
/// </summary>
/// <typeparam name="TEntity">The entity to store.</typeparam>
/// <remarks>For this repository, the key of the entity key must be string-based.</remarks>
public class RedisEntityRepository<TEntity> : IEntityRepository<TEntity, string>
    where TEntity : class, IEntity<string>
{
    private readonly string _connectionString;
    private readonly string _keyPrefix;
    private readonly ILogger<RedisEntityRepository<TEntity>> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConnectionMultiplexer _redisConnectionMultiplexer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisEntityRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="jsonSerializerSettings">JSON serializer options.</param>
    /// <param name="connectionString">The connection string.</param>
    public RedisEntityRepository(ILogger<RedisEntityRepository<TEntity>> logger, IOptions<JsonSerializerOptions> jsonSerializerSettings, string connectionString)
    {
        _logger = logger;
        _jsonSerializerOptions = jsonSerializerSettings.Value;
        _connectionString = connectionString;
        _keyPrefix = $"Resolver{typeof(TEntity).Name}";
        _redisConnectionMultiplexer = ConnectionMultiplexer.Connect(_connectionString);
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity)
    {
        try
        {
            var connection = GetConnection();
            await connection.StringSetAsync(GetKey(entity), JsonSerializer.Serialize(entity, _jsonSerializerOptions)).ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Could not add entity {EntityName} of type {EntityType} into redis database. Database failure with error code {Code}.", entity.GetName(), typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not add entity {EntityName} of type {EntityType} into redis database. Unknown failure.", entity.GetName(), typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(TEntity entity)
    {
        // We do not have to delete the old one while using Redis.
        await AddAsync(entity).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            var connection = GetConnection();
            return await connection.KeyDeleteAsync(GetKey(key)).ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Could not delete entity {EntityName} of type {EntityType}. Database failure with error code {Code}.", key, typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not delete entity {EntityName} of type {EntityType}. Unknown failure.", key, typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public TEntity Get(string key)
    {
        try
        {
            var connection = GetConnection();
            var value = connection.StringGet(GetKey(key));

            if (value != RedisValue.Null)
            {
                var entity = JsonSerializer.Deserialize<TEntity>(value, _jsonSerializerOptions);
                return entity;
            }

            return null;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Could not get entity {EntityName} of type {EntityType}. Database failure with error code {Code}.", key, typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get entity {EntityName} of type {EntityType}. Unknown failure.", key, typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public async Task<TEntity> GetAsync(string key)
    {
        try
        {
            var connection = GetConnection();
            var value = await connection.StringGetAsync(GetKey(key)).ConfigureAwait(false);

            if (value != RedisValue.Null)
            {
                var entity = JsonSerializer.Deserialize<TEntity>(value, _jsonSerializerOptions);
                return entity;
            }

            return null;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Could not get entity {EntityName} of type {EntityType}. Database failure with error code {Code}.", key, typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get entity {EntityName} of type {EntityType}. Unknown failure.", key, typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public List<TEntity> GetAll()
    {
        try
        {
            var entities = new List<TEntity>();

            foreach (var key in GetAllKeys())
            {
                entities.Add(Get(key));
            }

            return entities;
        }
        catch (DataAccessException)
        {
            throw;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Could not get all entities of type {EntityType}. Database failure with error code {Code}.", typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get all entities of type {EntityType}. Unknown failure.", typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public async Task<List<TEntity>> GetAllAsync()
    {
        try
        {
            var entities = new List<TEntity>();

            foreach (var key in GetAllKeys())
            {
                entities.Add(await GetAsync(key).ConfigureAwait(false));
            }

            return entities;
        }
        catch (DataAccessException)
        {
            throw;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Could not get all entities of type {EntityType}. Database failure with error code {Code}.", typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get all entities of type {EntityType}. Unknown failure.", typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public List<string> GetAllKeys()
    {
        try
        {
            var server = GetServer();
            var keys = server.Keys(pattern: $"{_keyPrefix}*");
            return keys.Select(k => k.ToString().Substring($"{_keyPrefix}_".Length)).ToList();
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Could not get keys for all entities of type {EntityType}. Database failure with error code {Code}.", typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get keys for all entities of type {EntityType}. Unknown failure.", typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TEntity entity)
    {
        try
        {
            var connection = GetConnection();
            if (await connection.KeyExistsAsync(GetKey(entity)))
            {
                await AddAsync(entity).ConfigureAwait(false);
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
            _logger.LogError(ex, "Could not update entity {EntityName} of type {EntityType}. Database failure with error code {Code}.", entity.GetName(), typeof(TEntity), ex.ErrorCode);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update entity {EntityName} of type {EntityType}. Unknown failure.", entity.GetName(), typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    private IDatabase GetConnection()
    {
        return _redisConnectionMultiplexer.GetDatabase();
    }

    private string GetKey(TEntity entity)
    {
        return $"{_keyPrefix}_{entity.GetName()}";
    }

    private string GetKey(string key)
    {
        return $"{_keyPrefix}_{key}";
    }

    private IServer GetServer()
    {
        return _redisConnectionMultiplexer.GetServer(_connectionString.Split(',')[0]);
    }
}