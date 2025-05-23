using System;

namespace Faemiyah.BtDamageResolver.Api.Entities.Interfaces;

/// <summary>
/// Base interface for an entity stored in repositories.
/// </summary>
/// <typeparam name="TKey">The primary key type of the entity.</typeparam>
public interface IEntity<TKey>
    where TKey : IComparable
{
    /// <summary>
    /// Gets the base database identifier of the entity.
    /// </summary>
    /// <returns>The base database identifier of the entity.</returns>
    public TKey GetName();

    /// <summary>
    /// Sets the base database identifier of the IEntity.
    /// </summary>
    /// <param name="name">The id to set the base identifier to.</param>
    public void SetName(TKey name);
}