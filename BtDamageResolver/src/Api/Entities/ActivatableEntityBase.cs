using System;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <inheritdoc cref="IActivatableEntity{TKey}" />
    public abstract class ActivatableEntityBase<TKey> : EntityBase<TKey>, IActivatableEntity<TKey> where TKey : IComparable
    {
        /// <summary>
        /// Is the entity active.
        /// </summary>
        public bool Active { get; set; }

        /// <inheritdoc />
        public void Activate()
        {
            Active = true;
        }

        /// <inheritdoc />
        public void Deactivate()
        {
            Active = false;
        }

        /// <inheritdoc />
        public bool GetActiveStatus() => Active;
    }
}