using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Exceptions;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;

/// <summary>
/// A cached entity repository, which caches items locally and fetches from the back-end repository, if local cache is missing a value.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public class CachedEntityRepository<TEntity, TKey> : IEntityRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IComparable
{
    private readonly ILogger<CachedEntityRepository<TEntity, TKey>> _logger;
    private readonly IEntityRepository<TEntity, TKey> _repository;
    private readonly ConcurrentDictionary<TKey, TEntity> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedEntityRepository{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="repository">The repository to cache.</param>
    /// <remarks>
    /// This cache is a DI singleton accessed concurrently from both the owning <c>*RepositoryActor</c> grain
    /// (single-threaded writes) and directly from many <c>LogicUnit</c> instances during fire events
    /// (concurrent reads from arbitrary grain activations). A <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// is required to make this safe.
    /// </remarks>
    public CachedEntityRepository(ILogger<CachedEntityRepository<TEntity, TKey>> logger, IEntityRepository<TEntity, TKey> repository)
    {
        _logger = logger;
        _repository = repository;
        _cache = new ConcurrentDictionary<TKey, TEntity>();
        _logger.LogInformation("Filled entity cache for type {Type} with {Number} items.", typeof(TEntity).Name, FillCache());
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity)
    {
        try
        {
            await _repository.AddAsync(entity);
            if (!_cache.TryAdd(entity.GetName(), entity))
            {
                throw new ArgumentException($"An item with key '{entity.GetName()}' already exists in the cache.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not add entity {EntityName} into the repository. Unknown failure.", entity.GetName());
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(TEntity entity)
    {
        try
        {
            await _repository.AddOrUpdateAsync(entity);
            _cache[entity.GetName()] = entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not add or update entity {EntityName} in the repository. Unknown failure.", entity.GetName());
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(TKey key)
    {
        try
        {
            if (_cache.ContainsKey(key))
            {
                await _repository.DeleteAsync(key);
                _cache.TryRemove(key, out _);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not delete entity {EntityName} from the repository. Unknown failure.", key);
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    /// <inheritdoc />
    public TEntity Get(TKey key)
    {
        return _cache.TryGetValue(key, out var entity) ? entity : null;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<TEntity> GetAll()
    {
        return _cache.Values.ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<TKey> GetAllKeys()
    {
        var keys = _repository.GetAllKeys();

        // Update local cache in this situation
        foreach (var key in keys.Where(key => !_cache.ContainsKey(key)))
        {
            var item = _repository.Get(key);
            _cache.TryAdd(item.GetName(), item);
        }

        return keys;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TEntity entity)
    {
        try
        {
            await _repository.UpdateAsync(entity);
            _cache[entity.GetName()] = entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update entity {EntityName} in repository. Unknown failure.", entity.GetName());
            throw new DataAccessException(DataAccessErrorCode.OperationFailure);
        }
    }

    private int FillCache()
    {
        var items = _repository.GetAll();
        var count = 0;
        foreach (var item in items)
        {
            _cache.TryAdd(item.GetName(), item);
            count++;
        }

        return count;
    }
}