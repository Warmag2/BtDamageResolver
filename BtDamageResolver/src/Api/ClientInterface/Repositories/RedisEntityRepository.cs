using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;

/// <summary>
/// A Redis-based entity repository, which directly stores all entities into database 0.
/// Entities are stored with their string-based name, and prefixed with Resolver{EntityName}.
/// </summary>
/// <typeparam name="TEntity">The entity to store.</typeparam>
/// <remarks>For this repository, the key of the entity key must be string-based.</remarks>
public class RedisEntityRepository<TEntity> : IEntityRepository<TEntity, string>
    where TEntity : class, IEntity<string>
{
    private readonly string _keyPrefix;
    private readonly ILogger<RedisEntityRepository<TEntity>> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConnectionMultiplexer _connectionMultiplexer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisEntityRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="jsonSerializerOptions">JSON serializer options.</param>
    /// <param name="connectionString">The connection string.</param>
    public RedisEntityRepository(ILogger<RedisEntityRepository<TEntity>> logger, IOptions<JsonSerializerOptions> jsonSerializerOptions, string connectionString)
    {
        _logger = logger;
        _jsonSerializerOptions = jsonSerializerOptions.Value;
        _keyPrefix = $"Resolver{typeof(TEntity).Name}";
        _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity)
    {
        try
        {
            var connection = GetConnection();
            await connection.StringSetAsync(GetKey(entity), JsonSerializer.Serialize(entity, _jsonSerializerOptions)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not add entity {EntityName} of type {EntityType} into the database.", entity.GetName(), typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure, $"Could not add entity {entity.GetName()} of type {typeof(TEntity)} into the database.", ex);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not delete entity {EntityName} of type {EntityType}.", key, typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure, $"Could not delete entity {key} of type {typeof(TEntity)}.", ex);
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
                var entity = JsonSerializer.Deserialize<TEntity>(value.ToString(), _jsonSerializerOptions);
                return entity;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get entity {EntityName} of type {EntityType}.", key, typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure, $"Could not get entity {key} of type {typeof(TEntity)}.", ex);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<TEntity> GetAll()
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get all entities of type {EntityType}.", typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure, $"Could not get all entities of type {typeof(TEntity)}.", ex);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetAllKeys()
    {
        try
        {
            var server = GetServer();
            return server.Keys(pattern: $"{_keyPrefix}*").Select(key => key.ToString()[(_keyPrefix.Length + 1)..]).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get keys for all entities of type {EntityType}.", typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure, $"Could not get keys for all entities of type {typeof(TEntity)}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TEntity entity)
    {
        try
        {
            var connection = GetConnection();
            var updated = await connection.StringSetAsync(
                GetKey(entity),
                JsonSerializer.Serialize(entity, _jsonSerializerOptions),
                when: When.Exists).ConfigureAwait(false);

            if (!updated)
            {
                throw new DataAccessException(DataAccessErrorCode.NotFound);
            }
        }
        catch (DataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update entity {EntityName} of type {EntityType}.", entity.GetName(), typeof(TEntity));
            throw new DataAccessException(DataAccessErrorCode.OperationFailure, $"Could not update entity {entity.GetName()} of type {typeof(TEntity)}.", ex);
        }
    }

    private IDatabase GetConnection()
    {
        return _connectionMultiplexer.GetDatabase();
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
        return _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
    }
}
