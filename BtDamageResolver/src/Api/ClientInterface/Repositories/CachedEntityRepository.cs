using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Exceptions;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories
{
    /// <summary>
    /// A cached entity repository, which caches items locally and fetches from the back-end repository, if local cache is missing a value.
    /// </summary>
    public class CachedEntityRepository<TEntity, TKey> : IEntityRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IComparable
    {
        private readonly ILogger<CachedEntityRepository<TEntity, TKey>> _logger;
        private readonly IEntityRepository<TEntity, TKey> _repository;
        private readonly Dictionary<TKey, TEntity> _cache;

        public CachedEntityRepository(ILogger<CachedEntityRepository<TEntity, TKey>> logger, IEntityRepository<TEntity, TKey> repository)
        {
            _logger = logger;
            _repository = repository;
            _cache = new Dictionary<TKey, TEntity>();
            FillCache().Wait();
        }

        private async Task FillCache()
        {
            var items = await _repository.GetAllAsync();
            foreach (var item in items)
            {
                _cache.Add(item.GetId(), item);
            }
        }

        /// <inheritdoc />
        public async Task AddAsync(TEntity entity)
        {
            try
            {
                await _repository.AddAsync(entity);
                _cache.Add(entity.GetId(), entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not add entity {entityName} into the repository. Unknown failure.", entity.GetId());
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task AddOrUpdateAsync(TEntity entity)
        {
            try
            {
                await _repository.AddOrUpdateAsync(entity);
                if (_cache.ContainsKey(entity.GetId()))
                {
                    _cache[entity.GetId()] = entity;
                }
                else
                {
                    _cache.Add(entity.GetId(), entity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not add or update entity {entityName} in the repository. Unknown failure.", entity.GetId());
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(TKey key)
        {
            try
            {
                if (_cache.ContainsKey(key))
                {
                    await _repository.DeleteAsync(key);
                    _cache.Remove(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete entity {entityName} from the repository. Unknown failure.", key);
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }

        /// <inheritdoc />
        public Task<TEntity> GetAsync(TKey key)
        {
            return _cache.TryGetValue(key, out var entity) ? Task.FromResult(entity) : Task.FromResult((TEntity) null);
        }

        /// <inheritdoc />
        public Task<List<TEntity>> GetAllAsync()
        {
            return Task.FromResult(_cache.Values.ToList());
        }

        /// <inheritdoc />
        public async Task UpdateAsync(TEntity entity)
        {
            try
            {
                await _repository.UpdateAsync(entity);
                _cache[entity.GetId()] = entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not update entity {entityName} in repository. Unknown failure.", entity.GetId());
                throw new DataAccessException(DataAccessErrorCode.OperationFailure);
            }
        }
    }
}