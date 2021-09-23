using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Exceptions;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories
{
    /// <summary>
    /// The base interface for a repository actor.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity in the repository.</typeparam>
    /// <typeparam name="TKey">The primary key of the entity for repository access.</typeparam>
    public interface IEntityRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IComparable
    {
        /// <summary>
        /// Adds a <see cref="TEntity"/> into the repository.
        /// </summary>
        /// <param name="entity">The entity to add to the repository.</param>
        /// <exception cref="DataAccessException"> with the error code AlreadyExists if the entity already exists.</exception>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task Add(TEntity entity);

        /// <summary>
        /// Adds a <see cref="TEntity"/> into the repository or updates it to match the given entity, if already present.
        /// </summary>
        /// <param name="entity">The entity to add to the repository or to update with new values.</param>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task AddOrUpdate(TEntity entity);

        /// <summary>
        /// Deletes a <see cref="TEntity"/> with a given key from the repository.
        /// </summary>
        /// <param name="key">The key of the entity to delete.</param>
        /// <remarks>If the entity does not exist, this is simply accepted.</remarks>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task Delete(TKey key);

        /// <summary>
        /// Gets a <see cref="TEntity"/> with the given key from the repository.
        /// </summary>
        /// <param name="key">The key of the entity to get.</param>
        /// <returns>The entity, or null, if none could be found.</returns>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task<TEntity> Get(TKey key);

        /// <summary>
        /// Get all entities from the repository.
        /// </summary>
        /// <returns>A list of <see cref="TEntity"/> containing all entities from the database, or an empty list, if none could be found.</returns>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task<List<TEntity>> GetAll();

        /// <summary>
        /// Updates a <see cref="TEntity"/> in the repository.
        /// </summary>
        /// <exception cref="DataAccessException"> with the error code NotFound if the entity does not exist.</exception>
        /// <exception cref="DataAccessException"> with the error code OperationFailure if there is a problem with the repository.</exception>
        Task Update(TEntity entity);
    }
}