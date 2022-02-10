using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Exceptions;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes
{
    /// <summary>
    /// The base interface for a repository actor.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity in the repository.</typeparam>
    /// <typeparam name="TKey">The primary key of the entity for repository access.</typeparam>
    public interface IExternalRepositoryActorBase<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IComparable
    {
        /// <summary>
        /// Adds a <see cref="TEntity"/> into the repository governed by this repository actor.
        /// </summary>
        /// <param name="entity">The entity to add to the repository.</param>
        /// <exception cref="DataAccessException"> with the error code AlreadyExists if the entity already exists.</exception>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        /// <returns>A task which finishes when the item has been added to the repository.</returns>
        Task Add(TEntity entity);

        /// <summary>
        /// Adds a <see cref="TEntity"/> into the repository governed by this actor or updates it to match the given entity, if already present.
        /// </summary>
        /// <param name="entity">The entity to add to the repository or to update with new values.</param>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        /// <returns>A task which finishes when the item has been added to or updated in the repository.</returns>
        Task AddOrUpdate(TEntity entity);

        /// <summary>
        /// Deletes a <see cref="TEntity"/> with a given key from the repository governed by this repository actor.
        /// </summary>
        /// <param name="key">The key of the entity to delete.</param>
        /// <remarks>If the entity does not exist, this is simply accepted.</remarks>
        /// <returns><b>True</b> if the entity existed before this deletion, <b>false</b> otherwise.</returns>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task<bool> Delete(TKey key);

        /// <summary>
        /// Gets a <see cref="TEntity"/> with the given key from the repository governed by this repository actor.
        /// </summary>
        /// <param name="key">The key of the entity to get.</param>
        /// <returns>The entity, or null, if none could be found.</returns>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task<TEntity> Get(TKey key);

        /// <summary>
        /// GetAsync all entities from the repository governed by this repository actor.
        /// </summary>
        /// <returns>A list of <see cref="TEntity"/> containing all entities from the database, or an empty list, if none could be found.</returns>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task<List<TEntity>> GetAll();

        /// <summary>
        /// Updates a <see cref="TEntity"/> in the repository governed by this repository actor.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <exception cref="DataAccessException"> with the error code NotFound if the entity does not exist.</exception>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        /// <returns>A task which finishes when the item has been updated in the repository.</returns>
        Task Update(TEntity entity);
    }
}