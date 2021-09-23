using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Faemiyah.BtDamageResolver.Actors.Repositories.Base
{
    /// <summary>
    /// Base repository actor class.
    /// </summary>
    /// <typeparam name="TEntity">The entity to store.</typeparam>
    /// <typeparam name="TKey">The primary key of the entity to store.</typeparam>
    [StatelessWorker(1)]
    public abstract class ExternalRepositoryActorBase<TEntity, TKey> : Grain, IEntityRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IComparable
    {
        protected readonly ILogger Logger;
        protected readonly CachedEntityRepository<TEntity, TKey> Repository;

        protected ExternalRepositoryActorBase(ILogger logger, CachedEntityRepository<TEntity, TKey> repository)
        {
            Logger = logger;
            Repository = repository;
        }

        /// <inheritdoc />
        public virtual async Task Add(TEntity entity)
        {
            await Repository.Add(entity);
            Logger.LogInformation("{entity} {id} added to {repository}.", typeof(TEntity), entity.GetId(), GetType());
        }

        /// <inheritdoc />
        public virtual async Task AddOrUpdate(TEntity entity)
        {
            await Repository.AddOrUpdate(entity);
            Logger.LogInformation("{entity} {id} added to or updated in {repository}.", typeof(TEntity), entity.GetId(), GetType());
        }

        /// <inheritdoc />
        public virtual async Task Delete(TKey key)
        {
            await Repository.Delete(key);
            Logger.LogInformation("{entity} with key {key} deleted from {repository}.", typeof(TEntity), key, GetType());
        }

        /// <inheritdoc />
        public virtual async Task<TEntity> Get(TKey key)
        {
            return await Repository.Get(key);
        }

        /// <inheritdoc />
        public virtual async Task<List<TEntity>> GetAll()
        {
            return await Repository.GetAll();
        }

        /// <inheritdoc />
        public virtual async Task Update(TEntity entity)
        {
            await Repository.Update(entity);
            Logger.LogInformation("{entity} {id} updated in {repository}.", typeof(TEntity), entity.GetId(), GetType());
        }
    }
}