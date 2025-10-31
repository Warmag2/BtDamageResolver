using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;

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
    /// <summary>
    /// The logging interface.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The repository back-end.
    /// </summary>
    protected readonly CachedEntityRepository<TEntity, TKey> Repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalRepositoryActorBase{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="repository">The repository back-end.</param>
    protected ExternalRepositoryActorBase(ILogger logger, CachedEntityRepository<TEntity, TKey> repository)
    {
        Logger = logger;
        Repository = repository;
    }

    /// <inheritdoc />
    public virtual async Task Add(TEntity entity)
    {
        await Repository.AddAsync(entity);
        Logger.LogDebug("{EntityType} with key {EntityId} added to {RepositoryType}.", typeof(TEntity), entity.GetName(), GetType());
    }

    /// <inheritdoc />
    public virtual async Task AddOrUpdate(TEntity entity)
    {
        await Repository.AddOrUpdateAsync(entity);
        Logger.LogDebug("{EntityType} with key {EntityId} added to or updated in {RepositoryType}.", typeof(TEntity), entity.GetName(), GetType());
    }

    /// <inheritdoc />
    public virtual async Task<bool> Delete(TKey key)
    {
        var result = await Repository.DeleteAsync(key);
        Logger.LogDebug("{EntityType} with key {EntityId} deleted from {RepositoryType}.", typeof(TEntity), key, GetType());
        return result;
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> Get(TKey key)
    {
        return await Repository.GetAsync(key);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyCollection<TEntity>> GetAll()
    {
        return await Repository.GetAllAsync();
    }

    /// <inheritdoc />
    public virtual async Task Update(TEntity entity)
    {
        await Repository.UpdateAsync(entity);
        Logger.LogDebug("{EntityType} with key {EntityId} updated in {RepositoryType}.", typeof(TEntity), entity.GetName(), GetType());
    }
}