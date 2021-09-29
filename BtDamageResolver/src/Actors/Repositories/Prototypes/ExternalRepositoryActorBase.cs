using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes
{
    /// <summary>
    /// Base repository actor class.
    /// </summary>
    /// <typeparam name="TEntity">The entity to store.</typeparam>
    /// <typeparam name="TKey">The primary key of the entity to store.</typeparam>
    [StatelessWorker(1)]
    public abstract class ExternalRepositoryActorBase<TEntity, TKey> : Grain, IExternalRepositoryActorBase<TEntity, TKey>
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
            await Repository.AddAsync(entity);
            Logger.LogInformation("{entity} {id} added to {repository}.", typeof(TEntity), entity.GetId(), GetType());
        }

        /// <inheritdoc />
        public virtual async Task AddOrUpdate(TEntity entity)
        {
            await Repository.AddOrUpdateAsync(entity);
            Logger.LogInformation("{entity} {id} added to or updated in {repository}.", typeof(TEntity), entity.GetId(), GetType());
        }

        /// <inheritdoc />
        public virtual async Task Delete(TKey key)
        {
            await Repository.DeleteAsync(key);
            Logger.LogInformation("{entity} with key {key} deleted from {repository}.", typeof(TEntity), key, GetType());
        }

        /// <inheritdoc />
        public virtual async Task<TEntity> Get(TKey key)
        {
            return await Repository.GetAsync(key);
        }

        /// <inheritdoc />
        public virtual async Task<List<TEntity>> GetAll()
        {
             return await Repository.GetAllAsync(); ;
        }

        /// <inheritdoc />
        public virtual async Task Update(TEntity entity)
        {
            await Repository.UpdateAsync(entity);
            Logger.LogInformation("{entity} {id} updated in {repository}.", typeof(TEntity), entity.GetId(), GetType());
        }
    }
}