using System;

namespace Faemiyah.BtDamageResolver.Api.Entities.Interfaces
{
    /// <summary>
    /// Base interface for an entity stored in repositories, which can be activated or deactivated.
    /// </summary>
    /// <typeparam name="TKey">The primary key type of the entity.</typeparam>
    public interface IActivatableEntity<TKey> : IEntity<TKey> where TKey : IComparable
    {
        /// <summary>
        /// Activate the entity.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivate the entity.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Deactivate the entity.
        /// </summary>
        bool GetActiveStatus();
    }
}